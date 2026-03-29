// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Kothf.Logging.File.Formatters;

/// <summary>
/// Provides a simple, human-readable log formatter that outputs log entries in a single-line.
/// </summary>
public sealed class SimpleLogFormatter : ILogFormatter
{
    /// <summary>
    /// Gets the name associated with this instance.
    /// </summary>
    public string Name => "simple";

    /// <summary>
    /// Writes a formatted log entry to the specified string builder.
    /// </summary>
    /// <typeparam name="TState">The type of the state object associated with the log entry.</typeparam>
    /// <param name="logEntry">The log entry to write.</param>
    /// <param name="scopeProvider">The provider used to enumerate and format logging scopes.</param>
    /// <param name="builder">The string builder to which the formatted log entry is appended.</param>
    public void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, StringBuilder builder)
    {
        builder.Append(logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
        builder.Append(" [");
        builder.Append(logEntry.LogLevel.ToString());
        builder.Append("] ");
        builder.Append(logEntry.Category);

        if (scopeProvider != null)
        {
            scopeProvider.ForEachScope((scope, stringBuilder) => stringBuilder.Append(" => ").Append(scope), builder);
            builder.Append(':').AppendLine();
        }
        else
        {
            builder.Append(": ");
        }

        builder.AppendLine(logEntry.Formatter(logEntry.State, logEntry.Exception));

        if (logEntry.Exception != null)
        {
            builder.AppendLine(logEntry.Exception.ToString());
        }
    }
}
