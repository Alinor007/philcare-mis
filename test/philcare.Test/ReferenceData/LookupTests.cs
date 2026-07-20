using System.Net;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.ReferenceData;

public class LookupTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public LookupTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLookups_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/lookups");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
