using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalkToMe.Shared.Models;

namespace TalkToMe.Shared.IService
{
    public interface IConfigurationService
    {
        Task<IReadOnlyList<Configuration>> GetConfigurationAsync();

        Task<string> FindConfigurationByKeyAsync(string key);

        Task<bool> UpdateConfigurationAsync(Configuration config);
    }
}
