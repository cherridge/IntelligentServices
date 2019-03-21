using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using BlazorSignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace IntelligentServices.Client
{


    public class ServiceBuilder
    {
        public T Build<T>(System.IServiceProvider serviceProvider)
        {
            T service = System.Reflection.DispatchProxyAsync.Create<T, ServiceProxy<T>>();
            if (service is ServiceProxy<T> proxy)
            {
                proxy.ServiceProvider = serviceProvider;
                proxy.Build();
            }
            return service;
        }
    }
   public class StreamingServiceManager
    {
        public IServiceProvider ServiceProvider { get; internal set; }

        public StreamingServiceManager(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
       public void Cancel(Shared.IStreamingResult streamingResult)
        {

        }
        public async IAsyncEnumerable<Shared.StreamingResultContext<TReturnType>> AsStreaming<TServiceType,TReturnType>(System.Linq.Expressions.Expression<Func<TServiceType, IAsyncEnumerable<TReturnType>>> method)
        {
            var service = ServiceProvider.GetService<TServiceType>();
            var mce = method.Body as MethodCallExpression;
            List<object> args = new List<object>();
            foreach (var arg in mce.Arguments)
            {
                switch (arg)
                {
                    case ConstantExpression constantExpression:
                        args.Add(constantExpression.Value);
                        break;
                    default:
                        var argV = Expression.Lambda(arg).Compile().DynamicInvoke();
                        args.Add(argV);
                        break;
                }

            }


            dynamic argObject = new ExpandoObject();
            var zippedArgs = mce.Method.GetParameters().Zip(args, (pi, val) => new { ParameterInfo = pi, Value = val });

            var argObjectAsDictionary = argObject as IDictionary<String, Object>;
            foreach (var arg in zippedArgs)
                argObjectAsDictionary.Add(arg.ParameterInfo.Name, arg.Value);

            var serviceProxy = service as ServiceProxy<TServiceType>;
            string startUrl = $"{serviceProxy.UrlBase}/{mce.Method.Name}_Start";
            var sRes = await ServiceProvider.GetService<HttpClient>().PostJsonAsync<IntelligentServices.Shared.StreamingResultContext<TReturnType>>(startUrl, (object)argObject);

            string cancelUrl = $"{serviceProxy.UrlBase}/{mce.Method.Name}_Cancel/{sRes.RequestId}";
            Func<Task> cancelAction = async () =>
            {
                await ServiceProvider.GetService<HttpClient>().GetAsync(cancelUrl);
            };
            string continueUrl = $"{serviceProxy.UrlBase}/{mce.Method.Name}_Continue/" + sRes.RequestId;
            sRes.Cancel = cancelAction;
            while (true)
            {
                yield return sRes;

                if (sRes.State == IntelligentServices.Shared.StreamingResultState.Complete)
                    break;
                if (sRes.State == IntelligentServices.Shared.StreamingResultState.Cancelled)
                    break;
                sRes = await ServiceProvider.GetService<HttpClient>().GetJsonAsync<IntelligentServices.Shared.StreamingResultContext<TReturnType>>(continueUrl);
                sRes.Cancel = cancelAction;
            }
        }
        /*
        public async Task<TResult> AsStreaming2<TService, TParam1, TResult>(TService service, System.Linq.Expressions.Expression<Func<TService, Func<TParam1, TResult>>> method, TParam1 param1)
        {
            return default;
        }
            public async IAsyncEnumerable<Shared.StreamingResult<TResult>> AsStreaming<TService,TParam1,TResult>(TService service,System.Linq.Expressions.Expression<Func<TService,Func<TParam1,IAsyncEnumerable< TResult>>>> method,TParam1 param1)
        {
           var serviceProxy= service as ServiceProxy<TService>;

            var mce = method.Body as System.Linq.Expressions.MethodCallExpression;
            object[] args = new object[] { param1 };
            dynamic argObject = new ExpandoObject();
            var zippedArgs = mce.Method.GetParameters().Zip(args, (pi, val) => new { ParameterInfo = pi, Value = val });

            var argObjectAsDictionary = argObject as IDictionary<String, Object>;
            foreach (var arg in zippedArgs)
                argObjectAsDictionary.Add(arg.ParameterInfo.Name, arg.Value);

            string startUrl = $"{serviceProxy.UrlBase}/{method.Name}_Start";
            var sRes =   await ServiceProvider.GetService<HttpClient>().PostJsonAsync<IntelligentServices.Shared.StreamingResult<TResult>>(startUrl, (object)argObject);
            string continueUrl = $"{serviceProxy.UrlBase}/{method.Name}_Continue/" + sRes.RequestId;
    
            while (true)
            {
                yield return sRes;

                if (sRes.State == IntelligentServices.Shared.StreamingResultState.Complete)
                    break;
                if (sRes.State == IntelligentServices.Shared.StreamingResultState.Cancelled)
                    break;
                sRes = await ServiceProvider.GetService<HttpClient>().GetJsonAsync<IntelligentServices.Shared.StreamingResult<TResult>>(continueUrl);
            }
        }*/
    }

    public class ServiceProxy<T> : System.Reflection.DispatchProxyAsync
    {
        public IServiceProvider ServiceProvider { get; internal set; }

        public string UrlBase { get; private set; }
        public void Build()
        {
            UrlBase = $"/api/{typeof(T).Name.Substring(1)}";
        }
        public override async Task InvokeAsync(MethodInfo method, object[] args)
        {
            throw new NotImplementedException();
        }
        public override object Invoke(MethodInfo method, object[] args)
        {
            throw new NotImplementedException();
        }
        public override async Task<T> InvokeAsyncT<T>(MethodInfo method, object[] args)
        {
            dynamic argObject = new ExpandoObject();
            var zippedArgs = method.GetParameters().Zip(args, (pi, val) => new { ParameterInfo = pi, Value = val });

            var argObjectAsDictionary = argObject as IDictionary<String, Object>;
            foreach (var arg in zippedArgs)
                argObjectAsDictionary.Add(arg.ParameterInfo.Name, arg.Value);

            var result = await ServiceProvider.GetService<HttpClient>().PostJsonAsync<T>($"{UrlBase}/{method.Name}", (object)argObject);
            return
                result;
        }
        /*
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            ServiceProvider.GetService<HttpClient>().GetJsonAsync<Blaze6.Shared.WeatherForecast>($"/api/WeatherForecastService/{targetMethod.Name}").Result;

            throw new System.NotImplementedException();
        }*/
    }
    public static class ServiceBuilderExtensions
    {
        public static void AddServiceFromInterface<TServiceInterface>(this IServiceCollection services)
        {
            services.AddSingleton<ServiceBuilder>();
            services.AddSingleton<StreamingServiceManager>();

            services.AddSingleton(typeof(TServiceInterface), (sp) => sp.GetService<ServiceBuilder>().Build<TServiceInterface>(sp));
            //            sp.GetService<RestServiceBuilder>().Build<TServiceInterface>(sp));

        }
    }
    public class SignalRClient
    {
        System.IServiceProvider ServiceProvider;
        public HubConnection Connection { get; set; }
        public string Id { get; set; }
        public SignalRClient(System.IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

        }
        public async Task Start()
        {

            Connection = new HubConnectionBuilder()
            .WithUrlBlazor("/signalR", // The hub URL. If the Hub is hosted on the server where the blazor is hosted, you can just use the relative path.
            ServiceProvider.GetService<Microsoft.JSInterop.IJSRuntime>()
            )
            .Build(); // Build the HubConnection
            Connection.On<IntelligentServices.Shared.PromptMessage>("Prompt", this.HandlePrompt); // Subscribe to messages sent from the Hub to the "Receive" method by passing a handle (Func<object, Task>) to process messages.
            Connection.On<IntelligentServices.Shared.PromptMessage>("Alert", this.HandleAlert); // Subscribe to messages sent from the Hub to the "Receive" method by passing a handle (Func<object, Task>) to process messages.


            System.Diagnostics.Debug.WriteLine("Starting SigR client");
            await Connection.StartAsync(); // Start the connection.
            Id = await Connection.InvokeAsync<string>("GetConnectionId");
            System.Diagnostics.Debug.WriteLine("Get connection id " + Id);

            ServiceProvider.GetService<HttpClient>().DefaultRequestHeaders.Add("SignalRId", Id);

        }
        async void HandleAlert(IntelligentServices.Shared.PromptMessage promptMessage)
        {
            System.Diagnostics.Debug.WriteLine("HandleAlert");
            System.Diagnostics.Debug.WriteLine(promptMessage);
            IntelligentServices.Shared.PromptResult promptResult = default;
            /*
            await ServiceProvider.GetService<JsPromptService>().AlertAsync(promptMessage.Text);

            Blaze6.Shared.PromptResult promptResult = new Blaze6.Shared.PromptResult();
            promptResult.CloseButton = "OK";
            promptResult.CloseType = Blaze6.Shared.PromptCloseType.Button;*/
            await Connection.InvokeAsync("PromptReturn", promptMessage.WaitId, Json.Serialize(promptResult));
        }
        async void HandlePrompt(IntelligentServices.Shared.PromptMessage promptMessage)
        {
            System.Diagnostics.Debug.WriteLine("HandlePrompt");
            System.Diagnostics.Debug.WriteLine(promptMessage);
            IntelligentServices.Shared.PromptResult promptResult = default;
            /*
            var res = await ServiceProvider.GetService<JsPromptService>().PromptAsync(promptMessage.Text);

            Blaze6.Shared.PromptResult<Blaze6.Shared.AgePrompt> promptResult = new Blaze6.Shared.PromptResult<Blaze6.Shared.AgePrompt>();
            promptResult.CloseButton = "OK";
            promptResult.CloseType = Blaze6.Shared.PromptCloseType.Button;
            promptResult.PromptForm = new Blaze6.Shared.AgePrompt() { Age = Int32.Parse(res) };
            */
            await Connection.InvokeAsync("PromptReturn", promptMessage.WaitId, Json.Serialize(promptResult));
        }
    }
}
