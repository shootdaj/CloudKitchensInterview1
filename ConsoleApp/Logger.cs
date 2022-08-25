using System;

namespace ConsoleApp
{
    internal class Logger : ILogger
    {
        public void Log(string text)
        {
            Console.WriteLine($"{DateTime.Now:G}: {text}");
        }
    }

    internal interface ILogger
    {
        void Log(string text);
    }
}