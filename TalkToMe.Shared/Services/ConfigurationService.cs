using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalkToMe.Shared.Data;
using TalkToMe.Shared.IService;
using TalkToMe.Shared.Models;

namespace TalkToMe.Shared.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly TalkToMeContext _context;
        private readonly ICryptographyService _cryptography;

        public ConfigurationService (TalkToMeContext context, ICryptographyService cryptography)
        {
            _context = context;
            _cryptography = cryptography;
        }

        public async Task<IReadOnlyList<Configuration>> GetConfigurationAsync()
        {
            try
            {
                return await _context.Configurations.AsNoTracking().OrderBy(c => c.Key).ToListAsync();
            }
            catch (Exception ex)
            {
                LogError("GetConfigurationAsync", ex.Message);

                return new List<Configuration>();
            }
        }

        public async Task<string> FindConfigurationByKeyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                LogError("FindConfigurationByKeyAsync", "Key is null.");
                return string.Empty;
            }

            try
            {
                var config = await _context.Configurations.AsNoTracking().FirstOrDefaultAsync(c => c.Key == key && c.IsActive == true);

                if (config == null || string.IsNullOrWhiteSpace(config.Value))
                {
                    LogError("FindConfigurationByKeyAsync", $"Configuration key '{key}' not found or value is empty.");
                    return string.Empty;
                }

                string value = IsKeyRequiring(key) ? _cryptography.Unprotect(config.Value) : config.Value;

                return value;
            }
            catch (Exception ex)
            {
                LogError("FindConfigurationByKeyAsync", ex.Message);
                return string.Empty;
            }
        }

        public async Task<bool> UpdateConfigurationAsync(Configuration config)
        {
            if (config == null)
            {
                LogError("UpdateConfigurationAsync", "Configuration is null.");
                return false;
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var dbConfig = await _context.Configurations
                        .FirstOrDefaultAsync(c => c.Key == config.Key);

                    if (dbConfig == null)
                    {
                        LogError("UpdateConfigurationAsync", $"Key not found: {config.Key}");
                        return false;
                    }

                    string valueToSave = IsKeyRequiring(config.Key) ? _cryptography.Protect(config.Value) : config.Value;

                    dbConfig.Value = valueToSave;
                    dbConfig.Description = config.Description;
                    dbConfig.IsActive = config.IsActive;
                    dbConfig.Type = config.Type;
                    dbConfig.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    LogError("UpdateConfigurationAsync", ex.ToString());
                    throw;
                }
            });
        }

        private bool IsKeyRequiring(string key)
        {
            string[] secretKeywords = { "User", "Password", "Pwd" };

            foreach (var keyword in secretKeywords)
            {
                if (key.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void LogError(string fn, string msg)
        {
            LoggerHelper.WriteLog($"Configuration -> {fn}", msg);
        }
    }
}
