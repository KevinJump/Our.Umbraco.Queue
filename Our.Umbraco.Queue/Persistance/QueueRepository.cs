using System;
using System.Collections.Generic;
using System.Linq;

using NPoco;

using Our.Umbraco.Queue.Models;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Mapping;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Core.Scoping;

namespace Our.Umbraco.Queue.Persistance
{
    public class QueueRepository : IQueueRepository
    {
        const int maxParams = 2000; // SqlCE Group limit

        private readonly IScopeAccessor scopeAccessor;
        private readonly IProfilingLogger logger;

        private readonly object queueLock = new object();

        private readonly UmbracoMapper mapper;

        public QueueRepository(
            IScopeAccessor scopeAccessor,
            IProfilingLogger logger)
        {
            this.scopeAccessor = scopeAccessor;
            this.logger = logger;

            var definitions = new MapDefinitionCollection(new IMapDefinition[]
            {
                new QueuedItemMapper()
            });

            mapper = new UmbracoMapper(definitions);
        }

        private IScope AmbientScope
        {
            get
            {
                var scope = scopeAccessor.AmbientScope;
                if (scope == null)
                    throw new InvalidOperationException("Cannot run queue repo without an ambient scope");
                return scope;
            }
        }

        private IUmbracoDatabase database => AmbientScope.Database;
        private ISqlContext sqlContext => AmbientScope.SqlContext;
        private Sql<ISqlContext> Sql() => sqlContext.Sql();
        private ISqlSyntaxProvider sqlSyntax => sqlContext.SqlSyntax;
        private IQuery<T> Query<T>() => sqlContext.Query<T>();

        private Sql<ISqlContext> GetBaseQuery(bool isCount)
            => isCount
                ? Sql().SelectCount().From<QueuedItemDto>()
                : Sql().Select($"{Queue.QueueTable}.*").From<QueuedItemDto>();

        private string GetBaseWhereClause()
            => $"{Queue.QueueTable}.Id = @Id";


        public QueuedItem Get(int id)
        {
            var sql = GetBaseQuery(false)
                        .Where(GetBaseWhereClause(), new { Id = id });

            var dto = database.FirstOrDefault<QueuedItemDto>(sql);

            if (dto == null) return default;

            return mapper.Map<QueuedItem>(dto);
        }

        public IEnumerable<QueuedItem> GetAll(params int[] ids)
        {
            if (ids.Length == 0) return DoGetAll();

            ids = ids.Distinct().ToArray(); // remove duplicates

            if (ids.Length <= maxParams)
                return DoGetAll(ids);

            // else there are to many ids to get in a single query. 
            List<QueuedItem> results = new List<QueuedItem>();

            foreach(var groupOfIds in ids.InGroupsOf(maxParams))
            {
                results.AddRange(DoGetAll(groupOfIds.ToArray()));
            }

            return results;
        }

        private IEnumerable<QueuedItem> DoGetAll(params int[] ids)
        {
            var sql = GetBaseQuery(false);

            if (ids.Length > 0)
            {
                sql.Where($"{Queue.QueueTable}.Id in (@Ids)", new { Ids = ids });
            }
            else
            {
                sql.Where($"{Queue.QueueTable}.Id > 0");
            }

            return database.Fetch<QueuedItemDto>(sql)
                .Select(x => mapper.Map<QueuedItem>(x));
        }

        public PagedResult<QueuedItem> GetPaged(int page, int pageSize, Sql<ISqlContext> sql)
        {
            var results = database.Page<QueuedItemDto>(page, pageSize, sql);

            return new PagedResult<QueuedItem>(results.TotalItems, results.CurrentPage, results.ItemsPerPage)
            {
                Items = results.Items.Select(x => mapper.Map<QueuedItem>(x))
            };
        }

        public PagedResult<QueuedItem> GetAllPaged(int page, int pageSize, params int[] ids)
        {
            ids.Distinct().ToArray();

            if (ids.Length > maxParams && page * pageSize > maxParams)
            {
                // if we are asking for items beyond the sqlce groupby limit
                // then we need to do this a slower way :( 
            }

            var sql = GetBaseQuery(false);

            if (ids.Length > 0)
                sql.Where($"{Queue.QueueTable}.id in (@Ids)",
                    new { Ids = ids.Take(maxParams) });

            sql.OrderBy($"{Queue.QueueTable}.id");

            var results = database.Page<QueuedItemDto>(page, pageSize, sql);

            if (ids.Length > maxParams)
            {
                // we have to guess the pages, because there are more than 
                // we have queried for.
                results.TotalItems = ids.Length;
                results.TotalPages = (long)Math.Ceiling((decimal)ids.Length / pageSize);
            }

            return new PagedResult<QueuedItem>(results.TotalItems, results.CurrentPage, results.ItemsPerPage)
            {
                Items = results.Items.Select(x => mapper.Map<QueuedItem>(x))
            };
        }

        /// <summary>
        ///  called when we have asked for a page of specific items past the 
        ///  SqlCE 2000 item limit (not the same as paging all past this limit!)
        /// </summary>
        /// <remarks>
        ///  because we can't do the in (...) with more than 2000 ids, this does 
        ///  it a slower way, by getting all then working out the items that 
        ///  would belong on this page.
        ///  
        ///  this should hardly (if ever) be called, because paging this high
        ///  based on speicific ids is an edge case. 
        ///  
        ///  for example this would be - listing items of type
        ///  (so a from a seperate query) past page 20
        /// </remarks>
        /// <returns></returns>
        private PagedResult<QueuedItem> GetPagedPastLimit(int page, int pageSize, params int[] ids)
        {
            var allItems = GetAll(ids).ToArray();
            var start = (page - 1) * pageSize;
            var size = Math.Min(pageSize, allItems.Length - start);

            var items = new QueuedItem[size];
            Array.Copy(allItems, start, items, 0, size);

            return new PagedResult<QueuedItem>(allItems.Length, page, pageSize)
            {
                Items = items
            };
        }



        /// <summary>
        ///  Clears the queue of all items. 
        /// </summary>
        public void Clear()
        {
            lock (queueLock)
            {
                var sql = $"DELETE {Queue.QueueTable} WHERE {Queue.QueueTable}.Id > 0";

                using (var transaction = database.GetTransaction())
                {
                    database.Execute(sql);
                    transaction.Complete();
                }
            }
        }

        public int Count()
        {
            var sql = GetBaseQuery(true);
            return database.ExecuteScalar<int>(sql);
        }

        /// queue actions
        /// 
        public QueuedItem Enqueue(QueuedItem item)
        {
            lock (queueLock)
            {

                var dto = mapper.Map<QueuedItemDto>(item);

                using (var transaction = database.GetTransaction())
                {
                    database.Save(dto);
                    transaction.Complete();
                }

                return mapper.Map<QueuedItem>(dto);
            }
        }

        public QueuedItem Dequeue()
        {
            lock (queueLock)
            {
                var sql = $"SELECT top(1) * FROM {Queue.QueueTable} " +
                    $"WHERE schedule < @date ORDER BY PRIORITY DESC, SUBMITTED, schedule;";

                var item = database.FirstOrDefault<QueuedItemDto>(sql, new { date = DateTime.Now });

                if (item != null)
                {
                    using (var transaction = database.GetTransaction())
                    {
                        database.Delete<QueuedItemDto>(item.Id);
                        transaction.Complete();
                    }
                }

                return mapper.Map<QueuedItem>(item);

            }
        }

        public IEnumerable<QueuedItem> List()
            => GetAll();

        public PagedResult<QueuedItem> List(int page, int pageSize)
            => GetAllPaged(page, pageSize);
    }
}