// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.IO.Abstractions;

using JetBrains.Annotations;

using Mustache.MSBuild.Utils;

namespace Mustache.MSBuild.DataTypes;

/// <summary>
/// Contains the information about the (file) paths for a template.
/// </summary>
internal sealed class TemplatePathDescriptor
{
    /// <summary>
    /// The absolute path to the mustache file for this template.
    /// </summary>
    public FileSystemPath PathToMustacheFile { get; }

    /// <summary>
    /// The absolute path to the output file of this template.
    /// </summary>
    public FileSystemPath PathToOutputFile { get; }

    /// <summary>
    /// The absolute path to the data (.json) file of this template.
    /// </summary>
    public FileSystemPath PathToDataFile { get; }

    private TemplatePathDescriptor(string pathToMustacheFile, string pathToOutputFile, string pathToDataFile, IFileSystem fileSystem)
    {
        this.PathToMustacheFile = new FileSystemPath(fileSystem.Path.GetFullPath(pathToMustacheFile), pathToMustacheFile);
        this.PathToOutputFile = new FileSystemPath(fileSystem.Path.GetFullPath(pathToOutputFile), pathToOutputFile);
        this.PathToDataFile = new FileSystemPath(fileSystem.Path.GetFullPath(pathToDataFile), pathToDataFile);
    }

    /// <summary>
    /// Creates a descriptor where the data file's path and the output file's path are based on the path
    /// of the template file.
    /// </summary>
    [MustUseReturnValue]
    public static TemplatePathDescriptor ForTemplateFile(string pathToMustacheFile, IFileSystem fileSystem)
    {
        // NOTE: GetFileNameWithoutExtension() only removes the last(!) extension. So for "template.cs.mustache" we get "template.cs".
        var outputFileName = fileSystem.Path.GetFileNameWithoutExtension(pathToMustacheFile);
        var pathToOutputFile = fileSystem.Path.Combine(fileSystem.Path.GetDirectoryName(pathToMustacheFile)!, outputFileName);

        return new TemplatePathDescriptor(
            pathToMustacheFile: pathToMustacheFile,
            pathToOutputFile: pathToOutputFile,
            pathToDataFile: pathToOutputFile + ".json",
            fileSystem
        );

    }
}
