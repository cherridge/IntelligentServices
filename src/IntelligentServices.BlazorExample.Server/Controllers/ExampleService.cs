using IntelligentServices.BlazorExample.Shared;
using IntelligentServices.Server;
using IntelligentServices.Shared;
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
    public class ExampleService : StreamControllerBase, IntelligentServices.BlazorExample.Shared.IExampleService
    {

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
                    throw new ArgumentException("Unknown language", nameof(language));
            }

        }

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
        public async Task<ConversationResult> Conversation(string name)
        {
            var alertResult1 = await Alert("Title", "bell", "My Message", new ButtonBase[] {
                new ButtonBase() { Title= "Ok", Position= ButtonPosition.Primary},

                new ButtonBase() { Title= "Cancel", Position= ButtonPosition.Secondary}
            });
            if (alertResult1.Title == "Ok")
            {
                var alertResult2 = await Alert("Title", "bell", "My Message", new ButtonBase[] {
                new ButtonBase() { Title= "Ok", Position= ButtonPosition.Primary},

                new ButtonBase() { Title= "Cancel", Position= ButtonPosition.Secondary}
            });
            }
            else
            {
                var alertResult3 = await Alert("Title2", "bluetooth", "Another Message", new ButtonBase[] {
                new ButtonBase() { Title= "Ok", Position= ButtonPosition.Primary}
            });
            }

            //  var promptData = await Prompt($"Hi {name}, how old are you?", new[] { "Submit","Cancel" }, new AgePrompt() { Age = 21 });

            /*
            var promptData2 = await Alert($"{name} is {promptData.PromptForm.Age}", new[] { "Submit" });

            if (promptData.PromptForm.Age > 35)
            {
                var promptData3 = await Prompt($"Etc...", new[] { "Submit" }, new Over35sPrompt() {   });
            } else
            {
                var promptData3 = await Prompt($"Etc...", new[] { "Submit" }, new Under35sPrompt() { });
            }*/

            return new ConversationResult() { ADate = DateTime.UtcNow, AString = "Ends", SubTypes = new[] { new ConversationResultSubType() { aBoolean = false }, new ConversationResultSubType() { aBoolean = true }, new ConversationResultSubType() { aBoolean = false }, new ConversationResultSubType() { aBoolean = true } } };
        }

       
    }
}
