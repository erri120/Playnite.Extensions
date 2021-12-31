using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace TestUtils;

public class XunitLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XunitLogger(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _testOutputHelper.WriteLine(formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) => EmptyDisposable.Instance;

    private class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance => new();

        public void Dispose() { }
    }
}
