using System;
using System.Collections.Generic;

namespace TalkToMe.Shared.Models;

public partial class Log
{
    public string Id { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string? User { get; set; }

    public string Description { get; set; } = null!;

    public string Source { get; set; } = null!;
}
