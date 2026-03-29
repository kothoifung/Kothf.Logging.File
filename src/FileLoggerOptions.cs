// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Kothf.Logging.File.Internal;

namespace Kothf.Logging.File;

/// <summary>
/// Options for file logger
/// </summary>
public sealed class FileLoggerOptions : BatchingLoggerOptions
{
    private string _fileName = "log";
    private string _extension = ".txt";
    private string _logDirectory = "logs";

    /// <summary>
    /// The filename to use for log files
    /// Defaults to <c>log</c>
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _fileName = value;
        }
    }

    /// <summary>
    /// The file extension to use for log files
    /// Defaults to <c>.txt</c>
    /// </summary>
    public string Extension
    {
        get => _extension;
        set {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _extension = value[0] == '.' ? value : string.Concat(".", value);
        }
    }

    /// <summary>
    /// The directory in which log files will be written
    /// Default to <c>logs</c>
    /// </summary>
    public string LogDirectory
    {
        get => _logDirectory;
        set {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _logDirectory = value;
        }
    }

    /// <summary>
    /// The periodicity for rolling over log files
    /// </summary>
    public PeriodicityOptions Periodicity { get; set; } = PeriodicityOptions.Daily;
}
