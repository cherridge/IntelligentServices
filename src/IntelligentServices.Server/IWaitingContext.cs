namespace IntelligentServices.Server
{
    public interface IWaitingContext
    {

        string WaitId { get; set; }

        void ProcessResult(string dialogResultJson);
    }
}
