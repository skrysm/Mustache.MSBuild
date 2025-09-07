// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.Text;

namespace Mustache.MSBuild.DataTypes;

/// <summary>
/// Describe the parts of a template.
/// </summary>
internal sealed class TemplateDescriptor
{
    /// <summary>
    /// The template itself.
    /// </summary>
    public string MustacheTemplate { get; }

    /// <summary>
    /// The template data in JSON format.
    /// </summary>
    public string TemplateDataJson { get; }

    /// <summary>
    /// The name of the file (without path) that contains the contents for <see cref="MustacheTemplate"/>.
    /// </summary>
    public string MustacheTemplateFileName { get; }

    /// <summary>
    /// The encoding used by the template file and thus also for the output file.
    /// </summary>
    public Encoding TemplateFileEncoding { get; }

    /// <summary>
    /// The name of the data file.
    /// </summary>
    public string DataFileName { get; }

    public TemplateDescriptor(
            string mustacheTemplate,
            string templateDataJson,
            string mustacheTemplateFileName,
            Encoding templateFileEncoding,
            string dataFileName
        )
    {
        this.MustacheTemplate = mustacheTemplate;
        this.TemplateDataJson = templateDataJson;
        this.MustacheTemplateFileName = mustacheTemplateFileName;
        this.TemplateFileEncoding = templateFileEncoding;
        this.DataFileName = dataFileName;
    }
}
