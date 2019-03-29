using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace IntelligentServices.Server
{
    public class SigRHub : Hub
    {
        StreamingRuntimeContexts StreamingRuntimeContexts;

        public SigRHub(StreamingRuntimeContexts streamingRuntimeContexts)
        {
            StreamingRuntimeContexts = streamingRuntimeContexts;
           
        }
        public async Task<string> GetConnectionId()
        {
            return this.Context.ConnectionId;
        }
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }
      public async Task<string> TestCommand(string name)
        {
            await Task.Delay(100);
            return "Hello, " + name;
        }
        public async Task PromptReturn(string waitId, string promptResultJson)
        {
            StreamingRuntimeContexts.Waits.TryRemove(waitId, out IWaitingContext waitingContext);
            waitingContext.ProcessResult(promptResultJson);
            //await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        public async Task AlertReturn(string waitId, string alertResultJson)
        {
            StreamingRuntimeContexts.Waits.TryRemove(waitId, out IWaitingContext waitingContext);
            waitingContext.ProcessResult(alertResultJson);
            //await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

    }
}
