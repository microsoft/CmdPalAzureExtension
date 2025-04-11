// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataModel;

public class ProviderOperationResult
{
    public ProviderOperationResult(ProviderOperationStatus status, Exception? error, string displayMessage, string diagnosticText)
    {
        Status = status;
        ExtendedError = error;
        DisplayMessage = displayMessage;
        DiagnosticText = diagnosticText;
    }

    public ProviderOperationStatus Status { get; }

    public Exception? ExtendedError { get; }

    public string DisplayMessage { get; }

    public string DiagnosticText { get; }
}

public enum ProviderOperationStatus
{
    Success,
    Failure,
}
