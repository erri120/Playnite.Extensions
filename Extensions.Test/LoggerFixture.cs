using System;
using Playnite.SDK;
using Xunit.Abstractions;

namespace Extensions.Test
{
    public class LoggerFixture : IDisposable
    {
        public readonly TestLogger Logger;

        public LoggerFixture()
        {
            Logger = new TestLogger();
        }

        public void Dispose() { }
    }

    public class TestLogger : ILogger
    {
        private ITestOutputHelper _helper;

        public void SetOutputHelper(ITestOutputHelper helper)
        {
            _helper = helper;
        }

        public void Info(string message)
        {
            _helper.WriteLine($"[INFO]{message}");
        }

        public void Info(Exception exception, string message)
        {
            _helper.WriteLine($"[INFO]{message}");
            throw new Exception(message);
        }

        public void Debug(string message)
        {
            _helper.WriteLine($"[DEBUG]{message}");
        }

        public void Debug(Exception exception, string message)
        {
            _helper.WriteLine($"[DEBUG]{message}");
            throw new Exception(message);
        }

        public void Warn(string message)
        {
            _helper.WriteLine($"[WARN]{message}");
        }

        public void Warn(Exception exception, string message)
        {
            _helper.WriteLine($"[WARN]{message}");
            throw new Exception(message);
        }

        public void Error(string message)
        {
            _helper.WriteLine($"[ERROR]{message}");
            throw new Exception(message);
        }

        public void Error(Exception exception, string message)
        {
            _helper.WriteLine($"[ERROR]{message}");
            throw exception;
        }
    }
}
