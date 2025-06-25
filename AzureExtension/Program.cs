// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Controls;
using AzureExtension.Controls.Commands;
using AzureExtension.Controls.Forms;
using AzureExtension.Controls.ListItems;
using AzureExtension.Controls.Pages;
using AzureExtension.Data;
using AzureExtension.DataManager;
using AzureExtension.DataManager.Cache;
using AzureExtension.DataModel;
using AzureExtension.Helpers;
using AzureExtension.PersistentData;
using Microsoft.CommandPalette.Extensions;
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
        Environment.SetEnvironmentVariable("CMDPAL_LOGS_ROOT", ApplicationData.Current.TemporaryFolder.Path);
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

        var vssConnectionFactory = new VssConnectionFactory();
        using var azureClientProvider = new AzureClientProvider(accountProvider, vssConnectionFactory);
        var azureClientHelpers = new AzureClientHelpers(azureClientProvider);

        var azureValidator = new AzureValidatorAdapter(azureClientHelpers);
        var azureLiveDataProvider = new AzureLiveDataProvider();

        var dataStoreFolderPath = ApplicationData.Current.LocalFolder.Path;

        var combinedPersistentDataStorePath = Path.Combine(dataStoreFolderPath, "PersistentAzureData.db");
        var persistentDataStoreSchema = new PersistentDataSchema();
        using var persistentDataStore = new DataStore("PersistentDataStore", combinedPersistentDataStorePath, persistentDataStoreSchema);

        persistentDataStore.Create();

        var combinedCachePath = Path.Combine(dataStoreFolderPath, "AzureData.db");
        var cacheDataStoreSchema = new AzureCacheDataStoreSchema();
        using var cacheDataStore = new DataStore("DataStore", combinedCachePath, cacheDataStoreSchema);
        cacheDataStore.Create();

        var pipelineProvider = new AzureDataPipelineProvider(cacheDataStore);

        var queryRepository = new QueryRepository(persistentDataStore, azureValidator);
        var pullRequestSearchRepository = new PullRequestSearchRepository(persistentDataStore, azureValidator);
        var pipelineDefinitionRepository = new DefinitionSearchRepository(persistentDataStore, azureValidator);

        var queryManager = new AzureDataQueryManager(cacheDataStore, accountProvider, azureLiveDataProvider, azureClientProvider, queryRepository);
        var pullRequestSearchManager = new AzureDataPullRequestSearchManager(cacheDataStore, accountProvider, azureLiveDataProvider, azureClientProvider, pullRequestSearchRepository);

        var pipelineUpdater = new AzureDataPipelineUpdater(cacheDataStore, accountProvider, azureLiveDataProvider, azureClientProvider, pipelineDefinitionRepository, pipelineProvider);

        var updatersDictionary = new Dictionary<DataUpdateType, IDataUpdater>
        {
            { DataUpdateType.Query, queryManager },
            { DataUpdateType.PullRequests, pullRequestSearchManager },
            { DataUpdateType.Pipeline, pipelineUpdater },
        };

        var azureDataManager = new AzureDataManager(cacheDataStore, updatersDictionary);
        var authenticationMediator = new AuthenticationMediator();
        using var cacheManager = new CacheManager(azureDataManager, authenticationMediator);

        var contentProvidersDictionary = new Dictionary<Type, IContentDataProvider>
        {
            { typeof(IQuerySearch), queryManager },
            { typeof(IPullRequestSearch), pullRequestSearchManager },
            { typeof(IPipelineDefinitionSearch), pipelineProvider },
        };

        var searchDataProvidersDictionary = new Dictionary<Type, ISearchDataProvider>
        {
            { typeof(IQuerySearch), queryManager },
            { typeof(IPullRequestSearch), pullRequestSearchManager },
            { typeof(IPipelineDefinitionSearch), pipelineProvider },
        };

        var dataProvider = new LiveDataProvider(cacheManager, contentProvidersDictionary, searchDataProvidersDictionary);

        var path = ResourceLoader.GetDefaultResourceFilePath();
        var resourceLoader = new ResourceLoader(path);
        var resources = new Resources(resourceLoader);

        var timeSpanHelper = new TimeSpanHelper(resources);

        using var signInCommand = new SignInCommand(resources, accountProvider, authenticationMediator);
        using var signInForm = new SignInForm(authenticationMediator, resources, signInCommand);
        using var signInPage = new SignInPage(signInForm, resources, signInCommand, authenticationMediator);
        using var signOutCommand = new SignOutCommand(resources, accountProvider, authenticationMediator);
        using var signOutForm = new SignOutForm(resources, signOutCommand, authenticationMediator, accountProvider);
        using var signOutPage = new SignOutPage(signOutForm, resources, signOutCommand, authenticationMediator);

        var savedAzureSearchesMediator = new SavedAzureSearchesMediator();

        var azureSearchRepositories = new Dictionary<Type, IAzureSearchRepository>
        {
            { typeof(IQuerySearch), new AzureSearchRepositoryAdapter<IQuerySearch>(queryRepository, queryRepository) },
            { typeof(IPullRequestSearch), new AzureSearchRepositoryAdapter<IPullRequestSearch>(pullRequestSearchRepository, pullRequestSearchRepository) },
            { typeof(IPipelineDefinitionSearch), new AzureSearchRepositoryAdapter<IPipelineDefinitionSearch>(pipelineDefinitionRepository, pipelineDefinitionRepository) },
        };

        // passing null for SavedSearch because there is no standard TSearch type
        var saveQuerySearchCommand = new SaveSearchCommand<IQuerySearch>(queryRepository, savedAzureSearchesMediator, null, resources.GetResource("Message_Query_Saved"), resources.GetResource("Message_Query_Saved_Error"), resources.GetResource("Pages_EditQuery_SuccessMessage"), resources.GetResource("Pages_EditQuery_FailureMessage"));
        var savePullRequestSearchCommand = new SaveSearchCommand<IPullRequestSearch>(pullRequestSearchRepository, savedAzureSearchesMediator, null, resources.GetResource("Messages_PullRequestSearch_Saved"), resources.GetResource("Pages_SavePullRequestSearch_FailureMessage"), resources.GetResource("Pages_EditPullRequestSearch_SuccessMessage"), resources.GetResource("Pages_EditPullRequestSearch_FailureMessage"));
        var savePipelineSearchCommand = new SaveSearchCommand<IPipelineDefinitionSearch>(pipelineDefinitionRepository, savedAzureSearchesMediator, null, resources.GetResource("Pages_SavePipelineSearch_SuccessMessage"), resources.GetResource("Pages_SavePipelineSearch_FailureMessage"), resources.GetResource("Pages_EditPipelineSearch_SuccessMessage"), resources.GetResource("Pages_EditPipelineSearch_FailureMessage"));

        var searchPageFactory = new SearchPageFactory(
            resources,
            savedAzureSearchesMediator,
            accountProvider,
            azureClientHelpers,
            azureSearchRepositories,
            queryRepository,
            pullRequestSearchRepository,
            pipelineDefinitionRepository,
            new ContentDataProviderAdapter<IWorkItem>(dataProvider),
            new ContentDataProviderAdapter<IPullRequest>(dataProvider),
            new ContentDataProviderAdapter<IBuild>(dataProvider),
            new SearchDataProviderAdapter<IDefinition>(dataProvider),
            saveQuerySearchCommand,
            savePullRequestSearchCommand,
            savePipelineSearchCommand);

        var addQueryForm = new SaveQueryForm(null, resources, savedAzureSearchesMediator, accountProvider, azureClientHelpers, queryRepository, saveQuerySearchCommand);
        var addQueryListItem = new AddQueryListItem(new SaveQueryPage(addQueryForm, resources, savedAzureSearchesMediator), resources);
        var savedQueriesPage = new SavedQueriesPage(resources, addQueryListItem, savedAzureSearchesMediator, queryRepository, searchPageFactory);

        var savePullRequestSearchForm = new SavePullRequestSearchForm(null, resources, savedAzureSearchesMediator, accountProvider, azureClientHelpers, pullRequestSearchRepository, savePullRequestSearchCommand);
        var savePullRequestSearchPage = new SavePullRequestSearchPage(savePullRequestSearchForm, resources, savedAzureSearchesMediator);
        var addPullRequestSearchListItem = new AddPullRequestSearchListItem(savePullRequestSearchPage, resources);
        var savedPullRequestSearchesPage = new SavedPullRequestSearchesPage(resources, addPullRequestSearchListItem, savedAzureSearchesMediator, pullRequestSearchRepository, searchPageFactory);

        var savePipelineSearchForm = new SavePipelineSearchForm(null, resources, pipelineDefinitionRepository, savedAzureSearchesMediator, accountProvider, azureClientHelpers, savePipelineSearchCommand);
        var savePipelineSearchPage = new SavePipelineSearchPage(savePipelineSearchForm, resources, savedAzureSearchesMediator);
        var addPipelineSearchListItem = new AddPipelineSearchListItem(savePipelineSearchPage, resources);
        using var savedPipelineSearchesPage = new SavedPipelineSearchesPage(resources, addPipelineSearchListItem, savedAzureSearchesMediator, pipelineDefinitionRepository, accountProvider, new ContentDataProviderAdapter<IBuild>(dataProvider), searchPageFactory);

        using var commandProvider = new AzureExtensionCommandProvider(signInPage, signOutPage, accountProvider, savedQueriesPage, savedPullRequestSearchesPage, searchPageFactory, savedAzureSearchesMediator, authenticationMediator, savedPipelineSearchesPage, dataProvider);

        var extensionInstance = new AzureExtension(extensionDisposedEvent, commandProvider);

        server.RegisterClass<AzureExtension, IExtension>(() => extensionInstance);
        server.Start();

        // This will make the main thread wait until the event is signaled by the extension class.
        // Since we have single instance of the extension object, we exit as soon as it is disposed.
        extensionDisposedEvent.WaitOne();
        server.Stop();
        server.UnsafeDispose();
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
