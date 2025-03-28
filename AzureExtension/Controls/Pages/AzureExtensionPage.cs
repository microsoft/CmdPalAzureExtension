// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using AzureExtension.Client;
using AzureExtension.DataManager;
using AzureExtension.DataModel;
using AzureExtension.DeveloperId;
using AzureExtension.Helpers;
using CommandPaletteAzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Windows.Widgets.Providers;
using Newtonsoft.Json;
using Serilog;

namespace AzureExtension.Controls.Pages;

public partial class AzureExtensionPage : ListPage
{
    private readonly Lazy<ILogger> _log;

    protected ILogger Log => _log.Value;

    private string _widgetTitle = string.Empty;
    private string _selectedQueryUrl = string.Empty;
    private string _selectedQueryId = string.Empty;
    private string? _message;

    protected static readonly TimeSpan WidgetDataRequestMinTime = TimeSpan.FromSeconds(30);
    protected static readonly TimeSpan WidgetRefreshRate = TimeSpan.FromMinutes(5);
    protected static readonly string EmptyJson = new JsonObject().ToJsonString();

    protected IAzureDataManager? DataManager { get; private set; }

    private readonly IDeveloperIdProvider _developerIdProvider;

    protected string DataErrorMessage { get; set; } = string.Empty;

    protected string ContentData { get; set; } = EmptyJson;

    protected string LoadingMessage { get; set; } = string.Empty;

    protected string DeveloperLoginId { get; set; } = string.Empty;

    protected bool DeveloperIdLoginRequired { get; set; } = true;

    protected bool LoadedDataSuccessfully { get; set; }

    protected bool CanSave
    {
        get; set;
    }

    protected bool Pinned
    {
        get; set;
    }

    protected bool Enabled
    {
        get; set;
    }

    protected string SavedConfigurationData { get; set; } = string.Empty;

    protected DateTime LastUpdated { get; set; } = DateTime.MinValue;

    protected DataUpdater? DataUpdater { get; private set; }

    public AzureExtensionPage(IDeveloperIdProvider developerIdProvider)
    {
        _developerIdProvider = developerIdProvider;
        Icon = new(string.Empty);
        Name = "Azure extension for cmdpal";
        _log = new(() => Serilog.Log.ForContext("SourceContext", nameof(AzureExtensionPage)));
    }

    public override IListItem[] GetItems()
    {
        RequestContentData();

        var contentDataObject = JsonNode.Parse(ContentData);
        var listItems = new List<ListItem>();
        if (contentDataObject != null && contentDataObject["items"] is JsonArray itemsArray)
        {
            foreach (var item in itemsArray)
            {
                var title = item?["title"]?.GetValue<string>() ?? "No Title";
                var icon = item?["icon"]?.GetValue<string>() ?? string.Empty;
                listItems.Add(new ListItem(new NoOpCommand()) { Title = title, Icon = new IconInfo(icon) });
            }
        }

        return listItems.ToArray();
    }

    // Helper methods
    private string GetIconForType(string? workItemType)
    {
        return workItemType switch
        {
            "Bug" => IconLoader.GetIconAsBase64("Bug.png"),
            "Feature" => IconLoader.GetIconAsBase64("Feature.png"),
            "Issue" => IconLoader.GetIconAsBase64("Issue.png"),
            "Impediment" => IconLoader.GetIconAsBase64("Impediment.png"),
            "Pull Request" => IconLoader.GetIconAsBase64("PullRequest.png"),
            "Task" => IconLoader.GetIconAsBase64("Task.png"),
            _ => IconLoader.GetIconAsBase64("ADO.png"),
        };
    }

    private string GetIconForStatusState(string? statusState)
    {
        return statusState switch
        {
            "Closed" or "Completed" => IconLoader.GetIconAsBase64("StatusGreen.png"),
            "Committed" or "Resolved" or "Started" => IconLoader.GetIconAsBase64("StatusBlue.png"),
            _ => IconLoader.GetIconAsBase64("StatusGray.png"),
        };
    }

