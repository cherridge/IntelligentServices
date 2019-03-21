using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using System.Linq;

namespace IntelligentServices.BlazorExample.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IntelligentServices.Server.StreamingRuntimeContexts>();
            services.AddMvc(options => {
                options.ModelBinderProviders.Insert(
                     0, new IntelligentServices.Server.ObjectModelBinderProvider());
                options.ValueProviderFactories.Insert(0, new IntelligentServices.Server.CustomHttpBodyValueProviderFactory());
                options.Conventions.Add(new IntelligentServices.Server.StreamingRoutingConvention());
            }).AddNewtonsoftJson().AddControllersAsServices();

            services.AddSignalR();
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBlazorDebugging();
            }

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "default", template: "{controller}/{action}/{id?}");
            });

            app.UseSignalR(routes =>
            {
                routes.MapHub<IntelligentServices.Server.SigRHub>("/signalR");
            });
            app.UseBlazor<Client.Startup>();
        }
    }
}
