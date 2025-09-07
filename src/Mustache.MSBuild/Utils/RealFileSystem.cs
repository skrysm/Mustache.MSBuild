// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.IO.Abstractions;

namespace Mustache.MSBuild.Utils;

internal static class RealFileSystem
{
    public static IFileSystem Instance { get; } = new FileSystem();
}
