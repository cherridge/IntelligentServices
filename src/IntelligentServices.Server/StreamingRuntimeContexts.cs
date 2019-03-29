using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;

namespace IntelligentServices.Server
{
    public class StreamingRuntimeContexts
    {
        public System.Collections.Concurrent.ConcurrentDictionary<string, IWaitingContext> Waits { get; } = new System.Collections.Concurrent.ConcurrentDictionary<string, IWaitingContext>();

        public System.Collections.Concurrent.ConcurrentDictionary<string, IStreamingRuntimeContext> Contexts { get; } = new System.Collections.Concurrent.ConcurrentDictionary<string, IStreamingRuntimeContext>();
        public List<string> words { get; set; }
        public IWebHostEnvironment HostingEnvironment { get; }
        public StreamingRuntimeContexts(IWebHostEnvironment hostingEnvironment)
        {

            HostingEnvironment = hostingEnvironment;
            words = new List<string>();
            using (var sr = new System.IO.StreamReader(hostingEnvironment.WebRootFileProvider.GetFileInfo("words.txt").CreateReadStream()))
            {
                while (!sr.EndOfStream)
                {
                    words.Add(sr.ReadLine());
                }

            }
        }
    }
}
