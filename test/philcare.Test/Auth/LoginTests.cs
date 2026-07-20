using System.Net;
using System.Net.Http.Json;
using philcare.Test.Common;
using Xunit;

namespace philcare.Test.Auth;

public class LoginTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public LoginTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "nonexistent@philcare.local",
            Password = "WrongPassword123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "",
            Password = "SomePassword123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