    protected bool ValidateConfiguration(WidgetActionInvokedArgs args)
    {
        var data = args.Data;
        var dataObject = JsonNode.Parse(data);
        _message = null;

        if (dataObject != null && dataObject["account"] != null && dataObject["query"] != null)
        {
            _widgetTitle = dataObject["widgetTitle"]?.GetValue<string>() ?? string.Empty;
            _selectedQueryUrl = dataObject["query"]?.GetValue<string>() ?? string.Empty;
            DeveloperLoginId = dataObject["account"]?.GetValue<string>() ?? string.Empty;
            SetDefaultDeveloperLoginId();
            if (DeveloperLoginId != dataObject["account"]?.GetValue<string>())
            {
                dataObject["account"] = DeveloperLoginId;
                data = dataObject.ToJsonString();
            }

            var developerId = GetDevId(DeveloperLoginId);
            if (developerId == null)
            {
                _message = Resources.GetResource(@"Widget_Template/DevIDError");
                return false;
            }

            var queryInfo = AzureClientHelpers.GetQueryInfo(new AzureUri(_selectedQueryUrl), developerId);
            _selectedQueryId = queryInfo.AzureUri.Query;   // This will be empty string if invalid query.
            if (queryInfo.Result != ResultType.Success)
            {
                _message = GetMessageForError(queryInfo.Error, queryInfo.ErrorMessage);

                return false;
            }
            else
            {
                CanSave = true;
                Pinned = true;
                if (string.IsNullOrEmpty(_widgetTitle))
                {
                    _widgetTitle = queryInfo.Name;
                }

                return true;
            }
        }

        return false;
    }

