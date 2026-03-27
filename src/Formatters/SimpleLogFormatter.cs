// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Kothf.Logging.File.Formatters;

public sealed class SimpleLogFormatter : ILogFormatter
{
    public string Name => "simple";

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
