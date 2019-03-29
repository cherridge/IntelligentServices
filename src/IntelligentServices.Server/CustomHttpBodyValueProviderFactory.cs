using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http.Internal;
using System.IO;
using System.Globalization;
using System.Text;

namespace IntelligentServices.Server
{
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
}
