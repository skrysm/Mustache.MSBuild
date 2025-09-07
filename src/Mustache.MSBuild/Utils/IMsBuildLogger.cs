// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using JetBrains.Annotations;

using Microsoft.Build.Utilities;

namespace Mustache.MSBuild.Utils;

/// <summary>
/// Abstracts <see cref="TaskLoggingHelper"/>.
/// </summary>
internal interface IMsBuildLogger
{
    /// <summary>
    /// Logs the specified message to the MSBuild output.
    /// </summary>
    [StringFormatMethod("message")]
    void LogMessage(string message, params object[] messageArgs);

    /// <summary>
    /// Logs the specified warning to the MSBuild output.
    /// </summary>
    [StringFormatMethod("message")]
    void LogWarning(string message, params object[] messageArgs);

    /// <summary>
    /// Logs the specified exception to the MSBuild output.
    /// </summary>
    /// <remarks>
    /// This logging helper method is designed to capture and display information from arbitrary exceptions in a standard way.
    /// </remarks>
    void LogErrorFromException(Exception exception);
}
