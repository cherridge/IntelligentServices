using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

using Microsoft.Extensions.DependencyInjection;
namespace IntelligentServices.Client
{
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
}
