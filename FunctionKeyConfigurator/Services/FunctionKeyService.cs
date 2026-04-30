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

    public async Task UpsertFunctionKeysAsync(Models.FunctionAppConfig config, List<RoleKey> roleKeys)
    {
        logger.LogInformation("Starting upsert of function keys for {FunctionApp}", config.FunctionAppName);

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
                try
                {
                    logger.LogInformation("Upserting key '{KeyName}' for function '{FunctionName}'", roleKey.RoleName, functionName);
                    
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

                    await functionResource.CreateOrUpdateFunctionSecretAsync(roleKey.RoleName, keyInfo);
                    logger.LogInformation("Successfully upserted key '{KeyName}' for function '{FunctionName}'", roleKey.RoleName, functionName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error upserting key '{KeyName}' for function '{FunctionName}'", roleKey.RoleName, functionName);
                }
            }
        }
    }
}
