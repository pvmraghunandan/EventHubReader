﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace EventHubReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EventHubClient eventHubClient = null;
        private EventHubConsumerGroup consumerGroup = null;
        private EventProcessorHost eventProcessorHost = null;
        private CancellationTokenSource cancellationToken;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Eh_Connect(object sender, RoutedEventArgs e)
        {
            var eventHubConnectionString = ConfigurationManager.AppSettings["EventHub.ConnectionString"];
            var storageConnectionString = ConfigurationManager.AppSettings["storage"];
            if (string.IsNullOrWhiteSpace(eventHubConnectionString))
            {
                MessageBox.Show("Enter valid EventHub Connection string", "Missing!!", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            else if (string.IsNullOrWhiteSpace(storageConnectionString))
            {
                MessageBox.Show("Enter valid Storage Connection string", "Missing!!", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            else if (string.IsNullOrWhiteSpace(txtPath.Text))
            {
                WriteTraceMessage("Enter Valid Path");
            }

            else if (!Directory.Exists(txtPath.Text))
            {
                WriteTraceMessage("Path doesnt exist");
            }

            else if (string.IsNullOrWhiteSpace(txtEN.Text))
            {
                WriteTraceMessage("Event Hub Name is Empty");
            }

            else
            {
                Filter.Path = txtPath.Text;
                Filter.Vin = txtVIN.Text;
                try
                {
                    Filter.BlockingCollection = new BlockingCollection<string>(int.MaxValue);
                    this.cancellationToken = new CancellationTokenSource();
                    this.txtBox.Text = "Connecting....";
                    this.progressBar.Visibility = Visibility.Visible;
                    this.eventHubClient = EventHubClient.CreateFromConnectionString(eventHubConnectionString, txtEN.Text);
                   this.consumerGroup = string.IsNullOrWhiteSpace(txtCG.Text)
                        ? eventHubClient.GetDefaultConsumerGroup()
                        : eventHubClient.GetConsumerGroup(txtCG.Text);
                    this.eventProcessorHost = new EventProcessorHost("EventHubReader", eventHubClient.Path, this.consumerGroup.GroupName, eventHubConnectionString, storageConnectionString);
                    await this.eventProcessorHost.RegisterEventProcessorAsync<EventProcessor>();
                    this.btnConnect.IsEnabled = false;
                    this.btnDisconnect.IsEnabled = true;
                    this.txtBox.Text = "Connected";
                    this.progressBar.Visibility = Visibility.Hidden;
#pragma warning disable 4014
                    Task.Run(() =>
#pragma warning restore 4014
                    {
                        foreach (var message in Filter.BlockingCollection.GetConsumingEnumerable())
                        {
                            WriteTraceMessage(message);
                        }
                    }, cancellationToken.Token);

                }
                catch (Exception)
                {
                    this.txtBox.Text = "Disconnected";
                    this.progressBar.Visibility = Visibility.Hidden;
                    WriteTraceMessage("Exception during Event hub initialization. Please check the URL");
                }
            }
        }

        private async void Eh_Disconnect(object sender, RoutedEventArgs e)
        {
            if (this.eventProcessorHost != null)
            {
                try
                {
                    this.txtBox.Text = "Disconnecting";
                    this.progressBar.Visibility = Visibility.Visible;
                    await eventProcessorHost.UnregisterEventProcessorAsync(); 
                }
                catch (Exception)
                {
                    WriteTraceMessage("Error in Disconnecting");

                }
                finally
                {
                    this.btnConnect.IsEnabled = true;
                    this.btnDisconnect.IsEnabled = false;
                    this.progressBar.Visibility = Visibility.Hidden;
                    this.txtBox.Text = "Disconnected";
                    Filter.BlockingCollection.CompleteAdding();
                    cancellationToken.Cancel();
                }
            }
        }

        private void WriteTraceMessage(string message)
        {
            Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                                 new Action(delegate ()
                                 {
                                     txtTrace.Text += (Environment.NewLine + DateTime.Now.ToLocalTime() + " " + message);
                                 }
         ));
        }

        private void btnClearTrace_Click(object sender, RoutedEventArgs e)
        {
            txtTrace.Text = string.Empty;
        }
    }
}