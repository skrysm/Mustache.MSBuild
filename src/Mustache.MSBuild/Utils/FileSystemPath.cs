// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

namespace Mustache.MSBuild.Utils;

/// <summary>
/// Represents a path on the file system.
/// </summary>
internal sealed class FileSystemPath
{
    public string FullPath { get; }

    public string ShortPart { get; }

    public FileSystemPath(string fullPath, string shortPart)
    {
        this.FullPath = fullPath;
        this.ShortPart = shortPart;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.ShortPart;
    }
}
