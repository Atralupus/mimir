using Hangfire;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mimir.HangfireWorker;
using Mimir.HangfireWorker.Jobs;
using Mimir.HangfireWorker.Options;
using Mimir.HangfireWorker.Services;
using Mimir.Worker;
using Mimir.Worker.Client;
using Mimir.Worker.Services;
using Serilog;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

string configPath =
    Environment.GetEnvironmentVariable("HANGFIRE_CONFIG_FILE") ?? "appsettings.json";
builder
    .Configuration.AddJsonFile(configPath, optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("HANGFIRE_");

builder.Services.Configure<Configuration>(builder.Configuration.GetSection("Configuration"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration);

Log.Logger = loggerConfiguration.CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

var config = builder.Configuration.GetSection("Configuration").Get<Configuration>();
var redisOptions = builder.Configuration.GetSection("Redis").Get<RedisOptions>();
if (config == null || redisOptions == null)
{
    throw new InvalidOperationException("Configuration and Redis options are required");
}

builder.Services.AddSingleton(config);
builder.Services.AddSingleton(redisOptions);

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var redisConfig = new ConfigurationOptions { DefaultDatabase = redisOptions.HangfireDbNumber };

    redisConfig.EndPoints.Add(redisOptions.Host, int.Parse(redisOptions.Port));

    if (!string.IsNullOrEmpty(redisOptions.Username))
    {
        redisConfig.User = redisOptions.Username;
    }

    if (!string.IsNullOrEmpty(redisOptions.Password))
    {
        redisConfig.Password = redisOptions.Password;
    }

    return ConnectionMultiplexer.Connect(redisConfig);
});

builder.Services.AddSingleton<INotFoundCacheService, RedisNotFoundCacheService>();

builder.Services.AddSingleton<IHeadlessGQLClient, HeadlessGQLClient>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IOptions<Configuration>>().Value;
    return new HeadlessGQLClient(config.HeadlessEndpoints, config.JwtIssuer, config.JwtSecretKey);
});
builder.Services.AddSingleton<IStateService, HeadlessStateService>();

builder.Services.AddSingleton(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IOptions<Configuration>>().Value;
    return new MongoDbService(
        config.MongoDbConnectionString,
        config.PlanetType,
        config.MongoDbCAFile
    );
});

builder.Services.AddSingleton<DataCompletionJobs>();

builder.Services.AddHangfire(
    (provider, hangfireConfig) =>
    {
        var redisConfig = new ConfigurationOptions
        {
            DefaultDatabase = redisOptions.HangfireDbNumber,
        };

        redisConfig.EndPoints.Add(redisOptions.Host, int.Parse(redisOptions.Port));

        if (!string.IsNullOrEmpty(redisOptions.Username))
        {
            redisConfig.User = redisOptions.Username;
        }

        if (!string.IsNullOrEmpty(redisOptions.Password))
        {
            redisConfig.Password = redisOptions.Password;
        }

        hangfireConfig.UseRedisStorage(
            ConnectionMultiplexer.Connect(redisConfig),
            new Hangfire.Redis.StackExchange.RedisStorageOptions
            {
                Prefix = redisOptions.HangfirePrefix,
                Db = redisOptions.HangfireDbNumber,
            }
        );
    }
);

builder.Services.AddHangfireServer();

var host = builder.Build();

Log.Information("Starting Mimir Hangfire Worker with workers");

host.Run();
