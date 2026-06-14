// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Kothf.Logging.File.Internal;

/// <summary>
/// Represents a log message
/// </summary>
/// <param name="Timestamp">The date and time when the log occurred.</param>
/// <param name="Message">The formatted text of the log message.</param>
public readonly record struct LogMessage(DateTimeOffset Timestamp, string Message);
