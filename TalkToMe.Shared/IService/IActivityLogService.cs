using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalkToMe.Shared.Models;

namespace TalkToMe.Shared.IService
{
    public interface IActivityLogService
    {
        Task<IReadOnlyList<Log>> GetActivityLogsAsync();

        Task WriteLogAsync(string user, string source, string msg);

        Task DeleteLogAsync(DateTime date);
    }
}
