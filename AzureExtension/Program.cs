// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Controls.Forms;
using AzureExtension.Controls.Pages;
using AzureExtension.DataManager;
using AzureExtension.DataModel;
using AzureExtension.DeveloperId;
using CommandPaletteAzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Configuration;
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

            if (args?.Length > 1 && args[1] == "-RegisterProcessAsComServer")
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

        // We may have received an event on previous launch that the datastore should be recreated.
        // It should be recreated now before anything else tries to use it.

        // In the case that this is the first launch we will try to automatically connect the default Windows account
        var authenticationHelper = new AuthenticationHelper();
        var devIdProvider = new DeveloperIdProvider(authenticationHelper);
        devIdProvider.EnableSSOForAzureExtensionAsync();

        RecreateDataStoreIfNecessary(devIdProvider);

        using var azureDataManager = new AzureDataManager("MainInstance", devIdProvider);

        // Cache manager updates account data.
        // using var cacheManager = new CacheManager(azureDataManager, devIdProvider);
        // cacheManager.Start();

        // Set up the data updater. This will schedule updating the Developer Pull Requests.
        // using var dataUpdater = new DataUpdater(azureDataManager.Update);
        // _ = dataUpdater.Start();

        // Add an update whenever CacheManager is updated.
        // cacheManager.OnUpdate += HandleCacheUpdate;
        var signInForm = new SignInForm(devIdProvider);
        var signInPage = new SignInPage(signInForm, new StatusMessage(), Resources.GetResource("Message_Sign_In_Success"), Resources.GetResource("Message_Sign_In_Fail"), devIdProvider);

        var commandProvider = new AzureExtensionCommandProvider(signInPage);

        var extensionInstance = new AzureExtension(extensionDisposedEvent, commandProvider);

        server.RegisterClass<AzureExtension, IExtension>(() => extensionInstance);
        server.Start();

        // This will make the main thread wait until the event is signaled by the extension class.
        // Since we have single instance of the extension object, we exit as soon as it is disposed.
        extensionDisposedEvent.WaitOne();
        Log.Information($"Extension is disposed.");
    }

    private static void HandleCacheUpdate(object? source, CacheManagerUpdateEventArgs e)
    {
        if (e.Kind == CacheManagerUpdateKind.Updated)
        {
            Log.Debug("Cache was updated, updating developer pull requests.");

            // _ = AzureDataManager.UpdateDeveloperPullRequests();
            // TODO: THIS CODE SHOULD NOT BE HERE. FIX CYCLIC DEPENDENCY.
        }
    }

    private static void LogPackageInformation()
    {
        var relatedPackageFamilyNames = new string[]
        {
              "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy",
              "Microsoft.Windows.CommandPalette_8wekyb3d8bbwe",
              "Microsoft.Windows.CommandPaletteAzureExtension_8wekyb3d8bbwe",
              "Microsoft.Windows.CommandPaletteAzureExtension.Dev_8wekyb3d8bbwe",
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

    private static void RecreateDataStoreIfNecessary(IDeveloperIdProvider developerIdProvider)
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(AzureDataManager.RecreateDataStoreSettingsKey, out var recreateDataStore))
            {
                if ((bool)recreateDataStore)
                {
                    Log.Information("Recreating DataStore");

                    // Creating an instance of AzureDataManager with the recreate option will
                    // attempt to recreate the datastore. A new options is created to avoid
                    // altering the default options since it is a singleton.
                    var dataStoreOptions = new DataStoreOptions
                    {
                        DataStoreFileName = AzureDataManager.DefaultOptions.DataStoreFileName,
                        DataStoreSchema = AzureDataManager.DefaultOptions.DataStoreSchema,
                        DataStoreFolderPath = AzureDataManager.DefaultOptions.DataStoreFolderPath,
                        RecreateDataStore = true,
                    };

                    using var dataManager = new AzureDataManager("RecreateDataStore", developerIdProvider, dataStoreOptions);

                    // After we get this key once, set it back to false.
                    localSettings.Values[AzureDataManager.RecreateDataStoreSettingsKey] = false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed attempting to verify or perform database recreation.");
        }
    }
}
