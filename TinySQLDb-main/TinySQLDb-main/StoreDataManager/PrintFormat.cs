using System;

namespace StoreDataManager
{
    public class ConsoleHelper
    {
        // Constructor p�blico para permitir instanciaci�n
        public ConsoleHelper() { }

        // M�todo para imprimir errores en color rojo
        public void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        // M�todo para imprimir mensajes de �xito en color verde
        public void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        // M�todo para imprimir informaci�n en color celeste (Cyan)
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
