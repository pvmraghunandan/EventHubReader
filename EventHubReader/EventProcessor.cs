// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventProcessor.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventHubReader
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    class EventProcessor : IEventProcessor
    {
            private PartitionContext partitionContext;
            private Stopwatch checkpointStopWatch;
            private Filter filter;
            private IEventDataInspector eventDataInspector;
            private CancellationTokenSource cancellationTokenSource;

        public EventProcessor()
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                this.eventDataInspector = new LogEventDataInspector();
                this.eventDataInspector.StartTask(cancellationTokenSource.Token, Filter.Path);
            }

            public async Task CloseAsync(PartitionContext context, CloseReason reason)
            {
                // Console.WriteLine(string.Format("Processor Shuting Down.  Partition '{0}', Reason: '{1}'.", this.partitionContext.Lease.PartitionId, reason.ToString()));
                if (reason == CloseReason.Shutdown)
                {
                    await context.CheckpointAsync();
                    this.cancelTask();
                }
            }

            public Task OpenAsync(PartitionContext context)
            {
                // Console.WriteLine(string.Format("SimpleEventProcessor initialize.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset));
                this.partitionContext = context;
                this.checkpointStopWatch = new Stopwatch();
                this.checkpointStopWatch.Start();
                return Task.FromResult<object>(null);
            }

            private void cancelTask()
            {
                this.cancellationTokenSource.Cancel();
            }

            public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                try
                {
                    foreach (EventData lobjData in messages)
                    {
                        var message = string.Format("Message received.  Partition: '{0}', Offset: '{1}' , SequenceNumber: '{2}'", partitionContext.Lease.PartitionId, lobjData.Offset, lobjData.SequenceNumber);
                        if (!string.IsNullOrWhiteSpace(Filter.Vin))
                        {
                            if (lobjData.Properties[Constants.VehicleInformationKey].ToString().Equals(Filter.Vin))
                            {
                                this.eventDataInspector.AfterReceiveMessage(lobjData);
                                Filter.InsertIntoBlockingCollection(message);
                            }
                        }
                        else
                        {
                            this.eventDataInspector.AfterReceiveMessage(lobjData);
                            Filter.InsertIntoBlockingCollection(message);
                        }

                    }
                    //Call checkpoint every 5 minutes, so that worker can resume processing from the 5 minutes back if it restarts.
                    if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
                    {
                        await context.CheckpointAsync();
                        lock (this)
                        {
                            this.checkpointStopWatch.Reset();
                        }
                    }
                }
                catch (Exception lobjException)
                {
                    Console.WriteLine(lobjException.Message);
                }
            }
        }
    }