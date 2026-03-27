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
/// A provider of <see cref="ConsoleLogger"/> instances.
/// </summary>
[ProviderAlias("File")]
public sealed class FileLoggerProvider : BatchingLoggerProvider
{
    private readonly string _path;
    private readonly string _fileName;
    private readonly string? _extension;
    private readonly PeriodicityOptions _periodicity;

    public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options, IEnumerable<ILogFormatter> formatter) : base(options, formatter)
    {
        var loggerOptions = options.CurrentValue;
        _path = loggerOptions.LogDirectory;
        _fileName = loggerOptions.FileName;
        _extension = string.IsNullOrEmpty(loggerOptions.Extension) ? null : "." + loggerOptions.Extension;
        _periodicity = loggerOptions.Periodicity;
    }

    protected override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_path);

        foreach (var group in messages.GroupBy(GetGrouping))
        {
            string baseName = GetBaseName(group.Key);
            string filePath = GetFilePath(baseName);

            if (filePath == null)
                return;

            using var streamWriter = System.IO.File.AppendText(filePath);

            foreach (var item in group)
            {
                await streamWriter.WriteAsync(item.Message);
            }
        }
    }

    private string GetBaseName((int Year, int Month, int Day, int Hour, int Minute) group) => _periodicity switch {
        PeriodicityOptions.Hourly => $"{_fileName}{group.Year:0000}{group.Month:00}{group.Day:00}{group.Hour:00}",
        PeriodicityOptions.Daily => $"{_fileName}{group.Year:0000}{group.Month:00}{group.Day:00}",
        PeriodicityOptions.Monthly => $"{_fileName}{group.Year:0000}{group.Month:00}",
        _ => throw new InvalidDataException("Invalid periodicity")
    };

    private string GetFilePath(string baseName)
    {
        string path = Path.Combine(_path, $"{baseName}{_extension}");
        return path;
    }

    private (int Year, int Month, int Day, int Hour, int Minute) GetGrouping(LogMessage message) => (message.Timestamp.Year, message.Timestamp.Month, message.Timestamp.Day, message.Timestamp.Hour, message.Timestamp.Minute);
}
