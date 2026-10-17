using FluentAssertions;
using JassTournamentManager.App.Infrastructure.Api;
using Xunit;

namespace JassTournamentManager.App.Tests.Infrastructure;

public sealed class ApiOptionsTests
{
    [Fact]
    public void BaseAddress_WithExplicitValue_PreservesProvidedUri()
    {
        var address = new Uri("https://api.example.test/service/");
        var options = new ApiOptions { BaseAddress = address };

        options.BaseAddress.Should().BeSameAs(address);
    }

    [Fact]
    public void BaseAddress_ByDefault_UsesPlatformDevelopmentEndpoint()
    {
#if ANDROID
        var expected = new Uri("http://10.0.2.2:5272");
#else
        var expected = new Uri("http://localhost:5272");
#endif
        new ApiOptions().BaseAddress.Should().Be(expected);
    }

    [Fact]
    public void HttpClientName_IdentifiesApiClient()
    {
        ApiOptions.HttpClientName.Should().Be("Api");
    }
}