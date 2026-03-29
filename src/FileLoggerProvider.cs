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
    public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options, IEnumerable<ILogFormatter> formatter) : base(options, formatter)
    {
        var loggerOptions = options.CurrentValue;
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
        foreach (var group in messages.GroupBy(GetGrouping))
        {
            string baseName = GetBaseName(group.Key);
            string filePath = Path.Combine(_path, $"{baseName}{_extension}");

            using var streamWriter = System.IO.File.AppendText(filePath);

            foreach (var item in group)
            {
                await streamWriter.WriteAsync(item.Message);
            }
        }
    }

    /// <summary>
    /// Generates the base file name according to the current periodicity setting.
    /// </summary>
    /// <param name="group">A tuple containing the year, month, and day that identify the data group for which to generate the base file name.</param>
    /// <returns>A string representing the base file name for the specified group, formatted according to the periodicity option.</returns>
    private string GetBaseName((int Year, int Month, int Day) group) => _periodicity switch {
        PeriodicityOptions.Daily => $"{_fileName}{group.Year:0000}{group.Month:00}{group.Day:00}",
        PeriodicityOptions.Monthly => $"{_fileName}{group.Year:0000}{group.Month:00}",
        _ => throw new InvalidDataException("Invalid periodicity")
    };

    /// <summary>
    /// Extracts the year, month, and day components from the timestamp of the specified log message.
    /// </summary>
    /// <param name="message">The log message from which to retrieve the date components.</param>
    /// <returns>A tuple containing the year, month, and day values from the log message's timestamp.</returns>
    private (int Year, int Month, int Day) GetGrouping(LogMessage message) => (message.Timestamp.Year, message.Timestamp.Month, message.Timestamp.Day);
}
