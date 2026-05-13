// Configures and builds the ASP.NET Core WebApplication with all services, middleware, and GraphQL.
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using HotChocolate.Types.Descriptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.GraphQL;
using Server.GraphQL.Conventions;
using Server.GraphQL.Mutations;
using Server.GraphQL.Querries;
using Server.GraphQL.Types;
using Server.Services;

namespace Server.Configuration
{
    public static class Config
    {
        public static WebApplication Configure(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContextFactory<ElibDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

            builder.Services.AddDbContext<ElibDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

            var yandexSection = builder.Configuration.GetSection("ConnectionStrings:YandexCloud");
            builder.Services.Configure<YandexStorageOptions>(yandexSection);

            var s3KeyId = yandexSection["KeyIdentifier"];
            var s3Secret = yandexSection["SecretKey"];
            if (string.IsNullOrEmpty(s3KeyId))
                throw new InvalidOperationException("YandexCloud:KeyIdentifier is not configured.");
            if (string.IsNullOrEmpty(s3Secret))
                throw new InvalidOperationException("YandexCloud:SecretKey is not configured.");

            var credentials = new BasicAWSCredentials(s3KeyId, s3Secret);
            var s3Config = new AmazonS3Config
            {
                ServiceURL           = yandexSection["Endpoint"],
                ForcePathStyle       = true,
                AuthenticationRegion = yandexSection["Region"]
            };
            builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));
            builder.Services.AddScoped<IStorageService, YandexStorageService>();

            var sendSaySection = builder.Configuration.GetSection("ConnectionStrings:SendSay");
            builder.Services.Configure<SendSayOptions>(sendSaySection);
            builder.Services.AddHttpClient<IEmailService, SendSayService>();

            var securitySection = builder.Configuration.GetSection("ConnectionStrings:Security");
            builder.Services.Configure<SecurityOptions>(securitySection);
            builder.Services.AddScoped<ISecurityService, SecurityService>();
            builder.Services.AddSingleton<ICryptoService, CryptoService>();
            builder.Services.AddScoped<ILogService, LogService>();

            var jwtSection = builder.Configuration.GetSection("ConnectionStrings:Jwt");
            builder.Services.Configure<JwtOptions>(jwtSection);
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
                {
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey  = true,
                        ValidIssuer              = jwtSection["Issuer"],
                        ValidAudience            = jwtSection["Audience"],
                        IssuerSigningKey         = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSection["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is not set.")))
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddCors(options =>
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

            var graphql = builder.Services.AddGraphQLServer();

            graphql
                .AddConvention<INamingConventions>(new SnakeCaseNamingConvention())
                .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddTypeExtension<BookQueryResolver>()
                .AddTypeExtension<UserQueryResolver>()
                .AddTypeExtension<SystemQueryResolver>()
                .AddTypeExtension<CatalogQueryResolver>()
                .AddTypeExtension<AuthMutationResolver>()
                .AddTypeExtension<BookMutationResolver>()
                .AddTypeExtension<StorageMutationResolver>()
                .AddTypeExtension<UserLibraryMutationResolver>()
                .AddTypeExtension<UserMutationResolver>()
                .AddTypeExtension<ReferenceMutationResolver>()
                .AddType<BookObjectType>()
                .AddTypeExtension<BookTypeExtensions>()
                .AddProjections()
                .AddFiltering()
                .AddSorting()
                .AddAuthorization();

            var app = builder.Build();

            try
            {
                using var scope = app.Services.CreateScope();
                var db     = scope.ServiceProvider.GetRequiredService<ElibDbContext>();
                var crypto = scope.ServiceProvider.GetRequiredService<ICryptoService>();
                var logs   = db.SystemLogs.Where(l => l.IpAddress != null).ToList();
                bool changed = false;
                foreach (var log in logs)
                {
                    if (log.IpAddress != null && crypto.IsPlainText(log.IpAddress))
                    {
                        log.IpAddress = crypto.Encrypt(log.IpAddress);
                        changed = true;
                    }
                }
                if (changed) db.SaveChanges();
            }
            catch (Exception ex)
            {
                _ = ex;
            }

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGraphQL();

            app.MapGet("/health", async (
                IDbContextFactory<ElibDbContext> dbFactory,
                IOptions<YandexStorageOptions> storageOpts,
                IOptions<SendSayOptions> emailOpts) =>
            {
                bool dbOk = false;
                try
                {
                    await using var db = await dbFactory.CreateDbContextAsync();
                    dbOk = await db.Database.CanConnectAsync();
                }
                catch { }

                bool storageOk = !string.IsNullOrEmpty(storageOpts.Value.KeyIdentifier)
                              && !string.IsNullOrEmpty(storageOpts.Value.SecretKey);

                bool emailOk = !string.IsNullOrEmpty(emailOpts.Value.ApiKey)
                            && !string.IsNullOrEmpty(emailOpts.Value.ApiUrl);

                return Results.Json(new
                {
                    database         = dbOk,
                    storage          = storageOk,
                    email            = emailOk,
                    storage_base_url = storageOpts.Value.Buckets.PublicBaseUrl
                });
            });

            return app;
        }
    }
}
