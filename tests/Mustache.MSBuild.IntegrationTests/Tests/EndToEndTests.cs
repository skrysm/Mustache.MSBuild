// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

using AppMotor.CoreKit.Processes;
using AppMotor.TestKit;

using JetBrains.Annotations;

using Mustache.MSBuild.TestUtils;

using Newtonsoft.Json;

using Shouldly;

using Xunit;

namespace Mustache.MSBuild.Tests;

/// <summary>
/// Uses the built NuGet package in a test project to render a template - both with "dotnet build"
/// and "MSBuild" (Windows only).
/// </summary>
public sealed class EndToEndTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly string _projectDir;

    public EndToEndTests(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;

        this._projectDir = Path.GetFullPath("TemplateSampleProject");
        this._testOutputHelper.WriteLine($"Project dir: {this._projectDir}");

        var targetFramework = DetermineTargetFramework();
        this._testOutputHelper.WriteLine($"Target framework: {targetFramework}");

        Directory.CreateDirectory(this._projectDir);

        CreateNuGetConfig(this._projectDir);
        CreateTestProjectFile(this._projectDir, targetFramework);

        DeleteDirectory($"{this._projectDir}/bin");
        DeleteDirectory($"{this._projectDir}/obj");
        DeleteDirectory($"{this._projectDir}/_PackagesStorage");

        // NOTE: The file must exist so that MSBuild can pick it up as file to be compiled.
        //   However, we clear it to see that file generation actually works.
        File.WriteAllText($"{this._projectDir}/Program.cs", contents: "");
    }

    [MustUseReturnValue]
    private static string DetermineTargetFramework()
    {
        var targetFrameworkAttribute = typeof(EndToEndTests).Assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        targetFrameworkAttribute.ShouldNotBeNull();

        var match = Regex.Match(targetFrameworkAttribute.FrameworkName, @",Version=v(\d+\.\d+)", RegexOptions.IgnoreCase);
        match.Success.ShouldBe(true);

        return $"net{match.Groups[1].Value}";
    }

    /// <summary>
    /// Executes the test with "dotnet build". Works on any platform.
    /// </summary>
    [Fact]
    public void Test_Execute_DotNetBuild()
    {
        // Setup
        CreateTemplateFile(secondContent: false);
        CreateTemplateDataFile();

        // Test
        var buildResult = ExecuteChildProcess(
            "dotnet",
            new ProcessArguments("build", "TemplateSampleProject.csproj"),
            ignoreExitCode: true // <-- required so that we can print the build output even if the build has failed
        );

        this._testOutputHelper.WriteLine(buildResult.Output);

        // Verify
        buildResult.ExitCode.ShouldBe(0);
        File.ReadAllText($"{this._projectDir}/Program.cs").ShouldBe(GetExpectedGeneratedFileContent(secondContent: false));

        // Test 2 - Change template which should trigger rebuild
        CreateTemplateFile(secondContent: true);

        var buildResult2 = ExecuteChildProcess(
            "dotnet",
            new ProcessArguments("build", "TemplateSampleProject.csproj"),
            ignoreExitCode: true // <-- required so that we can print the build output even if the build has failed
        );

        this._testOutputHelper.WriteLine(buildResult2.Output);

        // Verify
        buildResult2.ExitCode.ShouldBe(0);
        File.ReadAllText($"{this._projectDir}/Program.cs").ShouldBe(GetExpectedGeneratedFileContent(secondContent: true));
    }

    /// <summary>
    /// Executes the test with MSBuild (.NET Framework). This is how Visual Studio will use this package. Only works on Windows.
    /// </summary>
    [Fact]
    public void Test_Execute_MsBuild()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "only on Windows");

        // Setup
        CreateTemplateFile(secondContent: false);
        CreateTemplateDataFile();

        var msBuildPath = DetermineMsBuildPath();
        this._testOutputHelper.WriteLine($"MSBuild path: {msBuildPath}");

        // Test
        var buildResult = ExecuteChildProcess(
            msBuildPath,
            new ProcessArguments("-restore", "TemplateSampleProject.csproj", "/v:m"),
            ignoreExitCode: true // <-- required so that we can print the build output even if the build has failed
        );

        this._testOutputHelper.WriteLine(buildResult.Output);

        // Verify
        buildResult.ExitCode.ShouldBe(0);
        File.ReadAllText($"{this._projectDir}/Program.cs").ShouldBe(GetExpectedGeneratedFileContent(secondContent: false));

        // Test 2 - Change template which should trigger rebuild
        CreateTemplateFile(secondContent: true);

        var buildResult2 = ExecuteChildProcess(
            msBuildPath,
            new ProcessArguments("TemplateSampleProject.csproj", "/v:m"),
            ignoreExitCode: true // <-- required so that we can print the build output even if the build has failed
        );

        this._testOutputHelper.WriteLine(buildResult2.Output);

        // Verify
        buildResult2.ExitCode.ShouldBe(0);
        File.ReadAllText($"{this._projectDir}/Program.cs").ShouldBe(GetExpectedGeneratedFileContent(secondContent: true));
    }

    private ChildProcessResult ExecuteChildProcess(
            string processFileName,
            ProcessArguments? arguments = null,
            string? workingDirectory = null,
            bool ignoreExitCode = false
        )
    {
        var startInfo = new ChildProcessStartInfo(processFileName, arguments ?? [])
        {
            WorkingDirectory = workingDirectory ?? this._projectDir,
            ProcessTimeout = TestEnvInfo.RunsInCiPipeline ? TimeSpan.FromMinutes(10) : TimeSpan.FromMinutes(1),
            SuccessExitCode = ignoreExitCode ? null : 0,
        };

        try
        {
            return ChildProcess.Exec(startInfo);
        }
        catch (Exception)
        {
            this._testOutputHelper.WriteLine($"Failed command: {processFileName} {arguments}");
            throw;
        }
    }

    [MustUseReturnValue]
    private string DetermineMsBuildPath()
    {
        var vsWhereResult = ExecuteChildProcess(
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Microsoft Visual Studio\Installer\vswhere.exe",
            new ProcessArguments("-requires", "Microsoft.Component.MSBuild", "-latest", "-utf8", "-format", "json")
        );

        var installationInfo = JsonConvert.DeserializeObject<List<VsInstallationInfo>>(vsWhereResult.Output);

        installationInfo.ShouldNotBeNull();
        installationInfo.ShouldNotBeEmpty();
        installationInfo[0].InstallationPath.ShouldNotBeNullOrWhiteSpace();

        var msBuildPath = Path.Combine(installationInfo[0].InstallationPath!, @"MSBuild\Current\Bin\MSBuild.exe");

        File.Exists(msBuildPath).ShouldBe(true, $"MSBuild path: {msBuildPath}");

        return msBuildPath;
    }

    private static void CreateNuGetConfig(string projectDir)
    {
        var builtPackageDir = BuiltPackagesUtils.GetBuiltPackagesDir();
        Directory.Exists(builtPackageDir).ShouldBe(true, $"Built Packages Dir: {builtPackageDir}");

        // language=xml
        string fileContents = $@"
<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <packageSources>
        <add key=""LocalTest"" value=""{builtPackageDir}"" />
    </packageSources>
    <config>
        <add key=""globalPackagesFolder"" value=""./_PackagesStorage"" />
    </config>
    <!-- See: https://docs.microsoft.com/en-us/nuget/consume-packages/package-source-mapping -->
    <packageSourceMapping>
        <packageSource key=""nuget.org"">
            <package pattern=""*"" />
        </packageSource>
        <packageSource key=""LocalTest"">
            <package pattern=""{BuiltPackagesUtils.NUGET_PACKAGE_NAME}"" />
        </packageSource>
    </packageSourceMapping>
</configuration>
";

        File.WriteAllText($"{projectDir}/NuGet.config", fileContents.TrimStart());
    }

    private void CreateTestProjectFile(string projectDir, string targetFramework)
    {
        var nugetPackage = BuiltPackagesUtils.GetMustacheMsbuildPackage();

        this._testOutputHelper.WriteLine($"Found NuGet version: {nugetPackage.Version}");

        // language=xml
        string projectFileContents = $@"
<Project Sdk=""Microsoft.NET.Sdk"">

    <PropertyGroup>
        <TargetFramework>{targetFramework}</TargetFramework>
        <OutputType>Exe</OutputType>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include=""{BuiltPackagesUtils.NUGET_PACKAGE_NAME}"" Version=""{nugetPackage.Version}"">
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
    </ItemGroup>

</Project>
";

        File.WriteAllText($"{projectDir}/TemplateSampleProject.csproj", projectFileContents);
    }

    private void CreateTemplateFile(bool secondContent)
    {
        const string TEMPLATE_FILE_CONTENTS = @"
// NOTE: This file has been automatically generated from {{TemplateFile}}. Any changes made to
//   this file will be lost on the next build.

Console.WriteLine(""I hereby greet all my friends:"");
Console.WriteLine();

{{#friends}}
Console.WriteLine(""Hello {{.}}!"");
{{/friends}}

Console.WriteLine();
Console.WriteLine(""Nice to know you all!"");
";

        if (secondContent)
        {
            File.WriteAllText($"{this._projectDir}/Program.cs.mustache", TEMPLATE_FILE_CONTENTS.TrimStart() + "Console.WriteLine();\r\n");
        }
        else
        {
            File.WriteAllText($"{this._projectDir}/Program.cs.mustache", TEMPLATE_FILE_CONTENTS.TrimStart());
        }
    }

    private void CreateTemplateDataFile()
    {
        // language=json
        const string DATA_FILE_CONTENTS = @"
{
    ""friends"": [ ""Alice"", ""Bob"", ""Charlie"" ]
}
";

        File.WriteAllText($"{this._projectDir}/Program.cs.json", DATA_FILE_CONTENTS.TrimStart());
    }

    [MustUseReturnValue]
    private static string GetExpectedGeneratedFileContent(bool secondContent)
    {
        const string EXPECTED_FILE_CONTENTS = @"
// NOTE: This file has been automatically generated from Program.cs.mustache. Any changes made to
//   this file will be lost on the next build.

Console.WriteLine(""I hereby greet all my friends:"");
Console.WriteLine();

Console.WriteLine(""Hello Alice!"");
Console.WriteLine(""Hello Bob!"");
Console.WriteLine(""Hello Charlie!"");

Console.WriteLine();
Console.WriteLine(""Nice to know you all!"");
";

        if (secondContent)
        {
            return EXPECTED_FILE_CONTENTS.TrimStart() + "Console.WriteLine();\r\n";
        }
        else
        {
            return EXPECTED_FILE_CONTENTS.TrimStart();
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [DataContract]
    private sealed class VsInstallationInfo
    {
        [DataMember(Name = "installationPath")]
        public string? InstallationPath { get; set; }
    }
}
