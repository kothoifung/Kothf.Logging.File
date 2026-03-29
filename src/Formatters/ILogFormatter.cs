// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Kothf.Logging.File.Formatters;

/// <summary>
/// A formatter that formats log entries for output.
/// </summary>
public interface ILogFormatter
{
    /// <summary>
    /// Gets the name associated with the current instance.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Writes a formatted log entry to the specified string builder.
    /// </summary>
    /// <typeparam name="TState">The type of the state object associated with the log entry.</typeparam>
    /// <param name="logEntry">The log entry to write.</param>
    /// <param name="scopeProvider">The provider used to supply external scope information for the log entry.</param>
    /// <param name="stringBuilder">The string builder to which the formatted log entry is appended.</param>
    void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, StringBuilder stringBuilder);
}
