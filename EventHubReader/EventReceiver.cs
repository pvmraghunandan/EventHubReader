// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventReceiver.cs" company="Microsoft"> 
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
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class EventReceiver
    {
        private EventHubClient eventHubClient = null;
        private EventHubConsumerGroup consumerGroup = null;
        private EventProcessorHost eventProcessorHost = null;

        public async Task InitializeEventProcessor(string connectionString, string eventHubName, string storageConnectionString, string path, string cGroup = null, string vin = null, string activityId = null)
        {
            Filter.BlockingCollection = new BlockingCollection<string>(int.MaxValue);
            Filter.EventDataCollection = new BlockingCollection<EventData>(int.MaxValue);
            Filter.Path = path;
            Filter.Vin = vin;
            Filter.ActivityId = activityId;
            this.eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);
            this.consumerGroup = string.IsNullOrWhiteSpace(cGroup)
                 ? eventHubClient.GetDefaultConsumerGroup()
                 : eventHubClient.GetConsumerGroup(cGroup);
            this.eventProcessorHost = new EventProcessorHost("EventHubReader", eventHubClient.Path, this.consumerGroup.GroupName, connectionString, storageConnectionString);
            await this.eventProcessorHost.RegisterEventProcessorAsync<EventProcessor>();
        }

        public async Task Disconnect()
        {
            if (this.eventProcessorHost != null)
            {
                await this.eventProcessorHost.UnregisterEventProcessorAsync();
                Filter.BlockingCollection.CompleteAdding();
                Filter.EventDataCollection.CompleteAdding();
            }
        }
    }
}