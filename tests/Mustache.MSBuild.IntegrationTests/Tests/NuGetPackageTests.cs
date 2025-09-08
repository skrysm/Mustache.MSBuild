// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.IO.Compression;

using Mustache.MSBuild.TestUtils;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Mustache.MSBuild.Tests;

/// <summary>
/// Tests regarding the NuGet package itself.
/// </summary>
/// <remarks>
/// Technically, these are already covered by <see cref="EndToEndTests"/> but it makes it easier
/// to find the root cause if the end-to-end tests fail.
/// </remarks>
public sealed class NuGetPackageTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public NuGetPackageTests(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Test_ExpectedPackageContents()
    {
        // Setup
        var nugetPackage = BuiltPackagesUtils.GetMustacheMsbuildPackage();

        using var zipToOpen = new FileStream(nugetPackage.Path, FileMode.Open);
        using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);

        var filesInArchive = archive.Entries.ToList();
        this._testOutputHelper.WriteLine("Files in archive:\n- " + string.Join("\n- ", filesInArchive));

        // Test
        archive.Entries.ShouldContain(item => item.FullName == "tools/Mustache.MSBuild.dll");
        archive.Entries.ShouldContain(item => item.FullName == "build/Mustache.MSBuild.targets");
        archive.Entries.ShouldContain(item => item.FullName == "build/Mustache.MSBuild.tasks");
        archive.Entries.ShouldContain(item => item.FullName == "Version.props");
    }
}
