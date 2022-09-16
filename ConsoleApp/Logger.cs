using System.IO;
using System.Text;

namespace ConsoleApp
{
    internal class Logger : ILogger
    {
        private StringBuilder LogText { get; } = new StringBuilder();

        public void Log(string text)
        {
            LogText.AppendLine(text);
            //Console.WriteLine($"{DateTime.Now:G}: {text}");
        }

        public void Dump()
        {
            File.WriteAllText("output.txt", LogText.ToString());
        }
    }

    internal interface ILogger
    {
        void Log(string text);

        void Dump();
    }
}