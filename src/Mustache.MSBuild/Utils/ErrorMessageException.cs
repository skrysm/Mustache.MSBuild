// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.Diagnostics.CodeAnalysis;

namespace Mustache.MSBuild.Utils;

/// <summary>
/// An exception for which only the message will be displayed to the user.
/// </summary>
[SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "By design")]
internal sealed class ErrorMessageException : Exception
{
    public ErrorMessageException(string message) : base(message)
    {
    }
}
