// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using JetBrains.Annotations;

using Mustache.MSBuild.DataTypes;
using Mustache.MSBuild.Utils;

using Newtonsoft.Json.Linq;

using Stubble.Core.Builders;
using Stubble.Core.Interfaces;

namespace Mustache.MSBuild.Services;

/// <summary>
/// Renders Mustache templates.
/// </summary>
internal static class MustacheTemplateRenderer
{
    private static readonly Lazy<IStubbleRenderer> s_renderer = new(CreateRenderer);

    [MustUseReturnValue]
    private static IStubbleRenderer CreateRenderer()
    {
        return new StubbleBuilder()
            .Configure(settings =>
                {
                    settings.AddNewtonsoftJson();
                    settings.SetIgnoreCaseOnKeyLookup(true);
                    // Disable encoding of XML characters (<>&"). If a user wants them encoded,
                    // they must encode them themselves. (Without this, there would be not way
                    // for the user to get them "un-encoded".)
                    settings.SetEncodingFunction(static val => val);
                }
            )
            .Build();
    }

    /// <summary>
    /// Renders the specified template and returns it.
    /// </summary>
    [MustUseReturnValue]
    public static string RenderTemplate(TemplateDescriptor templateDescriptor)
    {
        var templateData = DataFileJsonReader.DeserializeObject(templateDescriptor.TemplateDataJson, templateDescriptor.DataFileName);

        // Adds variables that are always available.
        templateData.Add("TemplateFile", new JValue(templateDescriptor.MustacheTemplateFileName));

        return s_renderer.Value.Render(templateDescriptor.MustacheTemplate, templateData);
    }
}
