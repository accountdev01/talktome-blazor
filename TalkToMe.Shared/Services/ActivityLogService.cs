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
    public class ActivityLogService : IActivityLogService
    {
        private readonly TalkToMeContext _context;

        public ActivityLogService (TalkToMeContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Log>> GetActivityLogsAsync()
        {
            try
            {
                return await _context.Logs.AsNoTracking().OrderByDescending(l => l.Timestamp).ToListAsync();
            }
            catch (Exception ex)
            {
                LogError("GetActivityLogsAsync", ex.ToString());
                return new List<Log>();
            }
        }

        public async Task WriteLogAsync(string user, string source, string msg)
        {
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(msg) || string.IsNullOrWhiteSpace(source))
            {
                LogError("WriteLogAsync - Invalid Parameters", "One or more parameters are null or empty.");
                return;
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var newlog = new Log
                    {
                        Timestamp = DateTime.Now,
                        User = user,
                        Source = source,
                        Description = msg
                    };

                    await _context.Logs.AddAsync(newlog);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    LogError("WriteLogAsync", ex.ToString());
                    throw;
                }
            });
        }

        public async Task DeleteLogAsync(DateTime date)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    await _context.Logs.Where(l => l.Timestamp < date).ExecuteDeleteAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    LogError("DeleteLogAsync", ex.ToString());
                    throw;
                }
            });
        }

        private void LogError(string fn, string msg)
        {
            LoggerHelper.WriteLog($"ActivityLogService -> {fn}", msg);
        }
    }
}
