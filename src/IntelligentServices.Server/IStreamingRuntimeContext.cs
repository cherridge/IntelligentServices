using IntelligentServices.Shared;

namespace IntelligentServices.Server
{
    public interface IStreamingRuntimeContext
    {
        string RequestId { get; set; }

        IStreamingResult CreateResult(bool allowEmpty);
        void Cancel();
    }
}
