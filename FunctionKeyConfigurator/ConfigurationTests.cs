using Microsoft.Extensions.Configuration;
using FunctionKeyConfigurator.Services;

namespace FunctionKeyConfigurator;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void TestConfigurationLoading()
    {
        // Arrange
        var initialData = new Dictionary<string, string?>
        {
            {"FunctionAppConfig:SubscriptionId", "sub123"},
            {"FunctionAppConfig:ResourceGroupName", "rg123"},
            {"FunctionAppConfig:FunctionAppName", "app123"},
            {"FunctionAppConfig:Roles:0:RoleName", "Admin"},
            {"FunctionAppConfig:Roles:0:AccessibleFunctions:0", "Func1"},
            {"RoleKeys:0:RoleName", "Admin"},
            {"RoleKeys:0:KeyValue", "key123"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();

        var initializer = new ConfigurationInitializer(configuration);

        // Act
        var appConfig = initializer.GetFunctionAppConfig();
        var roleKeys = initializer.GetRoleKeys();

        // Assert
        Assert.AreEqual("sub123", appConfig.SubscriptionId);
        Assert.AreEqual("Admin", appConfig.Roles[0].RoleName);
        Assert.AreEqual("Func1", appConfig.Roles[0].AccessibleFunctions[0]);
        Assert.AreEqual("Admin", roleKeys[0].RoleName);
        Assert.AreEqual("key123", roleKeys[0].KeyValue);
    }
}
