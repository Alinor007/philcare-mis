using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace philcare.Test.Common;

public sealed class TestWebAppFactory : WebApplicationFactory<Program>
{
    // Unique per factory instance (one per test class via IClassFixture) so parallel test
    // classes never share — and race to seed — the same EF Core InMemory database.
    private readonly string _databaseName = $"PhilCareMisTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:UseInMemory"] = "true",
                ["Persistence:InMemoryDatabaseName"] = _databaseName
            });
        });
    }
}
