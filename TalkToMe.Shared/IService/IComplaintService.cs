using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalkToMe.Shared.Dtos;
using TalkToMe.Shared.Models;

namespace TalkToMe.Shared.IService
{
    public interface IComplaintService
    {
        Task<List<Complaint>> GetComplaintsAsync(int count = 100);
        Task<Complaint?> FindComplaintByIdAsync(string Id);
        Task<bool> CreateComplaintAsync(Complaint complaint, List<FileUploadModel> attachments);
    }
}
