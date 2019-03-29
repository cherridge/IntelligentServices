namespace IntelligentServices.BlazorExample.Shared
{
    public class AgePrompt
    {
        [System.ComponentModel.DataAnnotations.Range(12, 110)]
        public int Age { get; set; }
    }
}
