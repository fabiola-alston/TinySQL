using System;

namespace StoreDataManager
{
    public class ConsoleHelper
    {
        // Constructor público para permitir instanciación
        public ConsoleHelper() { }

        // Método para imprimir errores en color rojo
        public void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        // Método para imprimir mensajes de éxito en color verde
        public void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        // Método para imprimir información en color celeste (Cyan)
        public void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public void PrintStartTimer(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        public void PrintStopTimer(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
