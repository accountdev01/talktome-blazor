using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalkToMe.Shared.CustomEntites;
using TalkToMe.Shared.IService;

namespace TalkToMe.Shared.Services
{
    public class AuthADService : IAuthADService
    {
        private readonly IConfigurationService _config;
        private readonly IActivityLogService _log;

        public AuthADService(IConfigurationService config, IActivityLogService log) 
        { 
            _config = config;
            _log = log;
        }

        public async Task<ADUserInfo> LoginADAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                LogError("LoginADAsync", "Username or password is null.");
                return new();
            }

            try
            {
                string domain = await _config.FindConfigurationByKeyAsync("Domain");
                string name = await _config.FindConfigurationByKeyAsync("FullnameAD");
                string empId = await _config.FindConfigurationByKeyAsync("EmpIdAD");
                string port = await _config.FindConfigurationByKeyAsync("PortAD");
                string allowedGroups = await _config.FindConfigurationByKeyAsync("AllowedGroups");

                var allowedGroupList = allowedGroups.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(g => g.Trim()).ToList() ?? new List<string>();

                using var ldap = new LdapConnection();

                ldap.Constraints.TimeLimit = 5000;

                await ldap.ConnectAsync(domain, int.Parse(port));
                await ldap.BindAsync(LdapConnection.LdapV3, $"{username}@{domain}", password);

                if (!ldap.Bound) return new();

                var baseDn = BuildBaseDn(domain);
                var entry = await FindUserEntryAsync(ldap, baseDn, username, name, empId);

                if (entry == null)
                {
                    LogError("LoginADAsync", $"User {username} not found in AD");
                    return new();
                }

                if (allowedGroupList == null || !allowedGroupList.Any())
                {
                    LogError("LoginADAsync", $"AllowedGroups is null or empty. Value from configService might be missing or invalid.");
                    return new();
                }

                string matchedGroup = FindMatchedGroup(entry.Get("memberOf"), allowedGroupList);
                if (string.IsNullOrWhiteSpace(matchedGroup))
                {
                    LogError("LoginADAsync", "User does not belong to allowed groups.");
                    return new();
                }

                var emp = entry.Get(empId)?.StringValue ?? string.Empty;
                var fullname = entry.Get(name)?.StringValue ?? string.Empty;

                var user = new ADUserInfo()
                {
                    EmpId = empId,
                    Name = fullname,
                    Department = matchedGroup
                };

                return user;
            }
            catch (LdapException ex)
            {
                LogError("LoginADAsync", $"LdapException: {ex.Message}");
                return new();
            }
            catch (Exception ex)
            {
                LogError("LoginADAsync", $"Exception: {ex.Message}");
                return new();
            }

        }

        private string BuildBaseDn(string domain) => "DC=" + string.Join(",DC=", domain.Split('.'));

        private async Task<LdapEntry?> FindUserEntryAsync(LdapConnection ldap, string baseDn, string username, string name, string empId)
        {
            try
            {
                string filter = $"(sAMAccountName={username})";

                var result = await ldap.SearchAsync(baseDn, LdapConnection.ScopeSub, filter, new[] {"memberOf", name, empId}, false);

                return await result.HasMoreAsync() ? await result.NextAsync() : null;
            }
            catch (Exception ex)
            {
                LogError("FindUserEntryAsync", ex.Message);
                return null;
            }
        }

        private string FindMatchedGroup(LdapAttribute memberOfArr, List<string> allowedGrops)
        {
            if (memberOfArr == null) return string.Empty;

            try
            {
                foreach (var groupDn in memberOfArr.StringValueArray)
                {
                    var groupName = ExtractCN(groupDn);
                    if (!string.IsNullOrWhiteSpace(groupName) &&
                        allowedGrops.Any(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase)))
                        return groupName;
                }
            }
            catch (Exception ex)
            {
                LogError("FindMatchedGroup", ex.Message);
            }

            return string.Empty;
        }

        private string ExtractCN(string distinguishedName)
        {
            if (distinguishedName == null) return string.Empty;

            try
            {
                foreach (var part in distinguishedName.Split(','))
                {
                    string trimed = part.Trim();
                    if (trimed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                        return trimed.Substring(3);
                }
            }
            catch (Exception ex)
            {
                LogError("ExtractCN", ex.Message);
            }

            return string.Empty;
        }

        private void LogError(string fn, string msg)
        {
            LoggerHelper.WriteLog($"AuthADService -> {fn}", msg);
        }
    }
}
