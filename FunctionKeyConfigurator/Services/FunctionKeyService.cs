using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using FunctionKeyConfigurator.Models;
using Microsoft.Extensions.Logging;

namespace FunctionKeyConfigurator.Services;

public class FunctionKeyService
{
    private readonly ArmClient _armClient;
    private readonly ILogger<FunctionKeyService> _logger;

    public FunctionKeyService(ILogger<FunctionKeyService> logger, ArmClient? armClient = null)
    {
        _logger = logger;
        _armClient = armClient ?? new ArmClient(new DefaultAzureCredential());
    }

    public async Task UpsertFunctionKeysAsync(Models.FunctionAppConfig config, List<RoleKey> roleKeys)
    {
        _logger.LogInformation("Starting upsert of function keys for {FunctionApp}", config.FunctionAppName);

        var websiteResourceId = WebSiteResource.CreateResourceIdentifier(
            config.SubscriptionId,
            config.ResourceGroupName,
            config.FunctionAppName);

        var websiteResource = _armClient.GetWebSiteResource(websiteResourceId);

        foreach (var roleKey in roleKeys)
        {
            var roleDef = config.Roles.FirstOrDefault(r => r.RoleName == roleKey.RoleName);
            if (roleDef == null)
            {
                _logger.LogWarning("Role {Role} not found in configuration, skipping.", roleKey.RoleName);
                continue;
            }

            foreach (var functionName in roleDef.AccessibleFunctions)
            {
                try
                {
                    _logger.LogInformation("Upserting key '{KeyName}' for function '{FunctionName}'", roleKey.RoleName, functionName);
                    
                    var functionResourceId = SiteFunctionResource.CreateResourceIdentifier(
                        config.SubscriptionId,
                        config.ResourceGroupName,
                        config.FunctionAppName,
                        functionName);

                    var functionResource = _armClient.GetSiteFunctionResource(functionResourceId);

                    var keyInfo = new WebAppKeyInfo
                    {
                        Name = roleKey.RoleName,
                        Value = roleKey.KeyValue
                    };

                    await functionResource.CreateOrUpdateFunctionSecretAsync(roleKey.RoleName, keyInfo);
                    _logger.LogInformation("Successfully upserted key '{KeyName}' for function '{FunctionName}'", roleKey.RoleName, functionName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error upserting key '{KeyName}' for function '{FunctionName}'", roleKey.RoleName, functionName);
                }
            }
        }
    }
}
