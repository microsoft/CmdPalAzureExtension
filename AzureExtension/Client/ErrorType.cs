// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AzureExtension.Client;

public enum ErrorType
{
    None,
    Unknown,
    InvalidArgument,
    EmptyUri,
    InvalidQueryUri,
    TemporaryQueryUriNotSupported,
    InvalidRepositoryUri,
    InvalidDefinitionUri,
    InvalidUri,
    NullDeveloperId,
    InvalidDeveloperId,
    QueryFailed,
    RepositoryFailed,
    FailedGettingClient,
    CredentialUIRequired,
    MsalServiceError,
    MsalClientError,
    GenericCredentialFailure,
    InitializeVssConnectionFailure,
    NullConnection,
    VssResourceNotFound,
    DefinitionNotFound,
}
