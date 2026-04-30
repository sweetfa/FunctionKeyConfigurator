namespace FunctionKeyConfigurator.Models;

public record RoleDefinition(string RoleName, List<string> AccessibleFunctions);

public record FunctionAppConfig(
    string SubscriptionId,
    string ResourceGroupName,
    string FunctionAppName,
    List<RoleDefinition> Roles
);

public record RoleKey(string RoleName, string KeyValue);