    // Increase precision of SetDefaultDeveloperLoginId by matching the selectedQueryUrl's org
    // with the first matching DeveloperId that contains that org.
    protected void SetDefaultDeveloperLoginId()
    {
        SetDefaultDeveloperLoginId();

        var azureOrg = new AzureUri(_selectedQueryUrl).Organization;
        if (!string.IsNullOrEmpty(azureOrg))
        {
            var devIds = _developerIdProvider.GetLoggedInDeveloperIds().DeveloperIds;
            if (devIds is null)
            {
                return;
            }

            DeveloperLoginId = devIds.Where(i => i.LoginId.Contains(azureOrg, StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.LoginId ?? DeveloperLoginId;
        }
    }

    // Data loading methods
    public void HandleDataManagerUpdate(object? source, DataManagerUpdateEventArgs e)
    {
        if (e.Requestor.ToString() == Id)
        {
            Log.Debug($"Received matching query update");

            if (e.Kind == DataManagerUpdateKind.Error)
            {
                DataErrorMessage = e.Context.ErrorMessage;

                // The DataManager log will have detailed exception info, use the short message.
                Log.Error($"Data update failed. {e.Context.QueryId} {e.Context.ErrorMessage}");
                return;
            }

            LoadContentData();
        }
    }

    public void RequestContentData()
    {
        var developerId = GetDevId(DeveloperLoginId);
        if (developerId == null)
        {
            // Should not happen
            Log.Error("Failed to get DeveloperId");
            return;
        }

        var requestOptions = RequestOptions.RequestOptionsDefault();

        try
        {
            var azureUri = new AzureUri(_selectedQueryUrl);
            DataManager!.UpdateDataForQueryAsync(azureUri, developerId.LoginId, requestOptions, new Guid(Id));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed requesting data update.");
        }
    }

    protected void ResetDataFromState(string data)
    {
        var dataObject = JsonNode.Parse(data);

        if (dataObject == null)
        {
            return;
        }

        _widgetTitle = dataObject["widgetTitle"]?.GetValue<string>() ?? string.Empty;
        DeveloperLoginId = dataObject["account"]?.GetValue<string>() ?? string.Empty;
        _selectedQueryUrl = dataObject["query"]?.GetValue<string>() ?? string.Empty;
        _message = null;

        var developerId = GetDevId(DeveloperLoginId);
        if (developerId == null)
        {
            return;
        }

        var azureUri = new AzureUri(_selectedQueryUrl);
        _selectedQueryId = azureUri.Query;
        if (!azureUri.IsQuery)
        {
            Log.Error("Selected Query Url from ResetDataFromState is not a valid query.");
        }
    }

    public string GetConfiguration(string data)
    {
        var configurationData = new JsonObject();

        var developerIdsData = new JsonArray();

        var devIdProvider = _developerIdProvider;

        foreach (var developerId in devIdProvider.GetLoggedInDeveloperIds().DeveloperIds)
        {
            developerIdsData.Add(new JsonObject
            {
                { "devId", developerId.LoginId },
            });
        }

        configurationData.Add("accounts", developerIdsData);

        configurationData.Add("selectedDevId", DeveloperLoginId);
        configurationData.Add("url", _selectedQueryUrl);
        configurationData.Add("message", _message);
        configurationData.Add("widgetTitle", _widgetTitle);

        configurationData.Add("pinned", Pinned);
        configurationData.Add("arrow", IconLoader.GetIconAsBase64("arrow.png"));

        return configurationData.ToString();
    }

    public void LoadContentData()
    {
        try
        {
            var developerId = GetDevId(DeveloperLoginId);
            if (developerId == null)
            {
                // Should not happen, but may be possible in situations where the app is removed and
                // the signed in account is not silently restorable.
                // This is also checked before on UpdateActivityState() method on base class.
                Log.Error("Failed to get Dev ID");
                return;
            }

            var azureUri = new AzureUri(_selectedQueryUrl);

            if (!azureUri.IsQuery)
            {
                // This should never happen. Already was validated on configuration.
                Log.Error($"Invalid Uri: {_selectedQueryUrl}");
                return;
            }

            // This can throw if DataStore is not connected.
            var queryInfo = DataManager!.GetQuery(azureUri, developerId.LoginId);

            var queryResults = queryInfo is null ? new Dictionary<string, object>() : JsonConvert.DeserializeObject<Dictionary<string, object>>(queryInfo.QueryResults);

            var itemsData = new JsonObject();
            var itemsArray = new JsonArray();

            foreach (var element in queryResults!)
            {
                var workItem = JsonNode.Parse(element.Value.ToStringInvariant());

                if (workItem != null)
                {
                    // If we can't get the real date, it is better better to show a recent
                    // closer-to-correct time than the zero value decades ago, so use DateTime.UtcNow.
                    var dateTicks = workItem["System.ChangedDate"]?.GetValue<long>() ?? DateTime.UtcNow.Ticks;
                    var dateTime = dateTicks.ToDateTime();
                    var creator = DataManager.GetIdentity(workItem["System.CreatedBy"]?.GetValue<long>() ?? 0L);
                    var workItemType = DataManager.GetWorkItemType(workItem["System.WorkItemType"]?.GetValue<long>() ?? 0L);
                    var item = new JsonObject
                    {
                        { "title", workItem["System.Title"]?.GetValue<string>() ?? string.Empty },
                        { "url", workItem[AzureDataManager.WorkItemHtmlUrlFieldName]?.GetValue<string>() ?? string.Empty },
                        { "icon", GetIconForType(workItemType.Name) },
                        { "status_icon", GetIconForStatusState(workItem["System.State"]?.GetValue<string>()) },
                        { "number", element.Key },
                        { "date", TimeSpanHelper.DateTimeOffsetToDisplayString(dateTime, Log) },
                        { "user", creator.Name },
                        { "status", workItem["System.State"]?.GetValue<string>() ?? string.Empty },
                        { "avatar", creator.Avatar },
                    };

                    itemsArray.Add(item);
                }
            }

            itemsData.Add("workItemCount", queryInfo is null ? 0 : (int)queryInfo.QueryResultCount);
            itemsData.Add("maxItemsDisplayed", AzureDataManager.QueryResultLimit);
            itemsData.Add("items", itemsArray);
            itemsData.Add("widgetTitle", _widgetTitle);

            ContentData = itemsData.ToJsonString();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error retrieving data.");
            return;
        }
    }

    protected IDeveloperId? GetDevId(string login)
    {
        var devIdProvider = _developerIdProvider;
        DeveloperId.DeveloperId? developerId = null;

        foreach (var devId in devIdProvider.GetLoggedInDeveloperIds().DeveloperIds)
        {
            if (devId.LoginId == login)
            {
                developerId = (DeveloperId.DeveloperId)devId;
            }
        }

        return developerId;
    }

    protected string GetMessageForError(ErrorType errorType, string? fallback = null)
    {
        var identifier = $"ErrorMessage/{errorType}";
        var message = errorType switch
        {
            ErrorType.None => string.Empty,
            _ => Resources.GetResource(identifier, Log),
        };

        // If identifier and message are different, then it means we have a specific message to
        // display for this error.
        if (message != identifier)
        {
            return message;
        }

        // Otherwise we do not have a specific error defined and should use the fallback if it
        // exists. If no fallback use the name of the error type.
        return fallback ?? errorType.ToString();
    }
}
