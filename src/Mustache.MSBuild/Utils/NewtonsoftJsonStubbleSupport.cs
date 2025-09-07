// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using Newtonsoft.Json.Linq;

using Stubble.Core.Settings;

namespace Mustache.MSBuild.Utils;

internal static class NewtonsoftJsonStubbleSupport
{
    // NOTE: Code of this class is adopted from: https://github.com/StubbleOrg/Stubble.Extensions.JsonNet/blob/master/src/Stubble.Extensions.JsonNet/JsonNet.cs

    public static void AddNewtonsoftJson(this RendererSettingsBuilder builder)
    {
        builder.AddValueGetter(typeof(JObject), GetValueFromJObject);

        // NOTE: This is (somehow) required to make sub objects work correctly.
        //   Apparently, a section is something like "{{#users}}" and the section
        //   blacklist prevents Stubble from applying the default interpretation
        //   for sections of type "JObject" (and instead use the value getter defined
        //   above).
        builder.AddSectionBlacklistType(typeof(JObject));
    }

    private static object? GetValueFromJObject(object container, string key, bool ignoreCase)
    {
        var jObject = (JObject)container;

        var property = jObject.Property(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        if (property is null)
        {
            // NOTE: While it would be possible to return "{{key}}" here (i.e. making unresolved tokens
            //   appear as they were declared), this only works (properly) for "{{token}}" but not, for example,
            //   for loop tokens like "{{#users}}" (since we can't get the "#" char here). So instead we
            //   keep the "default" and return "null".
            return null;
        }

        var value = property.Value;

        switch (value.Type)
        {
            case JTokenType.Array:
            case JTokenType.Object:
                return value;
        }

        return (value as JValue)?.Value;
    }
}
