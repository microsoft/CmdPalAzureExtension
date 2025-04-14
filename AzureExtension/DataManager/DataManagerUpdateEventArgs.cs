// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager;

public delegate void DataManagerUpdateEventHandler(object? source, DataManagerUpdateEventArgs e);

public enum DataManagerUpdateKind
{
    Cancel,         // Update was cancelled.
    Error,          // An error occurred during update.
    Success,        // Update was successful.
}

public class DataManagerUpdateEventArgs : EventArgs
{
    private readonly DataManagerUpdateKind _kind;
    private readonly DataUpdateParameters _parameters;
    private readonly Exception? _exception;

    public DataManagerUpdateEventArgs(DataManagerUpdateKind updateKind, DataUpdateParameters parameters, Exception? exception = null)
    {
        _kind = updateKind;
        _parameters = parameters;
        _exception = exception;
    }

    public DataManagerUpdateKind Kind => _kind;

    public DataUpdateParameters Parameters => _parameters;

    public Exception? Exception => _exception;
}
