using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentServices.BlazorExample.Shared
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public string Summary { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }

    public interface IExampleService
    {
        Task<string> SayHello(string name, Language language);

        IAsyncEnumerable<string> ListWords(string search);

        Task<string> Conversation(string name);
    }
    public enum Language
    {
        English,
        French,
        German
    }
    public class AgePrompt
    {
        [System.ComponentModel.DataAnnotations.Range(12, 110)]
        public int Age { get; set; }
    }
    public class Over35sPrompt
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Field1 { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string Field2 { get; set; }
        public bool Field3 { get; set; }
    }
    public class Under35sPrompt
    {
        public string Field1 { get; set; }
        public bool Field3 { get; set; }
    }
}
