// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.IO.Abstractions.TestingHelpers;
using System.Text;

using Mustache.MSBuild.DataTypes;
using Mustache.MSBuild.Services;
using Mustache.MSBuild.Utils;

using Shouldly;

using Xunit;

namespace Mustache.MSBuild.Tests.Services;

/// <summary>
/// Tests for <see cref="TemplatesFileService"/>.
/// </summary>
public sealed class TemplatesFileServiceTests
{
    #region LoadTemplate

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Test_LoadTemplate_UTF8(bool withBom)
    {
        Test_LoadTemplate(
            withBom ? Encoding.UTF8 : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            specifyEncodingInDataFile: false
        );
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Test_LoadTemplate_UTF16(bool bigEndian, bool withBom)
    {
        var encoding = new UnicodeEncoding(bigEndian: bigEndian, byteOrderMark: withBom);
        Test_LoadTemplate(encoding, specifyEncodingInDataFile: !withBom);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Test_LoadTemplate_UTF32(bool bigEndian, bool withBom)
    {
        var encoding = new UTF32Encoding(bigEndian: bigEndian, byteOrderMark: withBom);
        Test_LoadTemplate(encoding, specifyEncodingInDataFile: !withBom);
    }

    private static void Test_LoadTemplate(Encoding templateFileEncoding, bool specifyEncodingInDataFile)
    {
        // Setup
        var fileSystem = new MockFileSystem();

        const string TEMPLATE_CONTENTS = "<c>{{MyProperty}}</c>";

        string dataFileContents;
        Encoding expectedTemplateFileEncoding;

        if (specifyEncodingInDataFile)
        {
            dataFileContents = $"{{ \"MyProperty\": 42, \"$Encoding\": \"{templateFileEncoding.WebName}\" }}";
            expectedTemplateFileEncoding = Encoding.GetEncoding(templateFileEncoding.WebName);
        }
        else
        {
            dataFileContents = "{ \"MyProperty\": 42 }";
            expectedTemplateFileEncoding = templateFileEncoding;
        }

        fileSystem.AddFile("/templates/MyFile.txt.mustache", new MockFileData(TEMPLATE_CONTENTS, templateFileEncoding));
        fileSystem.AddFile("/templates/MyFile.txt.json", new MockFileData(dataFileContents, Encoding.UTF8));

        var pathDescriptor = TemplatePathDescriptor.ForTemplateFile("/templates/MyFile.txt.mustache", fileSystem);

        var templatesFileService = new TemplatesFileService(fileSystem);

        // Test
        var templateDescriptor = templatesFileService.LoadTemplate(pathDescriptor);

        // Verify
        templateDescriptor.MustacheTemplate.ShouldBe(TEMPLATE_CONTENTS);
        templateDescriptor.TemplateDataJson.ShouldBe(dataFileContents);
        templateDescriptor.MustacheTemplateFileName.ShouldBe("MyFile.txt.mustache");
        templateDescriptor.TemplateFileEncoding.ShouldBe(expectedTemplateFileEncoding);
    }

    [Fact]
    public void Test_LoadTemplate_InvalidEncoding()
    {
        // Setup
        var fileSystem = new MockFileSystem();

        const string TEMPLATE_CONTENTS = "<c>{{MyProperty}}</c>";
        const string DATA_FILE_CONTENTS = "{ \"MyProperty\": 42, \"$Encoding\": \"my-super-invalid-encoding\" }";

        fileSystem.AddFile("/templates/MyFile.txt.mustache", new MockFileData(TEMPLATE_CONTENTS, Encoding.UTF8));
        fileSystem.AddFile("/templates/MyFile.txt.json", new MockFileData(DATA_FILE_CONTENTS, Encoding.UTF8));

        var pathDescriptor = TemplatePathDescriptor.ForTemplateFile("/templates/MyFile.txt.mustache", fileSystem);

        var templatesFileService = new TemplatesFileService(fileSystem);

        // Test
        var ex = Should.Throw<ErrorMessageException>(() => templatesFileService.LoadTemplate(pathDescriptor));

        // Verify
        ex.Message.ShouldContain("'my-super-invalid-encoding'");
    }

    [Fact]
    public void Test_LoadTemplate_InvalidDataJson()
    {
        // Setup
        var fileSystem = new MockFileSystem();

        const string TEMPLATE_CONTENTS = "<c>{{MyProperty}}</c>";
        const string DATA_FILE_CONTENTS = "{ \"MyProperty\": 42";

        fileSystem.AddFile("/templates/MyFile.txt.mustache", new MockFileData(TEMPLATE_CONTENTS, Encoding.UTF8));
        fileSystem.AddFile("/templates/MyFile.txt.json", new MockFileData(DATA_FILE_CONTENTS, Encoding.UTF8));

        var pathDescriptor = TemplatePathDescriptor.ForTemplateFile("/templates/MyFile.txt.mustache", fileSystem);

        var templatesFileService = new TemplatesFileService(fileSystem);

        // Test
        var ex = Should.Throw<ErrorMessageException>(() => templatesFileService.LoadTemplate(pathDescriptor));

        // Verify
        ex.Message.ShouldContain("The content of data file 'MyFile.txt.mustache' is invalid:");
    }

    #endregion LoadTemplate

    #region WriteRenderedTemplate

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Test_WriteRenderedTemplate_OutputFileDoesNotYetExist(bool onlyWriteFileIfContentsHaveChanged)
    {
        // Setup
        var fileSystem = new MockFileSystem();

        const string TEMPLATE_CONTENTS = "<c>{{MyProperty}}</c>";
        const string DATA_FILE_CONTENTS = "{ \"MyProperty\": 42 }";
        const string OUTPUT_CONTENT = "Some rendered content";

        fileSystem.AddFile(MockUnixSupport.Path(@"C:\templates\MyFile.txt.mustache"), new MockFileData(TEMPLATE_CONTENTS, Encoding.UTF8));
        fileSystem.AddFile(MockUnixSupport.Path(@"C:\templates\MyFile.txt.json"), new MockFileData(DATA_FILE_CONTENTS, Encoding.UTF8));

        var pathDescriptor = TemplatePathDescriptor.ForTemplateFile("/templates/MyFile.txt.mustache", fileSystem);

        // Verify assumption
        pathDescriptor.PathToOutputFile.FullPath.ShouldBe(MockUnixSupport.Path(@"C:\templates\MyFile.txt"));
        fileSystem.File.Exists(pathDescriptor.PathToOutputFile.FullPath).ShouldBe(false);

        var templatesFileService = new TemplatesFileService(fileSystem);

        // Test
        templatesFileService.WriteRenderedTemplate(
            pathDescriptor,
            renderedTemplate: OUTPUT_CONTENT,
            Encoding.UTF8,
            onlyWriteFileIfContentsHaveChanged: onlyWriteFileIfContentsHaveChanged
        );

        // Verify
        fileSystem.File.Exists(pathDescriptor.PathToOutputFile.FullPath).ShouldBe(true);
        fileSystem.File.ReadAllText(pathDescriptor.PathToOutputFile.FullPath, Encoding.UTF8).ShouldBe(OUTPUT_CONTENT);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Test_WriteRenderedTemplate_OutputFileExists(bool sameContent, bool onlyWriteFileIfContentsHaveChanged)
    {
        // Setup
        var fileSystem = new MockFileSystem();

        const string TEMPLATE_CONTENTS = "<c>{{MyProperty}}</c>";
        const string DATA_FILE_CONTENTS = "{ \"MyProperty\": 42 }";
        const string OUTPUT_CONTENT = "Some rendered content";

        var originalLastWriteTime = new DateTimeOffset(2000, 1, 1, 12, 0, 30, TimeSpan.Zero);

        fileSystem.AddFile(MockUnixSupport.Path(@"C:\templates\MyFile.txt.mustache"), new MockFileData(TEMPLATE_CONTENTS, Encoding.UTF8));
        fileSystem.AddFile(MockUnixSupport.Path(@"C:\templates\MyFile.txt.json"), new MockFileData(DATA_FILE_CONTENTS, Encoding.UTF8));
        fileSystem.AddFile(
            MockUnixSupport.Path(@"C:\templates\MyFile.txt"),
            new MockFileData(sameContent ? OUTPUT_CONTENT : OUTPUT_CONTENT + "XXX")
            {
                LastWriteTime = originalLastWriteTime,
            }
        );

        var pathDescriptor = TemplatePathDescriptor.ForTemplateFile(MockUnixSupport.Path(@"C:\templates\MyFile.txt.mustache"), fileSystem);

        // Verify assumption
        pathDescriptor.PathToOutputFile.FullPath.ShouldBe(MockUnixSupport.Path(@"C:\templates\MyFile.txt"));

        var templatesFileService = new TemplatesFileService(fileSystem);

        // Test
        templatesFileService.WriteRenderedTemplate(
            pathDescriptor,
            renderedTemplate: OUTPUT_CONTENT,
            Encoding.UTF8,
            onlyWriteFileIfContentsHaveChanged: onlyWriteFileIfContentsHaveChanged
        );

        // Verify
        fileSystem.File.Exists(pathDescriptor.PathToOutputFile.FullPath).ShouldBe(true);
        fileSystem.File.ReadAllText(pathDescriptor.PathToOutputFile.FullPath, Encoding.UTF8).ShouldBe(OUTPUT_CONTENT);

        if (onlyWriteFileIfContentsHaveChanged && sameContent)
        {
            new DateTimeOffset(fileSystem.File.GetLastWriteTimeUtc(pathDescriptor.PathToOutputFile.FullPath)).ShouldBe(originalLastWriteTime);
        }
        else
        {
            fileSystem.File.GetLastWriteTimeUtc(pathDescriptor.PathToOutputFile.FullPath).ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        }
    }

    #endregion WriteRenderedTemplate
}
