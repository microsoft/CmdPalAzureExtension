﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.DataManager.Cache;

public delegate void CacheManagerUpdateEventHandler(object? source, CacheManagerUpdateEventArgs e);

public enum CacheManagerUpdateKind
{
    Started,
    Updated,
    Cleared,
    Error,
    Cancel,
    Account,
}

public class CacheManagerUpdateEventArgs : EventArgs
{
    private readonly CacheManagerUpdateKind _kind;
    private readonly Exception? _exception;
    private readonly DataUpdateParameters? _dataUpdateParameters;

    public CacheManagerUpdateEventArgs(CacheManagerUpdateKind updateKind, Exception? exception = null)
    {
        _kind = updateKind;
        _exception = exception;
    }

    public CacheManagerUpdateEventArgs(CacheManagerUpdateKind updateKind, DataUpdateParameters dataUpdateParameters, Exception? exception = null)
    {
        _kind = updateKind;
        _exception = exception;
        _dataUpdateParameters = dataUpdateParameters;
    }

    public CacheManagerUpdateKind Kind => _kind;

    public Exception? Exception => _exception;

    public DataUpdateParameters? DataUpdateParameters => _dataUpdateParameters;
}
