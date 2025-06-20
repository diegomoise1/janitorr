using System;
using System.Threading.Tasks;
using JanitorAspNet.Configuration;

namespace JanitorAspNet.Services
{
    public interface IConfigurationService
    {
        Task<bool> UpdateConfigurationAsync(ApplicationOptions newConfig);
        Task<ApplicationOptions> GetMaskedConfigurationAsync();
        event EventHandler<ApplicationOptions> ConfigurationChanged;
    }
}
