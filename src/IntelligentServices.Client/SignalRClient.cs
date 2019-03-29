using System.Net.Http;
using System.Threading.Tasks;
using BlazorSignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Blazor;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using System;

namespace IntelligentServices.Client
{
    public class SignalRClient
    {

        System.IServiceProvider ServiceProvider;
        public HubConnection Connection { get; set; }
        public string Id { get; set; }
        public SignalRClient(System.IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

        }
        public async Task Start()
        {
            if (Connection != null)
                return ;

            Connection = new HubConnectionBuilder()
            .WithUrlBlazor("/signalR", // The hub URL. If the Hub is hosted on the server where the blazor is hosted, you can just use the relative path.
            ServiceProvider.GetService<Microsoft.JSInterop.IJSRuntime>()
            )
            .Build(); // Build the HubConnection
            Connection.On<IntelligentServices.Shared.PromptMessage>("Prompt", this.HandlePrompt); // Subscribe to messages sent from the Hub to the "Receive" method by passing a handle (Func<object, Task>) to process messages.

            Connection.On<IntelligentServices.Shared.AlertMessage>("Alert", this.HandleAlert); // Subscribe to messages sent from the Hub to the "Receive" method by passing a handle (Func<object, Task>) to process messages.


            System.Diagnostics.Debug.WriteLine("Starting SigR client");
            await Connection.StartAsync(); // Start the connection.
            Id = await Connection.InvokeAsync<string>("GetConnectionId");
            System.Diagnostics.Debug.WriteLine("Get connection id " + Id);

            ServiceProvider.GetService<HttpClient>().DefaultRequestHeaders.Add("SignalRId", Id);

        }

        async void HandlePrompt(IntelligentServices.Shared.PromptMessage promptMessage)
        {
            System.Diagnostics.Debug.WriteLine("HandlePrompt");
            System.Diagnostics.Debug.WriteLine(promptMessage);
            IntelligentServices.Shared.PromptResult promptResult = default;
            /*
            var res = await ServiceProvider.GetService<JsPromptService>().PromptAsync(promptMessage.Text);

            Blaze6.Shared.PromptResult<Blaze6.Shared.AgePrompt> promptResult = new Blaze6.Shared.PromptResult<Blaze6.Shared.AgePrompt>();
            promptResult.CloseButton = "OK";
            promptResult.CloseType = Blaze6.Shared.PromptCloseType.Button;
            promptResult.PromptForm = new Blaze6.Shared.AgePrompt() { Age = Int32.Parse(res) };
            */
            await Task.Delay(System.TimeSpan.FromSeconds(10));

            await ServiceProvider.GetService<Microsoft.JSInterop.IJSRuntime>().InvokeAsync<object>("alert", 1);
            await Task.Delay(System.TimeSpan.FromSeconds(10));

            await ServiceProvider.GetService<Microsoft.JSInterop.IJSRuntime>().InvokeAsync<object>("alert", 2);
            await Connection.InvokeAsync("PromptReturn", promptMessage.WaitId, Json.Serialize(promptResult));
        }
        async void HandleAlert(IntelligentServices.Shared.AlertMessage alertMessage)
        {
            System.Diagnostics.Debug.WriteLine("HandleAlert");
            System.Diagnostics.Debug.WriteLine(alertMessage);


            var jsonToSend = string.Empty;
            try
            {
                var clickedButton = await ServiceProvider.GetService<Shared.IDialogService>().Alert(alertMessage.Title, alertMessage.Icon, alertMessage.Message, alertMessage.Buttons);

                jsonToSend = Json.Serialize(new { clickedButton.Title });
            } catch (System.OperationCanceledException cancelledEx)
            {

                System.Diagnostics.Debug.WriteLine($"OperationCanceledException {cancelledEx}");
            } catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine($"Exception {ex}");
            }

            System.Diagnostics.Debug.WriteLine($"Sending Alert return... {alertMessage.WaitId} {jsonToSend}");
            await Connection.InvokeAsync("AlertReturn", alertMessage.WaitId, jsonToSend);
        }
    }
}
