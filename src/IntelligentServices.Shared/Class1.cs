using System;

namespace IntelligentServices.Shared
{


    public enum StreamingResultState
    {
        Query,
        QueryRead,
        Read,
        Errored,
        Cancelled,
        Complete
    }
    public interface IStreamingResult
    {

        StreamingResultState State { get; set; }
        string Status { get; set; }
        Exception Exception { get; set; }
        string RequestId { get; set; }
        int Index { get; set; }
        int Count { get; set; }
    }
    public interface IStreamingResultContext : IStreamingResult
    {
        Func<System.Threading.Tasks.Task> Cancel { get; }
    }
    
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
    public class StreamingResultContext<T> : StreamingResult<T>, IStreamingResultContext
    {
        public Func<System.Threading.Tasks.Task> Cancel { get; set; }

    }


    public class PromptMessage
    {
        public string WaitId { get; set; }
        public string Text { get; set; }

        public string[] Buttons { get; set; }
        public object PromptForm { get; set; }
    }

    public class PromptResult
    {
        public PromptCloseType CloseType { get; set; }
        public string CloseButton { get; set; }
    }
    public class PromptResult<T> : PromptResult
    {
        public T PromptForm { get; set; }
    }
    public enum PromptCloseType
    {
        Button,
        Cancelled
    }
}
