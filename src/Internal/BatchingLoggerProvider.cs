// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in https://github.com/aspnet/Logging for license information.
// https://github.com/aspnet/Logging/blob/2d2f31968229eddb57b6ba3d34696ef366a6c71b/src/Microsoft.Extensions.Logging.AzureAppServices/Internal/BatchingLoggerProvider.cs

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kothf.Logging.File.Formatters;

namespace Kothf.Logging.File.Internal;

public abstract class BatchingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly List<LogMessage> _currentBatch = [];
    private readonly TimeSpan _interval;
    private readonly int? _queueSize;
    private readonly int? _batchSize;
    private readonly ILogFormatter _formatter;

    private BlockingCollection<LogMessage> _messageQueue;
    private Task _outputTask;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _includeScopes;

    protected BatchingLoggerProvider(IOptionsMonitor<BatchingLoggerOptions> options, IEnumerable<ILogFormatter> formatters)
    {
        var loggerOptions = options.CurrentValue;

        if (loggerOptions.BatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), $"{nameof(loggerOptions.BatchSize)} must be a positive number.");
        if (loggerOptions.FlushPeriod <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), $"{nameof(loggerOptions.FlushPeriod)} must be longer than zero.");

        string formatterName = string.IsNullOrEmpty(loggerOptions.FormatterName)
            ? "simple"
            : loggerOptions.FormatterName.ToLowerInvariant();
        var formatter = formatters.FirstOrDefault(x => x.Name == formatterName) ??
            throw new ArgumentException($"Unknown formatter name {formatterName} - ensure custom formatters are registered correctly with the DI container", nameof(options));

        _formatter = formatter;
        _interval = loggerOptions.FlushPeriod;
        _batchSize = loggerOptions.BatchSize;
        _queueSize = loggerOptions.BackgroundQueueSize;

        UpdateOptions(options.CurrentValue);
    }

    public bool IsEnabled { get; private set; }

    public ILogger CreateLogger(string categoryName)
    {
        return new BatchingLogger(this, categoryName, _formatter);
    }

    public void Dispose()
    {
        if (IsEnabled)
        {
            Stop();
        }
    }

    internal void AddMessage(DateTimeOffset timestamp, string message)
    {
        if (!_messageQueue.IsAddingCompleted)
        {
            try
            {
                _messageQueue.Add(new LogMessage { Message = message, Timestamp = timestamp }, _cancellationTokenSource.Token);
            }
            catch
            {
                //cancellation token canceled or CompleteAdding called
            }
        }
    }

    internal IExternalScopeProvider ScopeProvider { get => _includeScopes ? field : null; private set; }

    protected abstract Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken token);

    private async Task ProcessLogQueue()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            int limit = _batchSize ?? int.MaxValue;

            while (limit > 0 && _messageQueue.TryTake(out var message))
            {
                _currentBatch.Add(message);
                limit--;
            }

            if (_currentBatch.Count > 0)
            {
                try
                {
                    await WriteMessagesAsync(_currentBatch, _cancellationTokenSource.Token);
                }
                catch
                {
                    // ignored
                }

                _currentBatch.Clear();
            }

            await Task.Delay(_interval, _cancellationTokenSource.Token);
        }
    }

    private void UpdateOptions(BatchingLoggerOptions options)
    {
        bool oldIsEnabled = IsEnabled;
        IsEnabled = options.IsEnabled;
        _includeScopes = options.IncludeScopes;

        if (oldIsEnabled != IsEnabled)
        {
            if (IsEnabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }

    private void Start()
    {
        _messageQueue = _queueSize == null ?
            [.. new ConcurrentQueue<LogMessage>()] :
            new BlockingCollection<LogMessage>(new ConcurrentQueue<LogMessage>(), _queueSize.Value);

        _cancellationTokenSource = new CancellationTokenSource();
        _outputTask = Task.Run(ProcessLogQueue);
    }

    private void Stop()
    {
        _cancellationTokenSource.Cancel();
        _messageQueue.CompleteAdding();

        try
        {
            _outputTask.Wait(_interval);
        }
        catch (TaskCanceledException)
        {
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException)
        {
        }
    }

    void ISupportExternalScope.SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        ScopeProvider = scopeProvider;
    }
}
