using System;
using Playnite.SDK;

namespace DLSiteMetadata.Test
{
    public class TestLoggerFixture : IDisposable
    {
        public readonly TestLogger Logger;

        public TestLoggerFixture()
        {
            Logger = new TestLogger();
        }

        public void Dispose() { }
    }

    public class TestLogger : ILogger
    {
        public void Info(string message)
        {
            Console.WriteLine($"[INFO]{message}");
        }

        public void Info(Exception exception, string message)
        {
            Console.WriteLine($"[INFO]{message}");
            throw new Exception(message);
        }

        public void Debug(string message)
        {
            Console.WriteLine($"[DEBUG]{message}");
        }

        public void Debug(Exception exception, string message)
        {
            Console.WriteLine($"[DEBUG]{message}");
            throw new Exception(message);
        }

        public void Warn(string message)
        {
            Console.WriteLine($"[WARN]{message}");
        }

        public void Warn(Exception exception, string message)
        {
            Console.WriteLine($"[WARN]{message}");
            throw new Exception(message);
        }

        public void Error(string message)
        {
            Console.WriteLine($"[ERROR]{message}");
            throw new Exception(message);
        }

        public void Error(Exception exception, string message)
        {
            Console.WriteLine($"[ERROR]{message}");
            throw exception;
        }
    }
}
