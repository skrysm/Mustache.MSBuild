// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using AppMotor.NetStandardCompatKit.Extensions;

using JetBrains.Annotations;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mustache.MSBuild.Utils;

/// <summary>
/// JSON reader for template data files.
/// </summary>
internal static class DataFileJsonReader
{
    [MustUseReturnValue]
    public static JObject DeserializeObject(string json, string dataFileName)
    {
        // If the json is empty, DeserializeObject() below would return "null". We don't want this.
        if (json.IsNullOrWhiteSpace())
        {
            throw new ErrorMessageException($"The data file '{dataFileName}' is empty.");
        }

        try
        {
            var deserializedData = JsonConvert.DeserializeObject(json);

            if (deserializedData is JObject deserializedObject)
            {
                return deserializedObject;
            }
            else
            {
                throw new ErrorMessageException($"The content of data file '{dataFileName}' is not an object but a {deserializedData!.GetType().Name}.");
            }
        }
        catch (JsonSerializationException ex)
        {
            throw new ErrorMessageException($"The content of data file '{dataFileName}' is invalid: {ex.Message}");
        }
    }

    [MustUseReturnValue]
    public static T DeserializeObject<T>(string json, string dataFileName)
    {
        // If the json is empty, DeserializeObject() below would return "null". We don't want this.
        if (json.IsNullOrWhiteSpace())
        {
            throw new ErrorMessageException($"The data file '{dataFileName}' is empty.");
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(json)!;
        }
        catch (JsonSerializationException ex)
        {
            throw new ErrorMessageException($"The content of data file '{dataFileName}' is invalid: {ex.Message}");
        }
    }
}
