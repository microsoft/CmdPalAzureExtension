// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using AzureExtension.Account;
using AzureExtension.Client;
using AzureExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Identity.Client;

namespace AzureExtension.Controls.Commands;

public partial class RemoveCommand : InvokableCommand
{
    private readonly IAzureSearch _savedAzureSearch;
    private readonly IResources _resources;
    private readonly SavedAzureSearchesMediator _savedAzureSearchesMediator;
    private readonly IAzureSearchRepository _azureSearchRepository;
    private readonly IAccountProvider _accountProvider;
    private readonly string _successMessage;
    private readonly string _failureMessage;
    private readonly AzureClientHelpers _azureClientHelpers;

    public RemoveCommand(
        IAzureSearch azureSearch,
        IResources resources,
        SavedAzureSearchesMediator savedAzureSearchesMediator,
        IAzureSearchRepository azureSearchRepository,
        string successMessage,
        string failureMessage,
        AzureClientHelpers azureClientHelpers,
        IAccountProvider accountProvider)
    {
        _resources = resources;
        _savedAzureSearchesMediator = savedAzureSearchesMediator;
        _azureSearchRepository = azureSearchRepository;
        _savedAzureSearch = azureSearch;
        Name = GetCommandNameFromSearchType();
        Icon = IconLoader.GetIcon("Remove");
        _successMessage = successMessage;
        _failureMessage = failureMessage;
        _azureClientHelpers = azureClientHelpers;
        _accountProvider = accountProvider;
    }

    public override CommandResult Invoke()
    {
        try
        {
            _azureSearchRepository.Remove(_savedAzureSearch);
            _savedAzureSearchesMediator.Remove(_savedAzureSearch);
            var name = string.Empty;

            if (_savedAzureSearch is IPipelineDefinitionSearch pipelineSearch)
            {
                name = SearchHelper.GetPipelineSearchName(pipelineSearch, _azureClientHelpers, _accountProvider);
            }

            ToastHelper.ShowSuccessToast(string.Format(CultureInfo.CurrentCulture, _successMessage, string.IsNullOrWhiteSpace(name) ? _savedAzureSearch.Name : name));
        }
        catch (Exception ex)
        {
            ToastHelper.ShowErrorToast(string.Format(CultureInfo.CurrentCulture, _failureMessage, _savedAzureSearch.Name, ex.Message));
        }

        return CommandResult.KeepOpen();
    }

    private string GetCommandNameFromSearchType()
    {
        return _savedAzureSearch switch
        {
            IQuerySearch => _resources.GetResource("Commands_Remove_Query"),
            IPullRequestSearch => _resources.GetResource("Commands_Remove_PullRequestSearch"),
            IPipelineDefinitionSearch => _resources.GetResource("Commands_Remove_PipelineSearch"),
            _ => _resources.GetResource("Commands_Remove"),
        };
    }
}
