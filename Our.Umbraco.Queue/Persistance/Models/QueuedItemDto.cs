using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPoco;
using Our.Umbraco.Queue.Models;
using Umbraco.Core;
using Umbraco.Core.Mapping;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Our.Umbraco.Queue.Persistance
{
    [TableName(Queue.QueueTable)]
    [PrimaryKey("id", AutoIncrement = true)]
    public class QueuedItemDto
    {
        [Column("id")]
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }

        [Column("udi")]
        public string Udi { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("userId")]
        public int UserId { get; set; }

        [Column("submitted")]
        public DateTime Submitted { get; set; }

        [Column("attempt")]
        public int Attempt { get; set; }

        [Column("action")]
        public string Action { get; set; }

        [Column("priority")]
        public int Priority { get; set; }

        [Column("schedule")]
        public DateTime Schedule { get; set; }

        [Column("data")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Data { get; set; }
    }


    public class QueuedItemMapper : IMapDefinition
    {
        public void DefineMaps(UmbracoMapper mapper)
        {
            mapper.Define<QueuedItemDto, QueuedItem>((source, context) => new QueuedItem(), MapFromDto);
            mapper.Define<QueuedItem, QueuedItemDto>((source, context) => new QueuedItemDto(), MapToDto);
        }

        private void MapFromDto(QueuedItemDto source, QueuedItem target, MapperContext mapper)
        {
            target.Action = source.Action;
            target.Attempt = source.Attempt;
            target.Data = source.Data;
            target.Id = source.Id;
            target.Name = source.Name;
            target.Priority = source.Priority;
            target.Schedule = source.Schedule;
            target.Submitted = source.Submitted;
            target.Udi = Udi.Parse(source.Udi);
            target.UserId = source.UserId;
        }

        private void MapToDto(QueuedItem source, QueuedItemDto target, MapperContext context)
        {
            target.Action = source.Action;
            target.Attempt = source.Attempt;
            target.Data = source.Data;
            target.Id = source.Id;
            target.Name = source.Name;
            target.Priority = source.Priority;
            target.Schedule = source.Schedule;
            target.Submitted = source.Submitted;
            target.Udi = source.Udi.ToString();
            target.UserId = source.UserId;
        }
    }
}
