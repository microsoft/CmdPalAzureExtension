// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;

namespace AzureExtension.Helpers;

public class Resources : IResources
{
    private const int MaxBufferLength = 1024;

    private readonly ResourceLoader _resourceLoader;

    public Resources(ResourceLoader resourceLoader)
    {
        _resourceLoader = resourceLoader;
    }

    public string GetResource(string identifier, ILogger? log = null)
    {
        try
        {
            return _resourceLoader.GetString(identifier);
        }
        catch (Exception ex)
        {
            log?.Error(ex, $"Failed loading resource: {identifier}");

            // If we fail, load the original identifier so it is obvious which resource is missing.
            return identifier;
        }
    }

    // Replaces all identifiers in the provided list in the target string. Assumes all identifiers
    // are wrapped with '%' to prevent sub-string replacement errors. This is intended for strings
    // such as a JSON string with resource identifiers embedded.
    public string ReplaceIdentifiers(string str, string[] resourceIdentifiers, ILogger? log = null)
    {
        var start = DateTime.UtcNow;
        foreach (var identifier in resourceIdentifiers)
        {
            // What is faster, String.Replace, RegEx, or StringBuilder.Replace? It is String.Replace().
            // https://learn.microsoft.com/archive/blogs/debuggingtoolbox/comparing-regex-replace-string-replace-and-stringbuilder-replace-which-has-better-performance
            var resourceString = GetResource(identifier, log);
            str = str.Replace($"%{identifier}%", resourceString);
        }

        var elapsed = DateTime.UtcNow - start;
        log?.Debug($"Replaced identifiers in {elapsed.TotalMilliseconds}ms");
        return str;
    }
}

public interface IResources
{
    string GetResource(string identifier, ILogger? log = null);

    string ReplaceIdentifiers(string str, string[] resourceIdentifiers, ILogger? log = null);
}
