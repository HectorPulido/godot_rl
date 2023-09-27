using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace rl_client
{

    public class ClientRL
    {
        private ClientWebSocket clientWebSocket;

        private readonly string serverAddress;
        private bool threadLock;

        public ClientRL(string serverAddress)
        {
            this.serverAddress = serverAddress;
            threadLock = false;
        }

        public async Task Start()
        {
            Console.WriteLine("Connecting to server: ", serverAddress);
            clientWebSocket = new ClientWebSocket();
            await clientWebSocket.ConnectAsync(new Uri(serverAddress), CancellationToken.None);
            Console.WriteLine("Connected to server");
        }


        private async Task GracefulShutdown()
        {
            if (clientWebSocket.State == WebSocketState.Open)
            {
                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }

        public async Task<J> SendGenericData<T, J>(T message)
        {
            // while (threadLock)
            // {
            //     Console.WriteLine("Waiting for thread lock");
            //     await Task.Delay(10);
            // }
            // threadLock = true;
            string jsonMessage = JsonConvert.SerializeObject(message);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
            await clientWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Message sent: {message}");

            byte[] buffer = new byte[1024];
            var receiveResult = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                Console.WriteLine($"Message received: {receivedMessage}");
                threadLock = false;
                return JsonConvert.DeserializeObject<J>(receivedMessage);
            }

            // threadLock = false;
            Debug.WriteLine("No response received");
            return default(J);
        }
    }
}