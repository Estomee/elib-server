// Partial DbContext extension providing a static factory method for standalone context creation.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Server.Data;

public partial class ElibDbContext
{
    private static readonly Lazy<DbContextOptions<ElibDbContext>> _options =
        new Lazy<DbContextOptions<ElibDbContext>>(() =>
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<ElibDbContext>()
                .AddEnvironmentVariables()
                .Build();

            var builder = new DbContextOptionsBuilder<ElibDbContext>();
            builder.UseNpgsql(configuration.GetConnectionString("Postgres"));
            return builder.Options;
        });

    public static ElibDbContext Create() => new ElibDbContext(_options.Value);
}
