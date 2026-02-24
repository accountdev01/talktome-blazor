using System;
using System.Collections.Generic;

namespace TalkToMe.Shared.Models;

public partial class ComplaintAttachment
{
    public string Id { get; set; } = null!;

    public string ComplaintId { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.Now;

    public virtual Complaint Complaint { get; set; } = null!;
}
