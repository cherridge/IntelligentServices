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
}
