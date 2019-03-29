using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IntelligentServices.Server
{
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
}
