using IntelligentServices.Shared;
using System.Threading.Tasks;

namespace IntelligentServices.Server
{
    public class WaitingContext<T> : IWaitingContext
    {
        public string WaitId { get; set; }
        public IDialogMessage DialogMessage { get; set; }

        public TaskCompletionSource<T> DialogResultSource = new TaskCompletionSource<T>()

           ;

        public void ProcessResult(string json)
        {
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            DialogResultSource.SetResult(result);
        }
    }
}
