using System;

namespace IntelligentServices.Shared
{
    public class StreamingResult<T> : IStreamingResult
    {
        public StreamingResultState State { get; set; }
        public int Index { get; set; }
        public int Count { get; set; }
        public Exception Exception { get; set; }
        public T[] Items { get; set; }
        public string RequestId { get; set; }
        public string Status { get; set; }
    }
}
