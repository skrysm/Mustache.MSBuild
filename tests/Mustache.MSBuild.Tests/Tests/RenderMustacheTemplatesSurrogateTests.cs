// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

using Moq;

using Mustache.MSBuild.Utils;

using Shouldly;
using Xunit;

namespace Mustache.MSBuild.Tests;

/// <summary>
/// Tests for <see cref="RenderMustacheTemplatesSurrogate"/>.
/// </summary>
public sealed class RenderMustacheTemplatesSurrogateTests
{
    [Fact]
    public void Test_Execute()
    {
        // Setup
        var fileSystem = new MockFileSystem();

        const string TEMPLATE_CONTENTS = "<c>{{MyProperty}}</c>";
        const string DATA_FILE_CONTENTS = "{ \"MyProperty\": 42 }";

        var templateFile = MockUnixSupport.Path(@"C:\templates\MyFile.txt.mustache");
        var dataFile = MockUnixSupport.Path(@"C:\templates\MyFile.txt.json");
        var outputFile = MockUnixSupport.Path(@"C:\templates\MyFile.txt");

        fileSystem.AddFile(templateFile, new MockFileData(TEMPLATE_CONTENTS));
        fileSystem.AddFile(dataFile, new MockFileData(DATA_FILE_CONTENTS));

        var templatePaths = new[]
        {
            templateFile,
        };

        var logger = new MsBuildTestLogger();

        var task = new RenderMustacheTemplatesSurrogate(fileSystem);

        // Test
        task.Execute(templatePaths, logger).ShouldBe(true);

        // Verify
        logger.Exceptions.ShouldBeEmpty();
        logger.Warnings.ShouldBeEmpty();
        logger.Messages.ShouldBe(new[]
        {
            $"Template target file '{outputFile}': updated from template '{templateFile}'",
        });

        fileSystem.File.Exists(outputFile);
        fileSystem.File.ReadAllText(outputFile).ShouldBe("<c>42</c>");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Test_Execute_ExistingFile(bool targetIsUpToDate)
    {
        // Setup
        var fileSystem = new MockFileSystem();

        const string TEMPLATE_CONTENTS = "<c>{{MyProperty}}</c>";
        const string DATA_FILE_CONTENTS = "{ \"MyProperty\": 42 }";

        var templateFile = MockUnixSupport.Path(@"C:\templates\MyFile.txt.mustache");
        var dataFile = MockUnixSupport.Path(@"C:\templates\MyFile.txt.json");
        var outputFile = MockUnixSupport.Path(@"C:\templates\MyFile.txt");

        fileSystem.AddFile(templateFile, new MockFileData(TEMPLATE_CONTENTS));
        fileSystem.AddFile(dataFile, new MockFileData(DATA_FILE_CONTENTS));

        var originalLastWriteTime = DateTime.UtcNow - TimeSpan.FromHours(1);

        fileSystem.AddFile(outputFile, new MockFileData(targetIsUpToDate ? "<c>42</c>" : "<c>XX</c>")
        {
            LastWriteTime = originalLastWriteTime,
        });

        var templatePaths = new[]
        {
            templateFile,
        };

        var logger = new MsBuildTestLogger();

        var task = new RenderMustacheTemplatesSurrogate(fileSystem);

        // Test
        task.Execute(templatePaths, logger).ShouldBe(true);

        // Verify
        logger.Exceptions.ShouldBeEmpty();
        logger.Warnings.ShouldBeEmpty();

        fileSystem.File.Exists(outputFile);
        fileSystem.File.ReadAllText(outputFile).ShouldBe("<c>42</c>");
        if (targetIsUpToDate)
        {
            fileSystem.File.GetLastWriteTimeUtc(outputFile).ShouldBe(originalLastWriteTime);

            logger.Messages.ShouldBe(new[]
            {
                $"Template target file '{outputFile}': already up-to-date",
            });
        }
        else
        {
            logger.Messages.ShouldBe(new[]
            {
                $"Template target file '{outputFile}': updated from template '{templateFile}'",
            });

            // TODO: This requires this to be fixed: https://github.com/TestableIO/System.IO.Abstractions/issues/872
            //fileSystem.File.GetLastWriteTimeUtc(outputFile).ShouldBeGreaterThan(originalLastWriteTime);
            fileSystem.File.GetLastWriteTimeUtc(outputFile).ShouldNotBe(originalLastWriteTime);
        }
    }

    [Fact]
    public void Test_Execute_EmptyPaths()
    {
        // Setup
        var fileSystem = new MockFileSystem();

        var logger = new MsBuildTestLogger();

        var task = new RenderMustacheTemplatesSurrogate(fileSystem);

        // Test
        task.Execute(null, logger).ShouldBe(true);
        task.Execute(Array.Empty<string>(), logger).ShouldBe(true);

        // Verify
        logger.Exceptions.ShouldBeEmpty();
        logger.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Test_Execute_MissingTemplateFile()
    {
        // Setup
        var fileSystem = new MockFileSystem();

        var templateFile1 = MockUnixSupport.Path(@"c:\templates\MyFile.txt.mustache");
        var templateFile2 = MockUnixSupport.Path(@"c:\templates\SomeClass.cs.mustache");

        var templatePaths = new[]
        {
            templateFile1,
            templateFile2,
        };

        var logger = new MsBuildTestLogger();

        var task = new RenderMustacheTemplatesSurrogate(fileSystem);

        // Test
        task.Execute(templatePaths, logger).ShouldBe(true);

        // Verify
        logger.Exceptions.ShouldBeEmpty();
        logger.Warnings.ShouldBe(new[]
        {
            $"The template file '{templateFile1}' doesn't exist. Ignoring it.",
            $"The template file '{templateFile2}' doesn't exist. Ignoring it.",
        });
    }

    [Fact]
    public void Test_Execute_MissingDataFiles()
    {
        // Setup
        var fileSystem = new MockFileSystem();

        const string TEMPLATE_CONTENTS = "<c>{{MyProperty}}</c>";

        var templateFile1 = MockUnixSupport.Path(@"c:\templates\MyFile.txt.mustache");
        var templateFile2 = MockUnixSupport.Path(@"c:\templates\SomeClass.cs.mustache");

        fileSystem.AddFile(templateFile1, new MockFileData(TEMPLATE_CONTENTS));
        fileSystem.AddFile(templateFile2, new MockFileData(TEMPLATE_CONTENTS));

        var templatePaths = new[]
        {
            templateFile1,
            templateFile2,
        };

        var logger = new MsBuildTestLogger();

        var task = new RenderMustacheTemplatesSurrogate(fileSystem);

        // Test
        task.Execute(templatePaths, logger).ShouldBe(true);

        // Verify
        logger.Exceptions.ShouldBeEmpty();
        logger.Warnings.ShouldBe(new[]
        {
            $"The data file 'MyFile.txt.json' is missing for template file '{templateFile1}'. Ignoring it.",
            $"The data file 'SomeClass.cs.json' is missing for template file '{templateFile2}'. Ignoring it.",
        });
    }

    [Fact]
    public void Test_Execute_ExceptionHandling()
    {
        // Setup
        // NOTE: By having a strict mock here, using any of its members will result in an exception
        //   - which we use to test the exception handling of the code.
        var fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);

        var templatePaths = new[]
        {
            "/templates/MyFile.txt.mustache",
        };

        var logger = new MsBuildTestLogger();

        var task = new RenderMustacheTemplatesSurrogate(fileSystemMock.Object);

        // Test
        task.Execute(templatePaths, logger).ShouldBe(false);

        // Verify
        logger.Warnings.ShouldBeEmpty();
        logger.Exceptions.Count.ShouldBe(1);
        logger.Exceptions[0].ShouldBeAssignableTo<MockException>();
    }

    private sealed class MsBuildTestLogger : IMsBuildLogger
    {
        public List<string> Messages { get; } = [];

        public List<string> Warnings { get; } = [];

        public List<Exception> Exceptions { get; } = [];

        /// <inheritdoc />
        public void LogMessage(string message, params object[] messageArgs)
        {
            this.Messages.Add(string.Format(message, messageArgs));
        }

        /// <inheritdoc />
        public void LogWarning(string message, params object[] messageArgs)
        {
            this.Warnings.Add(string.Format(message, messageArgs));
        }

        /// <inheritdoc />
        public void LogErrorFromException(Exception exception)
        {
            this.Exceptions.Add(exception);
        }
    }
}
