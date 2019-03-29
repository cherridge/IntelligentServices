using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;

namespace IntelligentServices.Server
{
    public class StreamingRoutingConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                Func<IntelligentServices.Shared.IStreamingResult> testF = () => {

                    return null;
                };

                var sr_start = controller.ControllerType.GetMethod("SR_Start");
                var sr_continue = controller.ControllerType.GetMethod("SR_Continue");
                var sr_cancel = controller.ControllerType.GetMethod("SR_Cancel");

                foreach (var action in controller.Actions.Where(a => a.ActionMethod.ReturnType.IsGenericType && a.ActionMethod.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)).ToArray())
                {
                    var startA = new ActionModel(sr_start.MakeGenericMethod(action.ActionMethod.ReturnType.GenericTypeArguments[0]), action.Attributes);
                    var paraInfo = startA.ActionMethod.GetParameters()[0];
                    var para1 = new ParameterModel(paraInfo, new List<object>());
                    para1.ParameterName = paraInfo.Name;

                    Action<ModelBindingContext> vp = async (context) => {
                        var controllerInst = context.HttpContext.RequestServices.GetRequiredService(action.ActionMethod.DeclaringType);
                       
                        var args = action.ActionMethod.GetParameters().Select(para => new { para = para, inString = context.ValueProvider.GetValue(para.Name) }).ToArray();

                        var argsTyped = args.Select(a => Convert.ChangeType(a.inString.FirstValue, a.para.ParameterType)).ToArray();



                        var asyncE = action.ActionMethod.Invoke(controllerInst, argsTyped);

                        context.Result = ModelBindingResult.Success(asyncE);
                    };

                    startA.Properties.Add("StreamingParameterConverter", vp);
                    para1.BindingInfo = new BindingInfo();
                    para1.BindingInfo.BinderType = typeof(ObjectModelBinder);
                    // para1.BindingInfo.
                    startA.Parameters.Add(para1);
                    foreach (var sel in action.Selectors)
                    {
                        startA.Selectors.Add(sel);
                    }
                    // startA..Add(action.Selectors[0]);
                    startA.Controller = controller;
                    startA.ActionName = action.ActionName + "_Start";

                    controller.Actions.Add(startA);

                    var continueA = new ActionModel(sr_continue.MakeGenericMethod(action.ActionMethod.ReturnType.GenericTypeArguments[0]), action.Attributes);
                    continueA.Controller = controller;
                    continueA.ActionName = action.ActionName + "_Continue";

                    var cont_paraInfo = sr_continue.GetParameters()[0];
                    var cont_para = new ParameterModel(cont_paraInfo, new List<object>());
                    cont_para.ParameterName = cont_paraInfo.Name;

                    continueA.Parameters.Add(cont_para);
                    var contselectorModel = new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel() { Template = "[action]/{" + cont_paraInfo.Name + "}" }
                    };
                    continueA.Selectors.Add(contselectorModel);
                    controller.Actions.Add(continueA);

                    var cancelA = new ActionModel(sr_cancel, action.Attributes);
                    cancelA.Controller = controller;
                    cancelA.ActionName = action.ActionName + "_Cancel";


                    var cancel_paraInfo = sr_cancel.GetParameters()[0];
                    var cancel_para = new ParameterModel(cancel_paraInfo, new List<object>());
                    cancel_para.ParameterName = cancel_paraInfo.Name;

                    cancelA.Parameters.Add(cancel_para);
                    var cancselectorModel = new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel() { Template = "[action]/{" + cancel_paraInfo.Name + "}" }
                    };

                    cancelA.Selectors.Add(cancselectorModel);
                    controller.Actions.Add(cancelA);

                    //controller.Actions.Add(new ActionModel(action) { ActionName = action.ActionName + "_Start" });
                    //controller.Actions.Add(new ActionModel(action) { ActionName = action.ActionName + "_Continue", Parameters=new[] { new ParameterModel( } });
                    //controller.Actions.Add(new ActionModel(action) { ActionName = action.ActionName + "_Cancel" });

                }
            }

            // You can continue to put attribute route templates for the controller actions depending on the way you want them to behave
        }
    }
}
