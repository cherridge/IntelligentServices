using System;
using IntelligentServices.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http.Internal;
using System.IO;
using System.Globalization;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IntelligentServices.Server
{


    public class ObjectModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.BindingInfo.BinderType == typeof(ObjectModelBinder))
                return new ObjectModelBinder();
            return null;
        }
    }
    public class ObjectModelBinder : IModelBinder
    {
        Func<ModelBindingContext, object> Func;
        public ObjectModelBinder()
        {
        }

        public ObjectModelBinder(Func<ModelBindingContext, object> func)
        {
            Func = func;
        }


        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var act = bindingContext.ActionContext.ActionDescriptor.Properties["StreamingParameterConverter"] as Action<ModelBindingContext>;
            act(bindingContext);
            //            bindingContext.Result = ModelBindingResult.Success(Func(bindingContext));
            return Task.CompletedTask;
        }
    }
    public class CustomHttpBodyValueProviderFactory : IValueProviderFactory
    {
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            var request = context.ActionContext.HttpContext.Request;
            if (request.Method == "GET")
                return Task.CompletedTask;
            if (request.ContentType.StartsWith("application/json"))
            {
                return AddValueProviderAsync(context);
            }

            return Task.CompletedTask;
        }

        private static async Task AddValueProviderAsync(ValueProviderFactoryContext context)
        {
            var request = context.ActionContext.HttpContext.Request;

            var body = string.Empty;

            request.EnableRewind();

            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                body = await reader.ReadToEndAsync();
            }

            request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                return;
            }
            var jObject = Newtonsoft.Json.Linq.JObject.Parse(body);


            var valueProvider = new CustomHttpBodyValueProvider(
                BindingSource.Form,
               jObject,
                CultureInfo.CurrentCulture);

            context.ValueProviders.Add(valueProvider);
        }

    }
    public class CustomHttpBodyValueProvider : BindingSourceValueProvider, IEnumerableValueProvider
    {
        private readonly Newtonsoft.Json.Linq.JObject JObject;
        private PrefixContainer _prefixContainer;
        private readonly CultureInfo _culture;

        public CustomHttpBodyValueProvider(
            BindingSource bindingSource,
            Newtonsoft.Json.Linq.JObject
            jObject,
            CultureInfo culture) : base(bindingSource)
        {
            JObject = jObject;
            _culture = culture;
        }

        protected PrefixContainer PrefixContainer
        {
            get
            {
                if (_prefixContainer == null)
                {
                    var propNames = JObject.Properties().Select(prop => prop.Name).ToArray();

                    _prefixContainer = new PrefixContainer(
                    propNames);
                }

                return _prefixContainer;
            }
        }

        public override bool ContainsPrefix(string prefix)
        {
            return PrefixContainer.ContainsPrefix(prefix.ToLower());
        }

        public IDictionary<string, string> GetKeysFromPrefix(string prefix)
        {
            return PrefixContainer.GetKeysFromPrefix(prefix);
        }

        public override ValueProviderResult GetValue(string key)
        {
            object rawValue = JObject.Property(key).First();
            var sv = new StringValues(rawValue.ToString());
            return new ValueProviderResult(sv, _culture);
        }

    }

    public class StreamingRoutingConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                Func<IntelligentServices.Shared.IStreamingResult> testF = () => {

                    return null;
                };

                var sr_start = controller.ControllerType.GetMethod("SR_Start");
                var sr_continue = controller.ControllerType.GetMethod("SR_Continue");
                var sr_cancel = controller.ControllerType.GetMethod("SR_Cancel");

                foreach (var action in controller.Actions.Where(a => a.ActionMethod.ReturnType.IsGenericType && a.ActionMethod.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)).ToArray())
                {
                    var startA = new ActionModel(sr_start.MakeGenericMethod(action.ActionMethod.ReturnType.GenericTypeArguments[0]), action.Attributes);
                    var paraInfo = startA.ActionMethod.GetParameters()[0];
                    var para1 = new ParameterModel(paraInfo, new List<object>());
                    para1.ParameterName = paraInfo.Name;

                    Action<ModelBindingContext> vp = async (context) => {
                        var controllerInst = context.HttpContext.RequestServices.GetRequiredService(action.ActionMethod.DeclaringType);
                       
                        var args = action.ActionMethod.GetParameters().Select(para => new { para = para, inString = context.ValueProvider.GetValue(para.Name) }).ToArray();

                        var argsTyped = args.Select(a => Convert.ChangeType(a.inString.FirstValue, a.para.ParameterType)).ToArray();



                        var asyncE = action.ActionMethod.Invoke(controllerInst, argsTyped);

                        context.Result = ModelBindingResult.Success(asyncE);
                    };

                    startA.Properties.Add("StreamingParameterConverter", vp);
                    para1.BindingInfo = new BindingInfo();
                    para1.BindingInfo.BinderType = typeof(ObjectModelBinder);
                    // para1.BindingInfo.
                    startA.Parameters.Add(para1);
                    foreach (var sel in action.Selectors)
                    {
                        startA.Selectors.Add(sel);
                    }
                    // startA..Add(action.Selectors[0]);
                    startA.Controller = controller;
                    startA.ActionName = action.ActionName + "_Start";

                    controller.Actions.Add(startA);

                    var continueA = new ActionModel(sr_continue.MakeGenericMethod(action.ActionMethod.ReturnType.GenericTypeArguments[0]), action.Attributes);
                    continueA.Controller = controller;
                    continueA.ActionName = action.ActionName + "_Continue";

                    var cont_paraInfo = sr_continue.GetParameters()[0];
                    var cont_para = new ParameterModel(cont_paraInfo, new List<object>());
                    cont_para.ParameterName = cont_paraInfo.Name;

                    continueA.Parameters.Add(cont_para);
                    var contselectorModel = new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel() { Template = "[action]/{" + cont_paraInfo.Name + "}" }
                    };
                    continueA.Selectors.Add(contselectorModel);
                    controller.Actions.Add(continueA);

                    var cancelA = new ActionModel(sr_cancel, action.Attributes);
                    cancelA.Controller = controller;
                    cancelA.ActionName = action.ActionName + "_Cancel";


                    var cancel_paraInfo = sr_cancel.GetParameters()[0];
                    var cancel_para = new ParameterModel(cancel_paraInfo, new List<object>());
                    cancel_para.ParameterName = cancel_paraInfo.Name;

                    cancelA.Parameters.Add(cancel_para);
                    var cancselectorModel = new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel() { Template = "[action]/{" + cancel_paraInfo.Name + "}" }
                    };

                    cancelA.Selectors.Add(cancselectorModel);
                    controller.Actions.Add(cancelA);

                    //controller.Actions.Add(new ActionModel(action) { ActionName = action.ActionName + "_Start" });
                    //controller.Actions.Add(new ActionModel(action) { ActionName = action.ActionName + "_Continue", Parameters=new[] { new ParameterModel( } });
                    //controller.Actions.Add(new ActionModel(action) { ActionName = action.ActionName + "_Cancel" });

                }
            }

            // You can continue to put attribute route templates for the controller actions depending on the way you want them to behave
        }
    }

    public interface IStreamingRuntimeContext
    {
        string RequestId { get; set; }

        IStreamingResult CreateResult(bool allowEmpty);
        void Cancel();
    }
    public class StreamingRuntimeContext<T> : IStreamingRuntimeContext
    {
        const int maxSize = 64;
        const int waitMaxMs = 1000 * 60 * 2;
        const int processMaxMs = 500;
        public IStreamingResult CreateResult(bool allowEmpty)
        {
            StreamingResult<T> streamingResult = new StreamingResult<T>();
            streamingResult.Exception = Exception;

            streamingResult.RequestId = RequestId;
            if (State == StreamingResultState.Query || State == StreamingResultState.QueryRead || State == StreamingResultState.Read || State == StreamingResultState.Complete)
            {
                List<T> resItems = new List<T>();
                try
                {


                    if (Items.TryTake(out T item, allowEmpty ? 0 : waitMaxMs, ct))
                    {
                        var start = DateTime.UtcNow; ;
                        resItems.Add(item);
                        while (true)
                        {

                            if (Items.TryTake(out T item2, (DateTime.UtcNow - start).TotalMilliseconds > processMaxMs ? 0 : processMaxMs, ct))
                            {
                                resItems.Add(item2);
                            }
                            else
                            {
                                break;
                            }
                            if (resItems.Count == maxSize)
                            {
                                break;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {

                    State = StreamingResultState.Cancelled;
                    streamingResult.Index = Index;
                    streamingResult.Count = Count;
                    streamingResult.State = State;
                    return streamingResult;
                }

                streamingResult.Items = resItems.ToArray();
                Index += streamingResult.Items.Length;
                if (Items.IsCompleted)
                {

                    State = StreamingResultState.Complete;
                    Count = Index + Items.Count;
                }
                else
                if (Items.IsAddingCompleted)
                {
                    State = StreamingResultState.Read;
                    Count = Index + Items.Count;
                }

            }

            streamingResult.Index = Index;
            streamingResult.Count = Count;
            streamingResult.State = State;
            return streamingResult;
        }
        public StreamingResultState State { get; set; }
        public Exception Exception { get; set; }

        public IAsyncEnumerable<T> AsyncEnumerable { get; set; }
        public string RequestId { get; set; }

        int Index;
        int Count;

        System.Threading.CancellationTokenSource cts;
        System.Threading.CancellationToken ct;

        Task RunTask;
        System.Collections.Concurrent.BlockingCollection<T> Items { get; } = new System.Collections.Concurrent.BlockingCollection<T>();
        public void Run()
        {
            cts = new System.Threading.CancellationTokenSource();
            ct = cts.Token;
            RunTask = Task.Run(DoRun, ct);
        }
        bool checkCancellation()
        {
            if (ct.IsCancellationRequested)
            {
                State = StreamingResultState.Cancelled;
                return true;
            }
            return false;
        }
        async void DoRun()
        {
            try
            {
                State = StreamingResultState.Query;
                var asyncEn = AsyncEnumerable;
                if (checkCancellation())
                    return;

                State = StreamingResultState.QueryRead;
                await foreach (var item in asyncEn)
                {
                    Items.Add(item);
                    if (checkCancellation())
                        return;
                }
                Items.CompleteAdding();
                State = StreamingResultState.Read;
            }
            catch (Exception ex)
            {
                State = StreamingResultState.Errored;
                Exception = ex;
            }
        }

        public void Cancel()
        {
            cts.Cancel();
        }
    }

    public interface IWaitingContext
    {

        string WaitId { get; set; }

        void ProcessResult(string promptResultJson);
    }
    public class WaitingContext<T> : IWaitingContext
    {
        public string WaitId { get; set; }
        public PromptMessage PromptMessage { get; set; }

        public TaskCompletionSource<T> PromptResultSource = new TaskCompletionSource<T>()

           ;

        public void ProcessResult(string json)
        {
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            PromptResultSource.SetResult(result);
        }
    }
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
        public async Task PromptReturn(string waitId, string promptResultJson)
        {
            StreamingRuntimeContexts.Waits.TryRemove(waitId, out IWaitingContext waitingContext);
            waitingContext.ProcessResult(promptResultJson);
            //await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

    }
    public class StreamingRuntimeContexts
    {
        public System.Collections.Concurrent.ConcurrentDictionary<string, IWaitingContext> Waits { get; } = new System.Collections.Concurrent.ConcurrentDictionary<string, IWaitingContext>();

        public System.Collections.Concurrent.ConcurrentDictionary<string, IStreamingRuntimeContext> Contexts { get; } = new System.Collections.Concurrent.ConcurrentDictionary<string, IStreamingRuntimeContext>();
        public List<string> words { get; set; }
        public IWebHostEnvironment HostingEnvironment { get; }
        public StreamingRuntimeContexts(IWebHostEnvironment hostingEnvironment)
        {

            HostingEnvironment = hostingEnvironment;
            words = new List<string>();
            using (var sr = new System.IO.StreamReader(hostingEnvironment.WebRootFileProvider.GetFileInfo("words.txt").CreateReadStream()))
            {
                while (!sr.EndOfStream)
                {
                    words.Add(sr.ReadLine());
                }

            }
        }
    }
    public abstract class StreamControllerBase : Controller
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
           if (context.Result is ObjectResult objResult)
            {
                context.Result = new JsonResult(objResult.Value);
            }
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


        protected async Task<PromptResult> Alert(string text, string[] buttons)
        {
            WaitingContext<PromptResult> waitingContext = new WaitingContext<PromptResult>() { WaitId = Guid.NewGuid().ToString() };

            PromptMessage msg = new PromptMessage() { WaitId = waitingContext.WaitId, Text = text, Buttons = buttons };
            waitingContext.PromptMessage = msg;
            RuntimeContexts.Waits.TryAdd(waitingContext.WaitId, waitingContext);


            // await Json(msg).ExecuteResultAsync(ControllerContext);
            // await ControllerContext.HttpContext.Response.Body.FlushAsync();
            await SigRHub.Clients.Client(ControllerContext.HttpContext.Request.Headers["SignalRId"].SingleOrDefault()).SendAsync("Alert", msg);

            return await waitingContext.PromptResultSource.Task;

        }
        protected async Task<PromptResult<T>> Prompt<T>(string text, string[] buttons, T promptForm)
        {
            WaitingContext<PromptResult<T>> waitingContext = new WaitingContext<PromptResult<T>>() { WaitId = Guid.NewGuid().ToString() };

            PromptMessage msg = new PromptMessage() { WaitId = waitingContext.WaitId, Text = text, Buttons = buttons, PromptForm = promptForm };
            waitingContext.PromptMessage = msg;
            RuntimeContexts.Waits.TryAdd(waitingContext.WaitId, waitingContext);


            // await Json(msg).ExecuteResultAsync(ControllerContext);
            // await ControllerContext.HttpContext.Response.Body.FlushAsync();
            await SigRHub.Clients.Client(ControllerContext.HttpContext.Request.Headers["SignalRId"].SingleOrDefault()).SendAsync("Prompt", msg);

            return await waitingContext.PromptResultSource.Task;

        }
    }
}
