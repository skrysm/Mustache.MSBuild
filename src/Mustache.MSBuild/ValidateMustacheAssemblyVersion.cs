// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.Diagnostics.CodeAnalysis;

using AppMotor.NetStandardCompatKit.Extensions;

using JetBrains.Annotations;

using Microsoft.Build.Framework;

using Task = Microsoft.Build.Utilities.Task;

namespace Mustache.MSBuild;

/// <summary>
/// A MSBuild task that validates the expected assembly version is loaded. This is primarily required/useful
/// for Visual Studio that can only load one version of this assembly - and doesn't reload it when the NuGet
/// package is updated.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
[ExcludeFromCodeCoverage]
public sealed class ValidateMustacheAssemblyVersion : Task
{
    // See also: https://docs.microsoft.com/en-us/visualstudio/msbuild/task-writing
    // See also: https://docs.microsoft.com/en-us/visualstudio/msbuild/tutorial-custom-task-code-generation
    // Base class code: https://github.com/dotnet/msbuild/blob/main/src/Utilities/Task.cs

    /// <summary>
    /// The paths to the template files to render.
    /// </summary>
    [Required]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string? ExpectedVersion { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        var expectedVersionAsString = this.ExpectedVersion;
        if (expectedVersionAsString.IsNullOrWhiteSpace())
        {
            this.Log.LogError("No ExpectedVersion specified.");
            return false;
        }

        // For something like "0.3.0-preview1", only parse "0.3.0".
        expectedVersionAsString = expectedVersionAsString.Split(['-'], count: 2, StringSplitOptions.RemoveEmptyEntries)[0];

        if (!Version.TryParse(expectedVersionAsString, out var expectedVersion))
        {
            this.Log.LogError($"The value for ExpectedVersion couldn't be parsed as version: {expectedVersionAsString}");
            return false;
        }

        if (expectedVersion.Revision == -1)
        {
            // Expected version only has 3 components while the assembly version always has 4 components.
            // Assume "0" for "Revision" in this case.
            expectedVersion = new Version(expectedVersion.Major, expectedVersion.Minor, expectedVersion.Build, revision: 0);
        }

        var ourAssemblyVersion = GetType().Assembly.GetName().Version;
        if (ourAssemblyVersion != expectedVersion)
        {
            this.Log.LogWarning($"The currently loaded version of Mustache.MSBuild ({ourAssemblyVersion}) doesn't match the version of the NuGet package ({expectedVersion}). Please restart Visual Studio to fix this problem.");
        }
        else
        {
            this.Log.LogMessage(MessageImportance.Normal, $"Loaded Mustache.MSBuild assembly version: {ourAssemblyVersion}");
        }

        return true;
    }
}
