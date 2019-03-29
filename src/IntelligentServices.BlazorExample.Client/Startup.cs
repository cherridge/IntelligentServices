using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using IntelligentServices.Client;
using Blazored.Modal;
using IntelligentServices.Shared;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace IntelligentServices.BlazorExample.Client
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddBlazoredModal();
            services.AddServiceFromInterface<IntelligentServices.BlazorExample.Shared.IExampleService>();

            services.AddSingleton<IntelligentServices.Client.SignalRClient>();

            services.AddSingleton<IDialogService, DialogService>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
    public class DialogService : IDialogService
    {
        public async Task<ButtonBase> Alert(string title, string icon, string message, ButtonBase[] buttons)
        {

            var mServ = ServiceProvider.GetService<Blazored.Modal.Services.IModalService>();
            System.Diagnostics.Debug.WriteLine($"DialogService-Alert {title}");
            TaskCompletionSource<ButtonBase> tcs = new TaskCompletionSource<ButtonBase>();
            ButtonBase buttonResult = default;
            List<Components.Button> buttonsImpl = new List<Components.Button>();
            foreach (var buttonBase in buttons)
            {
                var button=new Components.Button()
                {
                    Title = buttonBase.Title,
                    Position = buttonBase.Position,

                };
                button.Action = () =>
                {
                    System.Diagnostics.Debug.WriteLine($"DialogService-Action {button.Title}");
                    buttonResult = button;
                    //                    tcs.SetResult(button);
                    mServ.Close();
                };
                buttonsImpl.Add(button);
            }

            ModalParameters modalParameters = new ModalParameters();
            modalParameters.Add("Message", "A test message");
            modalParameters.Add("Buttons", buttonsImpl.ToArray());
            Action onCloseAct = () =>
            {
                System.Diagnostics.Debug.WriteLine($"DialogService-OnClose");
                //Send dialogResult on...
                if (buttonResult == null)
                {

                    System.Diagnostics.Debug.WriteLine($"button result is null");
                    tcs.SetCanceled();
                    System.Diagnostics.Debug.WriteLine($"DialogService-OnClose - Result set. (cancelled)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"button result is NOT null");
                    tcs.SetResult(buttonResult);
                    System.Diagnostics.Debug.WriteLine($"DialogService-OnClose - Result set.");
                }
            };
            mServ.OnClose += onCloseAct;
            ServiceProvider.GetService<Blazored.Modal.Services.IModalService>().Show(title, icon, typeof(Components.Alert), modalParameters);
            try {
                return await tcs.Task;
            } finally
            {
                System.Diagnostics.Debug.WriteLine($"DialogService-Removing expired onclose handler");
                mServ.OnClose -= onCloseAct;
            }
        }
        public IServiceProvider ServiceProvider { get; internal set; }

        public DialogService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
    }
}
