// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in https://github.com/aspnet/Logging for license information.
// https://github.com/aspnet/Logging/blob/2d2f31968229eddb57b6ba3d34696ef366a6c71b/src/Microsoft.Extensions.Logging.AzureAppServices/Internal/BatchingLoggerOptions.cs

namespace Kothf.Logging.File.Internal;

public class BatchingLoggerOptions
{
    private TimeSpan _flushPeriod = TimeSpan.FromSeconds(1);
    private int? _batchSize = 1000;

    /// <summary>
    /// Gets or sets the period after which logs will be flushed to the store.
    /// </summary>
    public TimeSpan FlushPeriod
    {
        get => _flushPeriod;
        set {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(FlushPeriod)} must be positive.");
            _flushPeriod = value;
        }
    }

    /// <summary>
    /// Gets or sets a maximum number of events to include in a single batch or null for no limit.
    /// </summary>
    /// Defaults to <c>null</c>.
    public int? BatchSize
    {
        get => _batchSize;
        set {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(BatchSize)} must be positive.");
            _batchSize = value;
        }
    }

    /// <summary>
    /// Gets or sets value indicating if logger accepts and queues writes.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether scopes should be included in the message.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IncludeScopes { get; set; } = false;

    /// <summary>
    /// Gets of sets the name of the log message formatter to use.
    /// Defaults to "simple" />.
    /// </summary>
    public string FormatterName { get; set; } = "simple";
}
