// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Kothf.Logging.File.Internal;

public struct LogMessage
{
    public DateTimeOffset Timestamp { get; set; }

    public string Message { get; set; }
}
