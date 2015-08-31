// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogEventData.cs" company="Microsoft"> 
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
    #region Using Directives

    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;
    #endregion

    public enum EventDataDirection
        {
            Send,
            Receive
        }

        public class LogEventDataInspector : IEventDataInspector, IDisposable
        {
            #region Private Constants
            //***************************
            // Constants
            //***************************
            private const string DateFormat = "<{0,2:00}:{1,2:00}:{2,2:00}> {3}";
            private const string NullValue = "NULL";
            private const string EventDataPropertiesHeader = "Properties:";
            private const string EventDataPayloadHeader = "Payload:";
            private const string EventDataPropertyFormat = " - Key=[{0}] Value=[{1}]";
            private const string LogFileNameFormat = "EventData {0}.json";
            private const string LogFileNamePropertiesFormat = "EventData_Properties {0}.json";
            private static readonly string line = new string('-', 100);
            #endregion

            #region Private Instance Fields

            private  Task writeTask ;
            #endregion

            #region Private Static Fields
            private BlockingCollection<Tuple<EventDataDirection, EventData>> messageCollection;
            private string path;
            #endregion

            public EventData AfterReceiveMessage(EventData eventData)
            {
                return LogEventData(EventDataDirection.Receive, eventData);
            }

            public void StartTask(CancellationToken token, string path)
            {
                this.path = path;
            messageCollection = new BlockingCollection<Tuple<EventDataDirection, EventData>>(int.MaxValue);
            writeTask = new Task(
                WriteToLog, token);
                writeTask.Start();
            }

            #region IDisposable Methods
            public void Dispose()
            {
                messageCollection.CompleteAdding();
                writeTask.Wait();
            }
            #endregion

            #region Private Static Methods
            private EventData LogEventData(EventDataDirection direction, EventData eventData)
            {
                try
                {
                    if (eventData != null)
                    {
                        messageCollection.TryAdd(new Tuple<EventDataDirection, EventData>(direction, eventData.Clone()));
                    }
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                }
                return eventData;
            }

            private async void WriteToLog()
            {
                try
                {
                    foreach (var tuple in messageCollection.GetConsumingEnumerable())
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(50));
                        if (tuple != null && tuple.Item2 != null)
                        {
                            var guid = Guid.NewGuid().ToString();
                            var eventData = tuple.Item2;
                            try
                            {
                                using (var writer = new StreamWriter(new FileStream(Path.Combine(this.path,
                                                                           string.Format(LogFileNameFormat,
                                                                                         guid)),
                                                              FileMode.Append,
                                                              FileAccess.Write,
                                                              FileShare.ReadWrite)))
                                    {
                                        //var direction = tuple.Item1;
                                        await writer.WriteAsync(Encoding.UTF8.GetString(eventData.GetBytes()));
                                        await writer.FlushAsync();
                                    }
                                using (var writer = new StreamWriter(new FileStream(Path.Combine(this.path,
                                                                       string.Format(LogFileNamePropertiesFormat,
                                                                                     guid)),
                                                          FileMode.Append,
                                                          FileAccess.Write,
                                                          FileShare.ReadWrite)))
                                {
                                    var now = DateTime.Now;
                                    var builder = new StringBuilder();
                                    builder.AppendLine(string.Format(DateFormat,
                                        now.Hour,
                                        now.Minute,
                                        now.Second,
                                        line));
                                    builder.AppendLine(string.Format(CultureInfo.CurrentCulture,
                                    
                                        string.IsNullOrWhiteSpace(eventData.PartitionKey) ? NullValue : eventData.PartitionKey));
                                    builder.AppendLine(EventDataPayloadHeader);
                                    if (eventData.Properties.Any())
                                    {
                                        builder.AppendLine(EventDataPropertiesHeader);
                                        foreach (var p in eventData.Properties)
                                        {
                                            builder.AppendLine(string.Format(EventDataPropertyFormat,
                                                p.Key,
                                                p.Value));
                                        }
                                    }
                                    await writer.WriteAsync(builder.ToString());
                                        await writer.FlushAsync();
                                }
                            }

                            // ReSharper disable once EmptyGeneralCatchClause
                            catch
                            {
                            }
                        }
                    }
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
                finally
                {
                    messageCollection.Dispose();
                }
            }
        #endregion
    }
}