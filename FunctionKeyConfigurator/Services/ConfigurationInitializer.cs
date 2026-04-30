using Microsoft.Extensions.Configuration;
using FunctionKeyConfigurator.Models;

namespace FunctionKeyConfigurator.Services;

public class ConfigurationInitializer
{
    private readonly IConfiguration _configuration;

    public ConfigurationInitializer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public FunctionAppConfig GetFunctionAppConfig()
    {
        var section = _configuration.GetSection("FunctionAppConfig");
        return section.Get<FunctionAppConfig>() 
               ?? throw new InvalidOperationException("FunctionAppConfig section is missing or invalid in configuration.");
    }

    public List<RoleKey> GetRoleKeys()
    {
        var section = _configuration.GetSection("RoleKeys");
        return section.Get<List<RoleKey>>() 
               ?? throw new InvalidOperationException("RoleKeys section is missing or invalid in configuration.");
    }
}
