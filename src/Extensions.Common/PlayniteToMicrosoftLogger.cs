using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Playnite.SDK;
using PlayniteLogger = Playnite.SDK.ILogger;

namespace Extensions.Common;

public static class CustomLogger
{
    public static ILogger<T> GetLogger<T>(string loggerName)
    {
        return new CustomLogger<T>(LogManager.GetLogger(loggerName));
    }
}

public class CustomLogger<T> : ILogger<T>
{
    private readonly PlayniteLogger _playniteLogger;

    public CustomLogger(PlayniteLogger playniteLogger)
    {
        _playniteLogger = playniteLogger;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (exception is not null)
        {
            _playniteLogger.Error(exception, formatter(state, exception));
            return;
        }

        switch (logLevel)
        {
            case LogLevel.Trace:
                _playniteLogger.Trace(formatter(state, null));
                break;
            case LogLevel.Debug:
                _playniteLogger.Debug(formatter(state, null));
                break;
            case LogLevel.Information:
                _playniteLogger.Info(formatter(state, null));
                break;
            case LogLevel.Warning:
                _playniteLogger.Warn(formatter(state, null));
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                _playniteLogger.Error(formatter(state, null));
                break;
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }
}
