using System;

namespace IntelligentServices.Shared
{
    public class StreamingResultContext<T> : StreamingResult<T>, IStreamingResultContext
    {
        public Func<System.Threading.Tasks.Task> Cancel { get; set; }

    }
}
