using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TalkToMe.Shared.Data;
using TalkToMe.Shared.Dtos;
using TalkToMe.Shared.IService;
using TalkToMe.Shared.Models;

namespace TalkToMe.Shared.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly TalkToMeContext _context;
        private readonly string _uploadPath;

        public ComplaintService (TalkToMeContext context, IConfiguration config)
        {
            _context = context;

            var folderName = config["StorageSettings:UploadFolder"] ?? "uploads";
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName);
        }

        public async Task<List<Complaint>> GetComplaintsAsync(int count = 100)
        {
            try
            {
                return await _context.Complaints.AsNoTracking().OrderByDescending(c => c.CreatedAt).Take(count).ToListAsync();
            }
            catch (Exception ex)
            {
                LogError("GetComplaintsAsync", $"Critical error fetching complaints: {ex.Message}");
                throw;
            }
        }

        public async Task<Complaint?> FindComplaintByIdAsync(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                LogError("FindComplaintByIdAsync", "ID is null.");
                return null;
            }

            try
            {
                var complaints = await _context.Complaints.Include(c => c.ComplaintAttachments).AsNoTracking().FirstOrDefaultAsync(c => c.Id == Id);

                return complaints;
            }
            catch (Exception ex)
            {
                LogError("FindComplaintByIdAsync", $"Error finding complaint {Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CreateComplaintAsync(Complaint complaint, List<FileUploadModel> attachments)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                var writtenFiles = new List<string>();

                try
                {
                    if (string.IsNullOrEmpty(complaint.Id))
                        complaint.Id = Guid.NewGuid().ToString();

                    if (!Directory.Exists(_uploadPath))
                        Directory.CreateDirectory(_uploadPath);

                    if (attachments?.Any() == true)
                    {
                        foreach (var file in attachments)
                        {
                            if (file.FileStream == null) continue;

                            var trustedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            var fullPath = Path.Combine(_uploadPath, trustedFileName);

                            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                            {
                                if (file.FileStream.CanSeek) file.FileStream.Position = 0;
                                await file.FileStream.CopyToAsync(fs);
                            }

                            writtenFiles.Add(fullPath);

                            complaint.ComplaintAttachments.Add(new ComplaintAttachment
                            {
                                Id = Guid.NewGuid().ToString(),
                                FileName = file.FileName,
                                FilePath = trustedFileName
                            });
                        }
                    }

                    _context.Complaints.Add(complaint);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    foreach (var filePath in writtenFiles)
                    {
                        try { if (File.Exists(filePath)) File.Delete(filePath); }
                        catch { /* Log warning: could not clean up file */ }
                    }

                    LogError("CreateComplaintAsync", $"Transaction failed: {ex.Message}");
                    throw;
                }
            });
        }

        private void LogError(string fn, string msg) => LoggerHelper.WriteLog($"ComplaintService -> {fn}", msg);
    }
}
