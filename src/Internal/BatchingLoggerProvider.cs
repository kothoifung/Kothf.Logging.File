// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in https://github.com/aspnet/Logging for license information.
// https://github.com/aspnet/Logging/blob/2d2f31968229eddb57b6ba3d34696ef366a6c71b/src/Microsoft.Extensions.Logging.AzureAppServices/Internal/BatchingLoggerProvider.cs

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kothf.Logging.File.Formatters;

namespace Kothf.Logging.File.Internal;

public abstract class BatchingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly List<LogMessage> _currentBatch = [];
    private readonly TimeSpan _flushPeriod;
    private readonly int? _batchSize;
    private readonly ILogFormatter _formatter;
    private readonly bool _includeScopes;

    private Channel<LogMessage> _messageQueue;
    private Task _outputTask;
    private CancellationTokenSource _cancellationTokenSource;

    private IExternalScopeProvider? _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the BatchingLoggerProvider class.
    /// </summary>
    /// <param name="options">The options that provides batching logger configuration settings.</param>
    /// <param name="formatters">A collection of available log formatters.</param>
    protected BatchingLoggerProvider(IOptions<BatchingLoggerOptions> options, IEnumerable<ILogFormatter> formatters)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(formatters);

        var loggerOptions = options.Value;

        if (loggerOptions.BatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), $"{nameof(loggerOptions.BatchSize)} must be a positive number.");
        if (loggerOptions.FlushPeriod <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), $"{nameof(loggerOptions.FlushPeriod)} must be longer than zero.");

        string formatterName = string.IsNullOrEmpty(loggerOptions.FormatterName)
            ? "simple"
            : loggerOptions.FormatterName.ToLowerInvariant();
        _formatter = formatters.FirstOrDefault(x => string.Equals(x.Name, formatterName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown formatter name {formatterName}", nameof(options));

        _flushPeriod = loggerOptions.FlushPeriod;
        _batchSize = loggerOptions.BatchSize;
        _includeScopes = loggerOptions.IncludeScopes;

        IsEnabled = loggerOptions.IsEnabled;
        if (IsEnabled)
        {
            Start();
        }
    }

    /// <summary>
    /// Gets the external scope provider.
    /// </summary>
    internal IExternalScopeProvider? ScopeProvider => _includeScopes ? _scopeProvider : null;

    /// <summary>
    /// Gets a value indicating whether the feature or component is enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Creates a logger instance for the specified category.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>An <see cref="ILogger"/> instance.</returns>
    public ILogger CreateLogger(string categoryName) => new BatchingLogger(this, categoryName, _formatter);

    /// <summary>
    /// Disposes the logger provider, stopping any background processing if enabled.
    /// </summary>
    public void Dispose()
    {
        if (IsEnabled)
        {
            Stop();
        }
    }

    /// <summary>
    /// Adds a log message to the queue for processing.
    /// </summary>
    /// <param name="timestamp">The timestamp of the log message.</param>
    /// <param name="message">The log message to add.</param>
    internal void AddMessage(DateTimeOffset timestamp, string message)
    {
        if (_cancellationTokenSource is { IsCancellationRequested: false } && _messageQueue is not null)
        {
            _ = _messageQueue.Writer.TryWrite(new LogMessage { Timestamp = timestamp, Message = message });
        }
    }

    /// <summary>
    /// Flushes the current batch of log messages by writing them asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    private async Task FlushBatchAsync(CancellationToken token)
    {
        if (_currentBatch.Count == 0)
            return;

        try
        {
            await WriteMessagesAsync(_currentBatch, token).ConfigureAwait(false);
        }
        catch { /* ignored */ }
        finally
        {
            _currentBatch.Clear();
        }
    }

    /// <summary>
    /// Processes log messages from the internal queue and writes them in batches asynchronously until cancellation is requested.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of processing and writing log messages.</returns>
    private async Task ProcessLogQueue()
    {
        try
        {
            while (true)
            {
                // 1. Try to read messages from the queue until we reach the batch size limit
                int remaining = (_batchSize ?? int.MaxValue) - _currentBatch.Count;

                while (remaining > 0 && _messageQueue.Reader.TryRead(out var message))
                {
                    _currentBatch.Add(message);
                    remaining--;
                }

                // 2. If we have any messages in the current batch, flush them to file
                if (_currentBatch.Count > 0)
                {
                    try
                    {
                        // Do not cancel flush operation
                        await WriteMessagesAsync(_currentBatch, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch { /* ignored */ }
                    finally
                    {
                        _currentBatch.Clear();
                    }
                }

                // 3. Wait for the next flush period
                try
                {
                    await Task.Delay(_flushPeriod, _cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested) { }
    }

    /// <summary>
    /// Initializes the internal message queue and starts the background task for processing log messages.
    /// </summary>
    private void Start()
    {
        _messageQueue = Channel.CreateUnbounded<LogMessage>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false
        });

        _cancellationTokenSource = new CancellationTokenSource();
        _outputTask = Task.Run(ProcessLogQueue);
    }

    /// <summary>
    /// Stops the background task for processing log messages and completes the message queue.
    /// </summary>
    private void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _messageQueue?.Writer.TryComplete();

        try
        {
            _outputTask?.Wait(_flushPeriod);
        }
        catch (TaskCanceledException) { }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) { }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is OperationCanceledException) { }
    }

    /// <summary>
    /// Asynchronously writes a collection of log messages to the underlying log storage or output.
    /// </summary>
    /// <param name="messages">The collection of log messages to write.</param>
    /// <param name="token">A cancellation token that can be used to cancel the asynchronous write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    protected abstract Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken token);

    /// <summary>
    /// Sets the external scope provider to be used for creating logging scopes.
    /// </summary>
    /// <param name="scopeProvider">The external scope provider that supplies scope information for logging.</param>
    void ISupportExternalScope.SetScopeProvider(IExternalScopeProvider? scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }
}
