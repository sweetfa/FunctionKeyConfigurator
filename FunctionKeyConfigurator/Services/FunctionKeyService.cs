using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using FunctionKeyConfigurator.Models;
using Microsoft.Extensions.Logging;

namespace FunctionKeyConfigurator.Services;

public class FunctionKeyService(ILogger<FunctionKeyService> logger, ArmClient? armClient = null)
{
    private readonly ArmClient _armClient = armClient ?? new ArmClient(new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ExcludeVisualStudioCredential = true,
        ExcludeVisualStudioCodeCredential = true
    }));

    private const int MaxRetries = 5;

    public async Task UpsertFunctionKeysAsync(Models.FunctionAppConfig config, List<RoleKey> roleKeys)
    {
        logger.LogInformation("Starting upsert of function keys for {FunctionApp}", config.FunctionAppName);

        var transactionNumber = 0;

        foreach (var roleKey in roleKeys)
        {
            var roleDef = config.Roles.FirstOrDefault(r => r.RoleName == roleKey.RoleName);
            if (roleDef == null)
            {
                logger.LogWarning("Role {Role} not found in configuration, skipping.", roleKey.RoleName);
                continue;
            }

            foreach (var functionName in roleDef.AccessibleFunctions)
            {
                transactionNumber++;

                try
                {
                    logger.LogInformation("[{TransactionNumber}] Upserting key '{KeyName}' for function '{FunctionName}'",
                        transactionNumber, roleKey.RoleName, functionName);

                    var functionResourceId = SiteFunctionResource.CreateResourceIdentifier(
                        config.SubscriptionId,
                        config.ResourceGroupName,
                        config.FunctionAppName,
                        functionName);

                    var functionResource = _armClient.GetSiteFunctionResource(functionResourceId);

                    var keyInfo = new WebAppKeyInfo
                    {
                        Properties = new WebAppKeyInfoProperties
                        {
                            Name = roleKey.RoleName,
                            Value = roleKey.KeyValue
                        }
                    };

                    await UpsertWithRetryAsync(functionResource, roleKey.RoleName, keyInfo, transactionNumber, functionName);
                    logger.LogInformation("[{TransactionNumber}] Successfully upserted key '{KeyName}' for function '{FunctionName}'",
                        transactionNumber, roleKey.RoleName, functionName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[{TransactionNumber}] Error upserting key '{KeyName}' for function '{FunctionName}'",
                        transactionNumber, roleKey.RoleName, functionName);
                }
            }
        }

        logger.LogInformation("Completed upsert of function keys. Total transactions: {Total}", transactionNumber);
    }

    private async Task UpsertWithRetryAsync(SiteFunctionResource functionResource, string keyName, WebAppKeyInfo keyInfo, int transactionNumber, string functionName)
    {
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await functionResource.CreateOrUpdateFunctionSecretAsync(keyName, keyInfo);
                return;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 429 && attempt < MaxRetries)
            {
                var delaySeconds = Math.Pow(2, attempt);
                logger.LogWarning("[{TransactionNumber}] Received 429 (Too Many Requests) for function '{FunctionName}'. Retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
                    transactionNumber, functionName, delaySeconds, attempt + 1, MaxRetries);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
}
