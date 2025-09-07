// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using Microsoft.Build.Framework;

using Mustache.MSBuild.Services;
using Mustache.MSBuild.Utils;

using Task = Microsoft.Build.Utilities.Task;

namespace Mustache.MSBuild;

/// <summary>
/// A MSBuild task that renders Mustache templates. For the actual rendering, see <see cref="MustacheTemplateRenderer"/>.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
[ExcludeFromCodeCoverage]
public sealed class RenderMustacheTemplates : Task
{
    // See also: https://docs.microsoft.com/en-us/visualstudio/msbuild/task-writing
    // See also: https://docs.microsoft.com/en-us/visualstudio/msbuild/tutorial-custom-task-code-generation
    // Base class code: https://github.com/dotnet/msbuild/blob/main/src/Utilities/Task.cs

    private static readonly RenderMustacheTemplatesSurrogate s_surrogateTask = new(RealFileSystem.Instance);

    /// <summary>
    /// The paths to the template files to render.
    /// </summary>
    [Required]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Required for MSBuild")]
    public ITaskItem[]? TemplatePaths { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        return s_surrogateTask.Execute(this.TemplatePaths, new MsBuildLogger(this.Log));
    }
}
