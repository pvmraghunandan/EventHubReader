// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Filter.cs" company="Microsoft"> 
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
//   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//   OTHER DEALINGS IN THE SOFTWARE. 
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.ServiceBus.Messaging;

namespace EventHubReader
{
    public class Filter
    {
        public static string ConsumerGroup { get; set; }

        public static DateTime ArrivalTime { get; set; }

        public static string Vin { get; set; }

        public static string ActivityId { get; set; }

        public static string Path { get; set; }

        public static BlockingCollection<string> BlockingCollection;

        public static BlockingCollection<EventData> EventDataCollection;

        private static object receiverLock = new object();

        public static void InsertIntoBlockingCollection(string logMessage)
        {
            Monitor.Enter(receiverLock);
            Filter.BlockingCollection.Add(logMessage);
            Monitor.Exit(receiverLock);
        }

        public static void InsertIntoEventDataCollection(EventData data)
        {
            Monitor.Enter(receiverLock);
            Filter.EventDataCollection.Add(data);
            Monitor.Exit(receiverLock);
        }

    }
}