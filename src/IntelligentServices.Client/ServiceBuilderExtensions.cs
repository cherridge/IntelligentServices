using Microsoft.Extensions.DependencyInjection;

namespace IntelligentServices.Client
{
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
}
