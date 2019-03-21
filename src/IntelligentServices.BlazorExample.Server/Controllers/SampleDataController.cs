using IntelligentServices.BlazorExample.Shared;
using IntelligentServices.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntelligentServices.BlazorExample.Server.Controllers
{
    [Route("api/[controller]")]
    public class SampleDataController : Controller
    {
        private static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet("[action]")]
        public IEnumerable<WeatherForecast> WeatherForecasts()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            });
        }
    }

    [Route("api/[controller]")]
    public class ExampleService : StreamControllerBase, IntelligentServices.BlazorExample.Shared.IExampleService
    {
        public ExampleService(IWebHostEnvironment hostingEnvironment, StreamingRuntimeContexts streamingRuntimeContexts, IHubContext<SigRHub> sigRHub) : base(hostingEnvironment, streamingRuntimeContexts, sigRHub)
        {
        }

        [HttpGet("[action]/{search}")]
        [HttpPost("[action]")]
        public async IAsyncEnumerable<string> ListWords(string search)
        {
            var res = RuntimeContexts.words.Where(w => search==null?true:w.StartsWith(search)).ToAsyncEnumerable();
            int i = 0;
            int nextBlock = 10;
            Random r = new Random();
           
            while (true)
            {
                nextBlock = r.Next(100, 200);
                var block = await res.Skip(i).Take(nextBlock).ToArrayAsync();
                if (block.Length == 0)
                {
                    break;
                }
                foreach (var item in block)
                {
                    yield return item;
                }
                i += nextBlock;
                if (i % 5 == 0)
                    await Task.Delay(250);
            }

        }

        [HttpGet("[action]/{name}")]
        [HttpPost("[action]")]
        public async Task<string> Conversation(string name)
        {
            var promptData = await Prompt($"Hi {name}, how old are you?", new[] { "Submit" }, new AgePrompt() { Age = 21 });

            var promptData2 = await Alert($"{name} is {promptData.PromptForm.Age}", new[] { "Submit" });

            if (promptData.PromptForm.Age > 35)
            {
                var promptData3 = await Prompt($"Etc...", new[] { "Submit" }, new Over35sPrompt() {   });
            } else
            {
                var promptData3 = await Prompt($"Etc...", new[] { "Submit" }, new Under35sPrompt() { });
            }

            return "Ends";
        }

        [HttpPost("[action]")]
        public async Task<string> SayHello(string name, Language language)
        {
            switch (language)
            {
                case Language.English:
                    return $"\"Hello {name}!\"";
                case Language.French:
                    return $"Salut! {name}";
                case Language.German:
                    return $"Hallo {name}";
                default:
                    throw new ArgumentException("Unknown language",nameof(language));
            }

        }
    }
}
