using Azure.Security.KeyVault.Secrets;
using FunctionKeyConfigurator.Models;
using Microsoft.Extensions.Logging;

namespace FunctionKeyConfigurator.Services;

public class KeyVaultService(ILogger<KeyVaultService> logger, SecretClient secretClient)
{
    public async Task SetRoleKeySecretsAsync(List<RoleKey> roleKeys, string environmentSuffix)
    {
        logger.LogInformation("Starting KeyVault secret upsert for environment suffix '{Suffix}'", environmentSuffix);

        foreach (var roleKey in roleKeys)
        {
            var secretName = $"{roleKey.RoleName}-{environmentSuffix}";
            try
            {
                logger.LogInformation("Setting secret '{SecretName}' in KeyVault", secretName);
                await secretClient.SetSecretAsync(secretName, roleKey.KeyValue);
                logger.LogInformation("Successfully set secret '{SecretName}' in KeyVault", secretName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting secret '{SecretName}' in KeyVault", secretName);
            }
        }
    }
}
