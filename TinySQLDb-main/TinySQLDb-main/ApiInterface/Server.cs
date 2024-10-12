using System.Net.Sockets;
using System.Net;
using System.Text;
using ApiInterface.InternalModels;
using System.Text.Json;
using ApiInterface.Exceptions;
using ApiInterface.Processors;
using ApiInterface.Models;
using Entities;

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
            Console.WriteLine($"Server ready at {serverEndPoint}");

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

        private static async Task<Request> ConvertToRequestObject(string rawMessage)
        {
            try
            {
                var request = JsonSerializer.Deserialize<Request>(rawMessage);
                if (request == null)
                {
                    throw new InvalidRequestException("La solicitud es nula o tiene un formato inválido.");
                }

                return request;
            }
            catch (JsonException ex)
            {
                throw new InvalidRequestException($"Error al deserializar la solicitud: {ex.Message}");
            }
        }

        private static Response ProcessRequest(Request requestObject)
        {
            try
            {
                if (requestObject == null)
                {
                    throw new InvalidRequestException("La solicitud no puede ser nula.");
                }

                var processor = ProcessorFactory.Create(requestObject);

                if (processor == null)
                {
                    throw new InvalidRequestException("No se pudo encontrar un procesador para esta solicitud.");
                }

                return processor.Process();
            }
            catch (InvalidRequestException ex)
            {
                return new Response
                {
                    Status = OperationStatus.Error,
                    Request = requestObject,  // Inicializar Request aquí
                    ResponseBody = $"Error de solicitud: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Status = OperationStatus.Error,
                    Request = requestObject,  // Inicializar Request aquí
                    ResponseBody = $"Error inesperado: {ex.Message}"
                };
            }
        }

        private static async Task SendResponse(Response response, Socket handler)
        {
            var json = JsonSerializer.Serialize(response) + eomToken;
            var bytes = Encoding.UTF8.GetBytes(json);
            await handler.SendAsync(bytes, 0);
        }

        private static async Task SendErrorResponse(string reason, Socket handler)
        {
            var request = new Request
            {
                RequestType = RequestType.SQLSentence,
                RequestBody = "Error handling request"
            };

            var errorResponse = new Response
            {
                Request = request,
                Status = OperationStatus.Error,
                ResponseBody = reason,
            };
            var json = JsonSerializer.Serialize(errorResponse) + eomToken;
            var bytes = Encoding.UTF8.GetBytes(json);
            await handler.SendAsync(bytes, 0);
        }
    }
}
