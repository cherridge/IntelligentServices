using System;

namespace IntelligentServices.Shared
{
    public interface IDialogMessage
    {
        string WaitId { get; set; }
        ButtonBase[] Buttons { get; set; }
    }

    public class PromptMessage: IDialogMessage
    {
        public string WaitId { get; set; }
        public string Text { get; set; }

        public ButtonBase[] Buttons { get; set; }
        public object PromptForm { get; set; }
    }
    public class AlertMessage : IDialogMessage
    {
        public string WaitId { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Message { get; set; }

        public ButtonBase[] Buttons { get; set; }
    }
}
