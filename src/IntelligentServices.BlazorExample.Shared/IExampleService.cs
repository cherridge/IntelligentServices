using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntelligentServices.BlazorExample.Shared
{
    public interface IExampleService
    {
        Task<string> SayHello(string name, Language language);

        IAsyncEnumerable<string> ListWords(string search);

        Task<ConversationResult> Conversation(string name);
    }

    public class ConversationResult
    {
        public string AString { get; set; }
        public System.DateTime ADate { get; set; }
        public ConversationResultSubType[] SubTypes { get; set; }
    }
    public class ConversationResultSubType
    {
        public bool aBoolean { get; set; }
    }
}
