// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.Text.RegularExpressions;

using JetBrains.Annotations;

using Shouldly;

namespace Mustache.MSBuild.TestUtils;

internal static class BuiltPackagesUtils
{
    public const string NUGET_PACKAGE_NAME = "Mustache.MSBuild";

    [MustUseReturnValue]
    public static string GetBuiltPackagesDir()
    {
        var path = Path.GetFullPath("TestResources/built-packages", Path.GetDirectoryName(typeof(BuiltPackagesUtils).Assembly.Location)!);

        Directory.Exists(path).ShouldBe(true, $"Path \"{path}\" doesn't exist.");

        return path;
    }

    [MustUseReturnValue]
    public static (string Path, string Version) GetMustacheMsbuildPackage()
    {
        var builtPackageDir = GetBuiltPackagesDir();

        var nugetPackages = Directory.EnumerateFiles(builtPackageDir, $"{NUGET_PACKAGE_NAME}.*.nupkg").Select(Path.GetFileName).ToList();
        nugetPackages.Count.ShouldBe(1, $"Found more than one NuGet package: {string.Join(", ", nugetPackages)}");

        var nugetPackageFileName = nugetPackages[0];
        nugetPackageFileName.ShouldNotBeNull();

        var packageVersionRegex = new Regex($@"^{Regex.Escape(NUGET_PACKAGE_NAME)}\.(.+)\.nupkg$", RegexOptions.IgnoreCase);
        var match = packageVersionRegex.Match(nugetPackageFileName);
        match.Success.ShouldBe(true);

        // NOTE: The version can contain something like "-prerelease-1" - which is why the version can't be expressed as Version instance.
        var version = match.Groups[1].Value;

        return (Path: Path.Combine(builtPackageDir, nugetPackageFileName), Version: version);
    }
}
