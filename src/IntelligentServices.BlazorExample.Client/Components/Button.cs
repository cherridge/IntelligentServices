using Blazored.Modal;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntelligentServices.BlazorExample.Client.Components
{
    public class Button : IntelligentServices.Shared.ButtonBase
    {
        public Action Action { get; set; }
    }
    public class DialogBase : ComponentBase
    {
        [CascadingParameter] protected ModalParameters Parameters { get; set; }

        [Parameter]
        protected Button[] Buttons { get; set; }
        protected override void OnInit()
        {
            base.OnInit();
            Buttons = Parameters.Get<Button[]>("Buttons");
        }
    }
    public class AlertBase : DialogBase
    {
        protected override void OnInit()
        {
            base.OnInit();
            Message = Parameters.Get<string>("Message");
        }
        [Parameter]
        protected string Message { get; set; }
    }
}
