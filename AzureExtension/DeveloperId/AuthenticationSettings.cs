// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Storage;

namespace AzureExtension.DeveloperId;

public class AuthenticationSettings
{
    private readonly string _cacheFolderPathDefault = Path.Combine(Path.GetTempPath(), "AzureExtension");
    private string? _cacheFolderPath;

    public string Authority
    {
        get; private set;
    }

    public string ClientId
    {
        get; private set;
    }

    public string TenantId
    {
        get; private set;
    }

    public string RedirectURI
    {
        get; private set;
    }

    public string CacheFileName
    {
        get; private set;
    }

    public string CacheDir
    {
        get => _cacheFolderPath is null ? _cacheFolderPathDefault : _cacheFolderPath;
        private set => _cacheFolderPath = string.IsNullOrEmpty(value) ? _cacheFolderPathDefault : value;
    }

    public string Scopes
    {
        get; private set;
    }

    public string[] ScopesArray => Scopes.Split(' ');

    public AuthenticationSettings()
    {
        Authority = string.Empty;
        ClientId = string.Empty;
        TenantId = string.Empty;
        RedirectURI = string.Empty;
        CacheFileName = string.Empty;
        CacheDir = string.Empty;
        Scopes = string.Empty;
    }

    public void InitializeSettings()
    {
        Authority = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47";
        ClientId = "ec33db7f-5b7e-4061-b729-8dab727cc764";
        TenantId = string.Empty;
        RedirectURI = "devhome://oauth_redirect_uri/";
        CacheFileName = "msal_cache";
        CacheDir = ApplicationData.Current != null ? ApplicationData.Current.LocalFolder.Path : _cacheFolderPathDefault;
        Scopes = "https://graph.microsoft.com/User.Read";
    }
}
