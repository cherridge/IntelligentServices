using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

using Microsoft.Extensions.DependencyInjection;
namespace IntelligentServices.Client
{
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
}
