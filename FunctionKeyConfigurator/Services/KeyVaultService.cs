using System.Text.RegularExpressions;
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
            var typeValue = SplitCamelCase(roleKey.RoleName) + " Role Function Access Key";
            try
            {
                logger.LogInformation("Setting secret '{SecretName}' in KeyVault", secretName);
                var secret = new KeyVaultSecret(secretName, roleKey.KeyValue);
                secret.Properties.ContentType = typeValue;
                secret.Properties.Tags["Type"] = typeValue;
                await secretClient.SetSecretAsync(secret);
                logger.LogInformation("Successfully set secret '{SecretName}' in KeyVault with Type '{Type}'", secretName, typeValue);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting secret '{SecretName}' in KeyVault", secretName);
            }
        }
    }

    private static string SplitCamelCase(string input)
    {
        return Regex.Replace(input, "(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
    }
}
