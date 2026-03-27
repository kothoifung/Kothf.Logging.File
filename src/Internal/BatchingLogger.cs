// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in https://github.com/aspnet/Logging for license information.
// https://github.com/aspnet/Logging/blob/2d2f31968229eddb57b6ba3d34696ef366a6c71b/src/Microsoft.Extensions.Logging.AzureAppServices/Internal/BatchingLogger.cs

using System.Text;
using Microsoft.Extensions.Logging;
using Kothf.Logging.File.Formatters;

namespace Kothf.Logging.File.Internal;

public sealed class BatchingLogger : ILogger
{
    private readonly BatchingLoggerProvider _provider;
    private readonly string _category;
    private readonly ILogFormatter _formatter;

    public BatchingLogger(BatchingLoggerProvider loggerProvider, string category, ILogFormatter formatter)
    {
        _provider = loggerProvider;
        _category = category;
        _formatter = formatter;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // NOTE: Differs from source
        return _provider.ScopeProvider?.Push(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _provider.IsEnabled;
    }

    public void Log<TState>(DateTimeOffset timestamp, LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var logEntry = new LogEntry<TState>(timestamp, logLevel, _category, eventId, state, exception, formatter!);
        var builder = new StringBuilder(128);
        _formatter.Write(in logEntry, _provider.ScopeProvider, builder);
        _provider.AddMessage(timestamp, builder.ToString());
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        Log(DateTimeOffset.Now, logLevel, eventId, state, exception, formatter);
    }
}
