// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.IO.Abstractions;

using Mustache.MSBuild.DataTypes;
using Mustache.MSBuild.Services;
using Mustache.MSBuild.Utils;

namespace Mustache.MSBuild;

/// <summary>
/// Contains the actual implementation for <see cref="RenderMustacheTemplates"/>. The code is extracted to
/// make it easier to unit test (because mocking the requirements of an actual MSBuild task is quite complex).
/// </summary>
internal sealed class RenderMustacheTemplatesSurrogate
{
    private readonly IFileSystem _fileSystem;

    private readonly TemplatesFileService _templatesFileService;

    public RenderMustacheTemplatesSurrogate(IFileSystem fileSystem)
    {
        this._fileSystem = fileSystem;
        this._templatesFileService = new TemplatesFileService(fileSystem);
    }

    public bool Execute(IEnumerable<string>? templatePaths, IMsBuildLogger logger)
    {
        if (templatePaths is null)
        {
            return true;
        }

        bool success = true;

        foreach (var templatePath in templatePaths)
        {
            try
            {
                var templatePathDescriptor = TemplatePathDescriptor.ForTemplateFile(pathToMustacheFile: templatePath, this._fileSystem);

                if (!this._fileSystem.File.Exists(templatePathDescriptor.PathToMustacheFile.FullPath))
                {
                    logger.LogWarning("The template file '{0}' doesn't exist. Ignoring it.", templatePathDescriptor.PathToMustacheFile.ShortPart);
                    continue;
                }

                if (!this._fileSystem.File.Exists(templatePathDescriptor.PathToDataFile.FullPath))
                {
                    logger.LogWarning(
                        "The data file '{0}' is missing for template file '{1}'. Ignoring it.",
                        Path.GetFileName(templatePathDescriptor.PathToDataFile.FullPath),
                        templatePathDescriptor.PathToMustacheFile.ShortPart
                    );
                    continue;
                }

                var templateDescriptor = this._templatesFileService.LoadTemplate(templatePathDescriptor);

                var renderedTemplate = MustacheTemplateRenderer.RenderTemplate(templateDescriptor);

                bool fileWasChanged = this._templatesFileService.WriteRenderedTemplate(
                    templatePathDescriptor,
                    renderedTemplate,
                    templateDescriptor.TemplateFileEncoding,
                    onlyWriteFileIfContentsHaveChanged: true
                );

                if (fileWasChanged)
                {
                    logger.LogMessage(
                        "Template target file '{0}': updated from template '{1}'",
                        templatePathDescriptor.PathToOutputFile.ShortPart,
                        templatePathDescriptor.PathToMustacheFile.ShortPart
                    );
                }
                else
                {
                    logger.LogMessage("Template target file '{0}': already up-to-date", templatePathDescriptor.PathToOutputFile.ShortPart);
                }
            }
            catch (Exception ex)
            {
                logger.LogErrorFromException(ex);
                success = false;
            }
        }

        return success;
    }
}
