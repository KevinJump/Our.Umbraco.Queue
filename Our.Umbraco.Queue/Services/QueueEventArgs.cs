namespace Our.Umbraco.Queue.Services
{
    public class QueueEventArgs
    {
        public bool Processing { get; set; }

        /// <summary>
        ///  number of items in the queue
        /// </summary>
        public int QueueSize { get; set; }

        /// <summary>
        ///  number of items processed this run
        /// </summary>
        public int Processed { get; set; }

        /// <summary>
        ///  number of items remaining 
        /// </summary>
        public int Remaining { get; set; }

        public bool Refresh { get; set; }
    }

}
