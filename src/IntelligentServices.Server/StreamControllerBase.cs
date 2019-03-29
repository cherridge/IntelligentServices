using System;
using IntelligentServices.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IntelligentServices.Server
{
    public abstract class StreamControllerBase : Controller
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
           if (context.Result is ObjectResult objResult)
            {
                if (Response.HasStarted)
                {
                  var t=  Task.Factory.StartNew(async () => { await writeValue(objResult.Value); });
                    t.Wait();
                    context.Result = new EmptyResult();

                }
                else
                {
                    context.Result = new JsonResult(objResult.Value);
                }
            }
        }
        async Task writeValue(object val)
        {

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(val);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await Response.Body.WriteAsync(bytes, 0, bytes.Length);

        }

        protected IWebHostEnvironment HostingEnvironment { get; }
        protected StreamingRuntimeContexts RuntimeContexts { get; }
        protected IHubContext<SigRHub> SigRHub { get; }
        public StreamControllerBase(IWebHostEnvironment hostingEnvironment, StreamingRuntimeContexts streamingRuntimeContexts, IHubContext<SigRHub> sigRHub)
        {
            HostingEnvironment = hostingEnvironment;
            RuntimeContexts = streamingRuntimeContexts;
            SigRHub = sigRHub;

        }
        public StreamingResult<T> SR_Start<T>(IAsyncEnumerable<T> AsyncEnum)
        {
            StreamingRuntimeContext<T> streamingRuntimeContext = new StreamingRuntimeContext<T>();
            streamingRuntimeContext.RequestId = Guid.NewGuid().ToString();
            streamingRuntimeContext.AsyncEnumerable = AsyncEnum;
            streamingRuntimeContext.State = StreamingResultState.Query;
            //streamingRuntimeContext.Parameter1 = search;
            if (!RuntimeContexts.Contexts.TryAdd(streamingRuntimeContext.RequestId, streamingRuntimeContext))
            {
                throw new Exception("?");
            }
            streamingRuntimeContext.Run();

            return streamingRuntimeContext.CreateResult(true) as StreamingResult<T>;
        }
        public StreamingResult<T> SR_Continue<T>(string requestId)
        {
            return RuntimeContexts.Contexts[requestId].CreateResult(false) as StreamingResult<T>;
        }
        public void SR_Cancel(string requestId)
        {
            RuntimeContexts.Contexts[requestId].Cancel();
        }

        protected async Task<Shared.ButtonBase> Alert(string title,string icon,string message, Shared.ButtonBase[] buttons)
        {
            WaitingContext<Shared.ButtonBase> waitingContext = new WaitingContext<Shared.ButtonBase>() { WaitId = Guid.NewGuid().ToString() };

            AlertMessage msg = new AlertMessage() { WaitId = waitingContext.WaitId, Title=title, Icon=icon, Message=message  , Buttons = buttons};
            waitingContext.DialogMessage = msg;
            RuntimeContexts.Waits.TryAdd(waitingContext.WaitId, waitingContext);

            if (Response.ContentType == String.Empty)
            {

                Response.StatusCode = 200;
                Response.ContentType = "application/json";
            }

            // await Json(msg).ExecuteResultAsync(ControllerContext);
            // await ControllerContext.HttpContext.Response.Body.FlushAsync();
            await SigRHub.Clients.Client(ControllerContext.HttpContext.Request.Headers["SignalRId"].SingleOrDefault()).SendAsync("Alert", msg);

            Task task = Task.Factory.StartNew(async () => { await keepConnectionAlive(waitingContext.DialogResultSource.Task); }, TaskCreationOptions.LongRunning);


            return await waitingContext.DialogResultSource.Task;

        }

        protected async Task<PromptResult<T>> Prompt<T>(string text, string[] buttons, T promptForm)
        {
            WaitingContext<PromptResult<T>> waitingContext = new WaitingContext<PromptResult<T>>() { WaitId = Guid.NewGuid().ToString() };

            PromptMessage msg = new PromptMessage() { WaitId = waitingContext.WaitId, Text = text,  PromptForm = promptForm };
            waitingContext.DialogMessage = msg;
            RuntimeContexts.Waits.TryAdd(waitingContext.WaitId, waitingContext);

            if (Response.ContentType == String.Empty)
            {

                Response.StatusCode = 200;
                Response.ContentType = "application/json";
            }

            // await Json(msg).ExecuteResultAsync(ControllerContext);
            // await ControllerContext.HttpContext.Response.Body.FlushAsync();
            await SigRHub.Clients.Client(ControllerContext.HttpContext.Request.Headers["SignalRId"].SingleOrDefault()).SendAsync("Prompt", msg);

            Task task =  Task.Factory.StartNew( async() => { await keepConnectionAlive(waitingContext.DialogResultSource.Task); }, TaskCreationOptions.LongRunning);

            
            return await waitingContext.DialogResultSource.Task;

        }

        protected async Task keepConnectionAlive(Task waitTask)
        {
            while (true)
            {
                var btrs = System.Text.Encoding.ASCII.GetBytes(" ");
               await  Response.Body.WriteAsync(btrs, 0, btrs.Length);
                await Response.Body.FlushAsync();
                await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(10)), waitTask);

                if (waitTask.IsCompleted)
                    return;

            }
        }
    }
}
