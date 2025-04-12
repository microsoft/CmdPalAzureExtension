// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Controls.Forms;
using AzureExtension.Controls.ListItems;
using AzureExtension.Controls.Pages;
using AzureExtension.Data;
using AzureExtension.DataManager;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Configuration;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using Windows.ApplicationModel.Activation;
using Windows.Management.Deployment;
using Windows.Storage;
using Log = Serilog.Log;

namespace AzureExtension;

public sealed class Program
{
    [MTAThread]
    public static async Task Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Setup Logging
        Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", ApplicationData.Current.TemporaryFolder.Path);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Log.Information($"Launched with args: {string.Join(' ', args.ToArray())}");
        LogPackageInformation();

        // Force the app to be single instanced.
        // Get or register the main instance.
        var mainInstance = AppInstance.FindOrRegisterForKey("mainInstance");
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (!mainInstance.IsCurrent)
        {
            Log.Information($"Not main instance, redirecting.");
            await mainInstance.RedirectActivationToAsync(activationArgs);
            Log.CloseAndFlush();
            return;
        }

        // Register for activation redirection.
        AppInstance.GetCurrent().Activated += AppActivationRedirectedAsync;

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            await HandleCOMServerActivation();
        }
        else
        {
            Log.Warning("Not being launched as a ComServer... exiting.");
        }

        Log.CloseAndFlush();
    }

    private static async void AppActivationRedirectedAsync(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments activationArgs)
    {
        Log.Information($"Redirected with kind: {activationArgs.Kind}");

        // Handle COM server.
        if (activationArgs.Kind == ExtendedActivationKind.Launch)
        {
            var d = activationArgs.Data as ILaunchActivatedEventArgs;
            var args = d?.Arguments.Split();

            if (args?.Length > 1 && args.Contains("-RegisterProcessAsComServer"))
            {
                Log.Information($"Activation COM Registration Redirect: {string.Join(' ', args.ToList())}");
                await HandleCOMServerActivation();
            }
        }

        // Handle Protocol.
        if (activationArgs.Kind == ExtendedActivationKind.Protocol)
        {
            var d = activationArgs.Data as IProtocolActivatedEventArgs;
            if (d is not null)
            {
                Log.Information($"Protocol Activation redirected from: {d.Uri}");
                HandleProtocolActivation(d.Uri);
            }
        }
    }

    private static void HandleProtocolActivation(Uri oauthRedirectUri)
    {
        Log.Error($"Protocol Activation not implemented.");
    }

    private static async Task HandleCOMServerActivation()
    {
        Log.Information($"Activating COM Server");

        // Register and run COM server.
        // This could be called by either of the COM registrations, we will do them all to avoid deadlock and bind all on the extension's lifetime.
        await using global::Shmuelie.WinRTServer.ComServer server = new();
        var extensionDisposedEvent = new ManualResetEvent(false);

        var authenticationSettings = new AuthenticationSettings();
        authenticationSettings.InitializeSettings();
        var accountProvider = new AccountProvider(authenticationSettings);

        // In the case that this is the first launch we will try to automatically connect the default Windows account
        await accountProvider.EnableSSOForAzureExtensionAsync();

        var azureClientProvider = new AzureClientProvider(accountProvider);
        var azureClientHelpers = new AzureClientHelpers(azureClientProvider);

        var dataStoreFolderPath = ApplicationData.Current.LocalFolder.Path;

        var combinedCachePath = Path.Combine(dataStoreFolderPath, "AzureData.db");
        var cacheDataStoreSchema = new AzureDataStoreSchema();
        using var cacheDataStore = new DataStore("DataStore", combinedCachePath, cacheDataStoreSchema);
        cacheDataStore.Create();

        var cache = new AzureDataManager(cacheDataStore, accountProvider, azureClientProvider);
        var dataProvider = new DataProvider(cache, cacheDataStore);

        var azureValidator = new AzureValidatorAdapter(azureClientHelpers);

        var combinedPersistendDataStorePath = Path.Combine(dataStoreFolderPath, "PersistentAzureData.db");
        var persistentDataStoreSchema = new PersistentDataSchema();
        using var persistentDataStore = new DataStore("PersistentDataStore", combinedPersistendDataStorePath, persistentDataStoreSchema);
        persistentDataStore.Create();

        var persistentDataManager = new PersistentDataManager(persistentDataStore, azureValidator);

        var path = ResourceLoader.GetDefaultResourceFilePath();
        var resourceLoader = new ResourceLoader(path);
        var resources = new Resources(resourceLoader);

        var timeSpanHelper = new TimeSpanHelper(resources);

        var signInForm = new SignInForm(accountProvider, azureClientHelpers);
        var signInPage = new SignInPage(signInForm, new StatusMessage(), resources.GetResource("Message_Sign_In_Success"), resources.GetResource("Message_Sign_In_Fail"));
        var signOutForm = new SignOutForm(accountProvider, resources);
        var signOutPage = new SignOutPage(signOutForm, new StatusMessage(), resources.GetResource("Message_Sign_Out_Success"), resources.GetResource("Message_Sign_Out_Fail"));

        var savedQueriesMediator = new SavedQueriesMediator();

        var addQueryForm = new SaveQueryForm(resources, savedQueriesMediator, accountProvider, azureClientHelpers, persistentDataManager);
        var addQueryListItem = new AddQueryListItem(new SaveQueryPage(addQueryForm, new StatusMessage(), resources.GetResource("Message_Search_Saved"), resources.GetResource("Message_Search_Saved_Error"), resources.GetResource("ListItems_AddSearch")), resources);
        var savedQueriesPage = new SavedQueriesPage(resources, addQueryListItem, savedQueriesMediator, dataProvider, accountProvider, azureClientHelpers, persistentDataManager, timeSpanHelper);

        var commandProvider = new AzureExtensionCommandProvider(signInPage, signOutPage, accountProvider, savedQueriesPage, resources, azureClientHelpers);

        var extensionInstance = new AzureExtension(extensionDisposedEvent, commandProvider);

        server.RegisterClass<AzureExtension, IExtension>(() => extensionInstance);
        server.Start();

        // This will make the main thread wait until the event is signaled by the extension class.
        // Since we have single instance of the extension object, we exit as soon as it is disposed.
        extensionDisposedEvent.WaitOne();
        Log.Information($"Extension is disposed.");
    }

    private static void LogPackageInformation()
    {
        var relatedPackageFamilyNames = new string[]
        {
              "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy",
              "Microsoft.Windows.CommandPalette_8wekyb3d8bbwe",
              "Microsoft.Windows.AzureExtension_8wekyb3d8bbwe",
              "Microsoft.Windows.AzureExtension.Dev_8wekyb3d8bbwe",
        };

        try
        {
            var packageManager = new PackageManager();
            foreach (var pfn in relatedPackageFamilyNames)
            {
                foreach (var package in packageManager.FindPackagesForUser(string.Empty, pfn))
                {
                    Log.Information($"{package.Id.FullName}  DevMode: {package.IsDevelopmentMode}  Signature: {package.SignatureKind}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Failed getting package information.");
        }
    }
}
