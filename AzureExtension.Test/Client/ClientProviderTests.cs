// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AzureExtension.Account;
using AzureExtension.Client;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;

namespace AzureExtension.Test.Client;

[TestClass]
public class ClientProviderTests
{
    [TestMethod]
    public async Task GetValidVssConnectionWithCache()
    {
        var mockAccountProvider = new Mock<IAccountProvider>();
        var mockFactory = new Mock<IVssConnectionFactory>();
        using var clientProvider = new AzureClientProvider(mockAccountProvider.Object, mockFactory.Object);

        mockAccountProvider.Setup(x => x.GetCredentials(It.IsAny<IAccount>()))
            .Returns(new VssCredentials(new WindowsCredential()));

        var mockConnection = new Mock<IVssConnection>();
        mockConnection.Setup(x => x.AuthorizedIdentity)
            .Returns(new Identity() { Id = Guid.NewGuid() });
        mockConnection.Setup(x => x.HasAuthenticated).Returns(true);

        mockFactory.Setup(x => x.CreateVssConnection(It.IsAny<Uri>(), It.IsAny<VssCredentials>()))
            .Returns(mockConnection.Object);

        var stubAccount = new Mock<IAccount>();
        var uri = new Uri("https://dev.azure.com/yourorganization");
        var firstConnection = await clientProvider.GetVssConnectionAsync(uri, stubAccount.Object);

        Assert.AreEqual(mockConnection.Object, firstConnection);

        var mockConnectionValid = new Mock<IVssConnection>();

        mockFactory.Setup(x => x.CreateVssConnection(It.IsAny<Uri>(), It.IsAny<VssCredentials>()))
            .Returns(mockConnectionValid.Object);

        var secondConnection = await clientProvider.GetVssConnectionAsync(uri, stubAccount.Object);

        Assert.AreEqual(mockConnection.Object, secondConnection);
    }

    [TestMethod]
    public async Task GetInvalidVssConnectionWithCache()
    {
        var mockAccountProvider = new Mock<IAccountProvider>();
        var mockFactory = new Mock<IVssConnectionFactory>();
        using var clientProvider = new AzureClientProvider(mockAccountProvider.Object, mockFactory.Object);

        mockAccountProvider.Setup(x => x.GetCredentials(It.IsAny<IAccount>()))
            .Returns(new VssCredentials(new WindowsCredential()));

        var mockConnection = new Mock<IVssConnection>();

        mockFactory.Setup(x => x.CreateVssConnection(It.IsAny<Uri>(), It.IsAny<VssCredentials>()))
            .Returns(mockConnection.Object);

        var uri = new Uri("https://dev.azure.com/yourorganization");
        var stubAccount = new Mock<IAccount>();
        var firstConnection = await clientProvider.GetVssConnectionAsync(uri, stubAccount.Object);

        Assert.AreEqual(mockConnection.Object, firstConnection);

        mockConnection.Setup(x => x.AuthorizedIdentity)
            .Throws(new VssUnauthorizedException("Unauthorized"));

        var mockConnectionValid = new Mock<IVssConnection>();

        mockFactory.Setup(x => x.CreateVssConnection(It.IsAny<Uri>(), It.IsAny<VssCredentials>()))
            .Returns(mockConnectionValid.Object);

        var secondConnection = await clientProvider.GetVssConnectionAsync(uri, stubAccount.Object);

        Assert.AreEqual(mockConnectionValid.Object, secondConnection);
    }
}
