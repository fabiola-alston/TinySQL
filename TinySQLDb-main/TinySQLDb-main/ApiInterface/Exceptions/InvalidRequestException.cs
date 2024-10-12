using System;

namespace ApiInterface.Exceptions
{
    internal class InvalidRequestException : Exception
    {
        // Constructor sin parámetros
        public InvalidRequestException() : base()
        {
        }

        // Constructor que acepta un mensaje personalizado
        public InvalidRequestException(string message) : base(message)
        {
        }
    }
}
