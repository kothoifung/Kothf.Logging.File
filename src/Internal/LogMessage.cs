// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Kothf.Logging.File.Internal;

/// <summary>
/// Represents a log message
/// </summary>
public struct LogMessage
{
    /// <summary>
    /// Gets or sets the date and time when the log occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the formatted text of the log message.
    /// </summary>
    public string Message { get; set; }
}
