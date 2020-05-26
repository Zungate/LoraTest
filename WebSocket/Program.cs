using System;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;

using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Websocket.Client;

namespace Websocket
{
    class Program
    {
        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var factory = new Func<ClientWebSocket>(() =>
            {
                var client = new ClientWebSocket
                {
                    Options =
                    {
                        KeepAliveInterval = TimeSpan.FromSeconds(5),
                        // Proxy = ...
                        // ClientCertificates = ...
                    }
                };
                //client.Options.SetRequestHeader("Origin", "xxx");
                return client;
            });

            var url = new Uri("wss://iotnet.teracom.dk/app?token=vnoSywAAABFpb3RuZXQudGVyYWNvbS5ka2rsDK-oV3FsspLZBx8Hh9k=");

            using (IWebsocketClient client = new WebsocketClient(url, factory))
            {
                client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                client.ErrorReconnectTimeout = TimeSpan.FromSeconds(30);
                client.ReconnectionHappened.Subscribe(type =>
                {
                    Console.WriteLine($"Reconnection happened, type: {type}, url: {client.Url}");
                });
                client.DisconnectionHappened.Subscribe(info =>
                    Console.WriteLine($"Disconnection happened, type: {info.Type}"));

                client.MessageReceived.Subscribe(msg =>
                {
                    Console.WriteLine($"Message received: {msg}");
                });

                Console.WriteLine("Starting...");
                client.Start().Wait();
                Console.WriteLine("Started.");

                Task.Run(() => StartSendingPing(client));

                ExitEvent.WaitOne();
            }

            Console.WriteLine("====================================");
            Console.WriteLine("              STOPPING              ");
            Console.WriteLine("====================================");
            Log.CloseAndFlush();
        }

        private static async Task StartSendingPing(IWebsocketClient client)
        {
            while (true)
            {
                await Task.Delay(1000);

                if (!client.IsRunning)
                    continue;

                client.Send("ping");
            }
        }

       
    }
}