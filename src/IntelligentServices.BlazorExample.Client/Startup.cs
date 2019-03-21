using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using IntelligentServices.Client;
namespace IntelligentServices.BlazorExample.Client
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddServiceFromInterface<IntelligentServices.BlazorExample.Shared.IExampleService>();

            services.AddSingleton<IntelligentServices.Client.SignalRClient>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
