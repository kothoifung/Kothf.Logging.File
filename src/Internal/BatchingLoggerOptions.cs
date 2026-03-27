// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in https://github.com/aspnet/Logging for license information.
// https://github.com/aspnet/Logging/blob/2d2f31968229eddb57b6ba3d34696ef366a6c71b/src/Microsoft.Extensions.Logging.AzureAppServices/Internal/BatchingLoggerOptions.cs

namespace Kothf.Logging.File.Internal;

public class BatchingLoggerOptions
{
    public TimeSpan FlushPeriod
    {
        get;
        set {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(FlushPeriod)} must be positive.");
            field = value;
        }
    } = TimeSpan.FromSeconds(1);

    public int? BackgroundQueueSize
    {
        get;
        set {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(BackgroundQueueSize)} must be non-negative.");
            field = value;
        }
    } = 1000;

    public int? BatchSize
    {
        get;
        set {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(BatchSize)} must be positive.");
            field = value;
        }
    }

    public bool IsEnabled { get; set; } = true;

    public bool IncludeScopes { get; set; } = false;

    public string FormatterName { get; set; } = "simple";
}
