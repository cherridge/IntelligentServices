using System;

namespace IntelligentServices.Shared
{
    public interface IStreamingResult
    {

        StreamingResultState State { get; set; }
        string Status { get; set; }
        Exception Exception { get; set; }
        string RequestId { get; set; }
        int Index { get; set; }
        int Count { get; set; }
    }

    public interface IDialogService
    {
        System.Threading.Tasks.Task<ButtonBase> Alert(string title, string icon, string message, ButtonBase[] buttons);
    }
    public class ButtonBase
    {
        public string Title { get; set; }
        public ButtonPosition Position { get; set; }
    }
    public enum ButtonPosition
    {
        Unset,
        Primary,
        Secondary
    }
}
