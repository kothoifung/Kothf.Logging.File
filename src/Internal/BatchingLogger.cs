// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in https://github.com/aspnet/Logging for license information.
// https://github.com/aspnet/Logging/blob/2d2f31968229eddb57b6ba3d34696ef366a6c71b/src/Microsoft.Extensions.Logging.AzureAppServices/Internal/BatchingLogger.cs

using System.Text;
using Microsoft.Extensions.Logging;
using Kothf.Logging.File.Formatters;

namespace Kothf.Logging.File.Internal;

/// <summary>
/// Represents an <see cref="ILogger"/> implementation for background batch processing.
/// </summary>
public sealed class BatchingLogger : ILogger
{
    private readonly BatchingLoggerProvider _provider;
    private readonly string _category;
    private readonly ILogFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the BatchingLogger class.
    /// </summary>
    /// <param name="loggerProvider">The batching logger provider that manages the lifecycle and batching behavior for this logger.</param>
    /// <param name="category">The category name for messages produced by this logger.</param>
    /// <param name="formatter">The log formatter used to format log messages.</param>
    public BatchingLogger(BatchingLoggerProvider loggerProvider, string category, ILogFormatter formatter)
    {
        _provider = loggerProvider;
        _category = category;
        _formatter = formatter;
    }

    /// <summary>
    /// Begins a logical operation scope that can be used to group related log entries.
    /// </summary>
    /// <typeparam name="TState">The type of the state to associate with the scope.</typeparam>
    /// <param name="state">The state to associate with the scope. </param>
    /// <returns>An IDisposable that ends the logical operation scope on disposal.</returns>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // NOTE: Differs from source
        return _provider.ScopeProvider?.Push(state);
    }

    /// <summary>
    /// Determines whether logging is enabled for the specified log level.
    /// </summary>
    /// <param name="logLevel">The log level to check for enabled status.</param>
    /// <returns>true if logging is enabled for the specified log level; otherwise, false.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return _provider.IsEnabled;
    }

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <typeparam name="TState">The type of the state to associate with the log entry.</typeparam>
    /// <param name="logLevel">The log level for the log entry.</param>
    /// <param name="eventId">The event ID for the log entry.</param>
    /// <param name="state">The state to associate with the log entry.</param>
    /// <param name="exception">The exception to associate with the log entry, if any.</param>
    /// <param name="formatter">The formatter function to format the log entry.</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        { return; }

        var timestamp = DateTimeOffset.Now;
        var logEntry = new LogEntry<TState>(timestamp, logLevel, _category, eventId, state, exception, formatter!);
        var builder = new StringBuilder(128);

        _formatter.Write(in logEntry, _provider.ScopeProvider, builder);
        _provider.AddMessage(timestamp, builder.ToString());
    }
}
