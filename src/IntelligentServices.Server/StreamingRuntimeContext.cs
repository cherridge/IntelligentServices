using System;
using IntelligentServices.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntelligentServices.Server
{
    public class StreamingRuntimeContext<T> : IStreamingRuntimeContext
    {
        const int maxSize = 64;
        const int waitMaxMs = 1000 * 60 * 2;
        const int processMaxMs = 500;
        public IStreamingResult CreateResult(bool allowEmpty)
        {
            StreamingResult<T> streamingResult = new StreamingResult<T>();
            streamingResult.Exception = Exception;

            streamingResult.RequestId = RequestId;
            if (State == StreamingResultState.Query || State == StreamingResultState.QueryRead || State == StreamingResultState.Read || State == StreamingResultState.Complete)
            {
                List<T> resItems = new List<T>();
                try
                {


                    if (Items.TryTake(out T item, allowEmpty ? 0 : waitMaxMs, ct))
                    {
                        var start = DateTime.UtcNow; ;
                        resItems.Add(item);
                        while (true)
                        {

                            if (Items.TryTake(out T item2, (DateTime.UtcNow - start).TotalMilliseconds > processMaxMs ? 0 : processMaxMs, ct))
                            {
                                resItems.Add(item2);
                            }
                            else
                            {
                                break;
                            }
                            if (resItems.Count == maxSize)
                            {
                                break;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {

                    State = StreamingResultState.Cancelled;
                    streamingResult.Index = Index;
                    streamingResult.Count = Count;
                    streamingResult.State = State;
                    return streamingResult;
                }

                streamingResult.Items = resItems.ToArray();
                Index += streamingResult.Items.Length;
                if (Items.IsCompleted)
                {

                    State = StreamingResultState.Complete;
                    Count = Index + Items.Count;
                }
                else
                if (Items.IsAddingCompleted)
                {
                    State = StreamingResultState.Read;
                    Count = Index + Items.Count;
                }

            }

            streamingResult.Index = Index;
            streamingResult.Count = Count;
            streamingResult.State = State;
            return streamingResult;
        }
        public StreamingResultState State { get; set; }
        public Exception Exception { get; set; }

        public IAsyncEnumerable<T> AsyncEnumerable { get; set; }
        public string RequestId { get; set; }

        int Index;
        int Count;

        System.Threading.CancellationTokenSource cts;
        System.Threading.CancellationToken ct;

        Task RunTask;
        System.Collections.Concurrent.BlockingCollection<T> Items { get; } = new System.Collections.Concurrent.BlockingCollection<T>();
        public void Run()
        {
            cts = new System.Threading.CancellationTokenSource();
            ct = cts.Token;
            RunTask = Task.Run(DoRun, ct);
        }
        bool checkCancellation()
        {
            if (ct.IsCancellationRequested)
            {
                State = StreamingResultState.Cancelled;
                return true;
            }
            return false;
        }
        async void DoRun()
        {
            try
            {
                State = StreamingResultState.Query;
                var asyncEn = AsyncEnumerable;
                if (checkCancellation())
                    return;

                State = StreamingResultState.QueryRead;
                await foreach (var item in asyncEn)
                {
                    Items.Add(item);
                    if (checkCancellation())
                        return;
                }
                Items.CompleteAdding();
                State = StreamingResultState.Read;
            }
            catch (Exception ex)
            {
                State = StreamingResultState.Errored;
                Exception = ex;
            }
        }

        public void Cancel()
        {
            cts.Cancel();
        }
    }
}
