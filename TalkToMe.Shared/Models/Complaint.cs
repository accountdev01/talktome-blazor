using System;
using System.Collections.Generic;

namespace TalkToMe.Shared.Models;

public partial class Complaint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int? AttachmentsCount { get; set; }

    public virtual ICollection<ComplaintAttachment> ComplaintAttachments { get; set; } = new List<ComplaintAttachment>();
}
