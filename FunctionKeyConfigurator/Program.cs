using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FunctionKeyConfigurator.Services;
using Azure.ResourceManager;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace FunctionKeyConfigurator;

class Program
{
    static async Task Main(string[] args)
    {
        // 0. Parse Command Line Arguments
        string configFilePath = args.Length > 0 ? args[0] : "config.json";

        // If the path is relative, try to find it in the current directory or the base directory
        if (!Path.IsPathRooted(configFilePath))
        {
            if (!File.Exists(configFilePath))
            {
                var baseDirFile = Path.Combine(AppContext.BaseDirectory, configFilePath);
                if (File.Exists(baseDirFile))
                {
                    configFilePath = baseDirFile;
                }
            }
            else
            {
                configFilePath = Path.GetFullPath(configFilePath);
            }
        }

        // 1. Setup Configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile(configFilePath, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // 2. Setup Dependency Injection
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .AddSingleton<IConfiguration>(sp => configuration)
            .AddSingleton<ConfigurationInitializer>()
            .AddSingleton<ArmClient>(sp =>
            {
                var options = new DefaultAzureCredentialOptions
                {
                    ExcludeVisualStudioCredential = true,
                    ExcludeVisualStudioCodeCredential = true
                };
                return new ArmClient(new DefaultAzureCredential(options));
            })
            .AddSingleton<FunctionKeyService>()
            .AddSingleton<KeyVaultService>()
            .BuildServiceProvider();

        // 3. Resolve and Run
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var configInitializer = serviceProvider.GetRequiredService<ConfigurationInitializer>();
        var functionKeyService = serviceProvider.GetRequiredService<FunctionKeyService>();

        try
        {
            logger.LogInformation("Starting Function Key Upsert process using config: {ConfigFile}", configFilePath);

            var appConfig = configInitializer.GetFunctionAppConfig();
            var roleKeys = configInitializer.GetRoleKeys();

            await functionKeyService.UpsertFunctionKeysAsync(appConfig, roleKeys);

            if (!string.IsNullOrEmpty(appConfig.KeyVaultUrl) && !string.IsNullOrEmpty(appConfig.EnvironmentSuffix))
            {
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeVisualStudioCredential = true,
                    ExcludeVisualStudioCodeCredential = true
                });
                var secretClient = new SecretClient(new Uri(appConfig.KeyVaultUrl), credential);
                var keyVaultService = new KeyVaultService(
                    serviceProvider.GetRequiredService<ILogger<KeyVaultService>>(),
                    secretClient);
                await keyVaultService.SetRoleKeySecretsAsync(roleKeys, appConfig.EnvironmentSuffix);
                logger.LogInformation("KeyVault secrets upsert completed successfully.");
            }
            else
            {
                logger.LogWarning("KeyVaultUrl or EnvironmentSuffix not configured, skipping KeyVault secret upsert.");
            }

            logger.LogInformation("Function Key Upsert process completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An unhandled exception occurred during the execution.");
            Environment.Exit(1);
        }
    }
}
