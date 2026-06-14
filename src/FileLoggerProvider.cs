// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in https://github.com/aspnet/Logging for license information.
// https://github.com/aspnet/Logging/blob/2d2f31968229eddb57b6ba3d34696ef366a6c71b/src/Microsoft.Extensions.Logging.AzureAppServices/Internal/BatchingLoggerProvider.cs

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kothf.Logging.File.Formatters;
using Kothf.Logging.File.Internal;

namespace Kothf.Logging.File;

/// <summary>
/// An <see cref="ILoggerProvider" /> that writes logs to a file
/// </summary>
[ProviderAlias("File")]
public sealed class FileLoggerProvider : BatchingLoggerProvider
{
    private readonly string _path;
    private readonly string _fileName;
    private readonly string? _extension;
    private readonly PeriodicityOptions _periodicity;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggerProvider"/> class.
    /// </summary>
    /// <param name="options">The options for configuring the file logger.</param>
    /// <param name="formatter">The collection of log formatters to use.</param>
    public FileLoggerProvider(IOptions<FileLoggerOptions> options, IEnumerable<ILogFormatter> formatter) : base(options, formatter)
    {
        var loggerOptions = options.Value;
        _path = loggerOptions.LogDirectory;
        _fileName = loggerOptions.FileName;
        _extension = string.IsNullOrEmpty(loggerOptions.Extension) ? null : loggerOptions.Extension;
        _periodicity = loggerOptions.Periodicity;
    }

    /// <summary>
    /// Asynchronously writes a collection of log messages to their respective log files.
    /// </summary>
    /// <param name="messages">The collection of log messages to write.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    protected override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
    {
        // Assumes that the directory already exists,
        // as the provider should be initialized at application startup and the directory should be created at that time. 
        //Directory.CreateDirectory(_path);

        string? currentPath = null;
        StreamWriter? writer = null;

        try
        {
            foreach (var m in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string key = (_periodicity == PeriodicityOptions.Monthly)
                    ? $"{_fileName}{m.Timestamp:yyyyMM}{_extension}"
                    : $"{_fileName}{m.Timestamp:yyyyMMdd}{_extension}";

                string filePath = Path.Combine(_path, key);

                if (!string.Equals(currentPath, filePath, StringComparison.Ordinal))
                {
                    if (writer is not null)
                    {
                        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                        writer.Dispose();
                    }

                    writer = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read, 16 * 1024, FileOptions.Asynchronous));
                    currentPath = filePath;
                }

                await writer!.WriteAsync(m.Message.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (writer is not null)
            {
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                writer.Dispose();
            }
        }
    }
}
