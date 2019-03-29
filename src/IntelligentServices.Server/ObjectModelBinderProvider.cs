using Microsoft.AspNetCore.Mvc.ModelBinding;

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
}
