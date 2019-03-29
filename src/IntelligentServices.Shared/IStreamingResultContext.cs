using System;

namespace IntelligentServices.Shared
{
    public interface IStreamingResultContext : IStreamingResult
    {
        Func<System.Threading.Tasks.Task> Cancel { get; }
    }
}
