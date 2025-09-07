// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.Text;

using Mustache.MSBuild.DataTypes;
using Mustache.MSBuild.Services;
using Mustache.MSBuild.Utils;

using Shouldly;

using Xunit;

namespace Mustache.MSBuild.Tests.Services;

/// <summary>
/// Tests for <see cref="MustacheTemplateRenderer"/>.
/// </summary>
public sealed class MustacheTemplateRendererTests
{
    [Fact]
    public void Test_RenderTemplate()
    {
        // Setup
        var templateDescriptor = new TemplateDescriptor(
            mustacheTemplate: "<b>{{MyTemplateValue}}</b> - <c>{{TemplateFile}}</c>",
            templateDataJson: "{ \"MyTemplateValue\": 123 }",
            mustacheTemplateFileName: "MyFile.cs.mustache",
            templateFileEncoding: Encoding.UTF8,
            dataFileName: "MyFile.cs.json"
        );

        // Test
        var renderedTemplate = MustacheTemplateRenderer.RenderTemplate(templateDescriptor);

        // Verify
        renderedTemplate.ShouldBe("<b>123</b> - <c>MyFile.cs.mustache</c>");
    }

    [Fact]
    public void Test_RenderTemplate_XmlChars()
    {
        // Setup
        var templateDescriptor = new TemplateDescriptor(
            mustacheTemplate: "<b>{{MyTemplateValue}}</b>",
            templateDataJson: "{ \"MyTemplateValue\": \"<>&\\\"\" }",
            mustacheTemplateFileName: "MyFile.cs.mustache",
            templateFileEncoding: Encoding.UTF8,
            dataFileName: "MyFile.cs.json"
        );

        // Test
        var renderedTemplate = MustacheTemplateRenderer.RenderTemplate(templateDescriptor);

        // Verify
        renderedTemplate.ShouldBe("<b><>&\"</b>");
    }

    [Fact]
    public void Test_RenderTemplate_CommentsInDataFile()
    {
        // Setup
        var templateDescriptor = new TemplateDescriptor(
            mustacheTemplate: "<b>{{MyTemplateValue}}</b>",
            templateDataJson: "{ /* my comment */ \"MyTemplateValue\": 42 // another comment\n }",
            mustacheTemplateFileName: "MyFile.cs.mustache",
            templateFileEncoding: Encoding.UTF8,
            dataFileName: "MyFile.cs.json"
        );

        // Test
        var renderedTemplate = MustacheTemplateRenderer.RenderTemplate(templateDescriptor);

        // Verify
        renderedTemplate.ShouldBe("<b>42</b>");
    }

    [Fact]
    public void Test_RenderTemplate_DifferentCasing()
    {
        // Setup
        var templateDescriptor = new TemplateDescriptor(
            mustacheTemplate: "<b>{{MyTemplateVALUE}}</b> - <c>{{TemplateFILE}}</c>",
            templateDataJson: "{ \"MyTemplateValue\": 123 }",
            mustacheTemplateFileName: "MyFile.cs.mustache",
            templateFileEncoding: Encoding.UTF8,
            dataFileName: "MyFile.cs.json"
        );

        // Test
        var renderedTemplate = MustacheTemplateRenderer.RenderTemplate(templateDescriptor);

        // Verify
        renderedTemplate.ShouldBe("<b>123</b> - <c>MyFile.cs.mustache</c>");
    }

    [Fact]
    public void Test_RenderTemplate_InvalidJson()
    {
        // Setup
        var templateDescriptor = new TemplateDescriptor(
            mustacheTemplate: "<b>{{MyTemplateValue}}</b> - <c>{{TemplateFile}}</c>",
            templateDataJson: "{ \"SomeValue\": 42,",
            mustacheTemplateFileName: "MyFile.cs.mustache",
            templateFileEncoding: Encoding.UTF8,
            dataFileName: "MyFile.cs.json"
        );

        // Test
        var ex = Should.Throw<ErrorMessageException>(() => MustacheTemplateRenderer.RenderTemplate(templateDescriptor));

        // Verify
        ex.Message.ShouldStartWith("The content of data file 'MyFile.cs.json' is invalid:");
    }

    [Fact]
    public void Test_RenderTemplate_InvalidJson_Array()
    {
        // Setup
        var templateDescriptor = new TemplateDescriptor(
            mustacheTemplate: "<b>{{MyTemplateValue}}</b> - <c>{{TemplateFile}}</c>",
            templateDataJson: "[ 42, 43 ]",
            mustacheTemplateFileName: "MyFile.cs.mustache",
            templateFileEncoding: Encoding.UTF8,
            dataFileName: "MyFile.cs.json"
        );

        // Test
        var ex = Should.Throw<ErrorMessageException>(() => MustacheTemplateRenderer.RenderTemplate(templateDescriptor));

        // Verify
        ex.Message.ShouldStartWith("The content of data file 'MyFile.cs.json' is not an object but a JArray.");
    }

    [Fact]
    public void Test_RenderTemplate_UnresolvedToken()
    {
        // Setup
        var templateDescriptor = new TemplateDescriptor(
            mustacheTemplate: "<b>{{MyUnresolvedToken}}</b> - <c>{{MyResolvedToken}}</c> - <i>{{MyNullToken}}</i>",
            templateDataJson: "{ \"MyResolvedToken\": 123, \"MyNullToken\": null }",
            mustacheTemplateFileName: "MyFile.cs.mustache",
            templateFileEncoding: Encoding.UTF8,
            dataFileName: "MyFile.cs.json"
        );

        // Test
        var renderedTemplate = MustacheTemplateRenderer.RenderTemplate(templateDescriptor);

        // Verify
        renderedTemplate.ShouldBe("<b></b> - <c>123</c> - <i></i>");
    }

    [Fact]
    public void Test_RenderTemplate_UnresolvedToken_List()
    {
        // Setup
        var templateDescriptor = new TemplateDescriptor(
            mustacheTemplate: "{{#users}}\n{{.}}\n{{/users}} - {{MyResolvedToken}}",
            templateDataJson: "{ \"MyResolvedToken\": 123 }",
            mustacheTemplateFileName: "MyFile.cs.mustache",
            templateFileEncoding: Encoding.UTF8,
            dataFileName: "MyFile.cs.json"
        );

        // Test
        var renderedTemplate = MustacheTemplateRenderer.RenderTemplate(templateDescriptor);

        // Verify
        renderedTemplate.ShouldBe(" - 123");
    }

    [Fact]
    public void Test_RenderTemplate_SubObject()
    {
        // Setup
        var templateDescriptor = new TemplateDescriptor(
            mustacheTemplate: "{{#foo}}{{bar}},{{zar}}{{/foo}}",
            templateDataJson: "{ foo: { bar: \"foobar\", \"zar\": \"zoo\" } }",
            mustacheTemplateFileName: "MyFile.cs.mustache",
            templateFileEncoding: Encoding.UTF8,
            dataFileName: "MyFile.cs.json"
        );

        // Test
        var renderedTemplate = MustacheTemplateRenderer.RenderTemplate(templateDescriptor);

        // Verify
        renderedTemplate.ShouldBe("foobar,zoo");
    }
}
