using System.Net.Sockets;
using System.Net;
using System.Text;
using ApiInterface.InternalModels;
using System.Text.Json;
using ApiInterface.Exceptions;
using ApiInterface.Processors;
using ApiInterface.Models;

namespace ApiInterface
{
    public class Server
    {
        private static IPEndPoint serverEndPoint = new(IPAddress.Loopback, 11000);
        private static int supportedParallelConnections = 1;
        private const string eomToken = "<EOM>";

        public static async Task Start()
        {
            using Socket listener = new(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(serverEndPoint);
            listener.Listen(supportedParallelConnections);
            Console.WriteLine($"Server ready at {serverEndPoint.ToString()}");

            while (true)
            {
                var handler = await listener.AcceptAsync();
                try
                {
                    var rawMessage = await GetMessage(handler);
                    var requestObject = await ConvertToRequestObject(rawMessage);
                    var response = ProcessRequest(requestObject);
                    await SendResponse(response, handler);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    await SendErrorResponse("Unknown exception", handler);
                }
                finally
                {
                    handler.Close();
                }
            }
        }

        private static async Task<string> GetMessage(Socket handler)
        {
            var buffer = new byte[1024];
            var response = new StringBuilder();
            var received = -1;
            var eom = false;           
            do
            {
                received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                var partialResponse = Encoding.UTF8.GetString(buffer, 0, received);
                if (partialResponse.EndsWith(eomToken))
                {
                    partialResponse = partialResponse.Remove(partialResponse.Length - eomToken.Length);
                    eom = true;
                }
                response.Append(partialResponse);                
            }
            while (!eom && received > 0);

            return response.ToString();
        }

        private static Task<Request> ConvertToRequestObject(string rawMessage)
        {
            return Task.FromResult(
                JsonSerializer.Deserialize<Request>(rawMessage) ?? throw new InvalidRequestException());
        }

        private static Response ProcessRequest(Request requestObject)
        {
            var processor = ProcessorFactory.Create(requestObject);
            return processor.Process();
        }

        private static async Task SendResponse(Response response, Socket handler)
        {
            var json = JsonSerializer.Serialize(response) + eomToken;
            var bytes = Encoding.UTF8.GetBytes(json);
            await handler.SendAsync(bytes, 0);
        }

        private static Task SendErrorResponse(string reason, Socket handler)
        {
            throw new NotImplementedException();
        }

        
    }
}
