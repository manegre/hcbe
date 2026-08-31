using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HcbeApi.Data;

namespace HcbeApi.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public CustomWebApplicationFactory()
    {
        // Program validates the signing key before the test host callback runs.
        Environment.SetEnvironmentVariable(
            "JwtSettings__Secret",
            "TestSecretKeyThatIsAtLeast32CharactersLongForJWTTokenGeneration!");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureAppConfiguration(config =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" },
                { "JwtSettings:Secret", "TestSecretKeyThatIsAtLeast32CharactersLongForJWTTokenGeneration!" },
                { "JwtSettings:Issuer", "HcbeApi" },
                { "JwtSettings:Audience", "HcbeApi" },
                { "JwtSettings:ExpirationInMinutes", "1440" },
                { "Cors:AllowedOrigins:0", "http://localhost:5173" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                // Ensure the database is created
                db.Database.EnsureCreated();
            }
        });

        builder.UseEnvironment("Testing");
    }
}

