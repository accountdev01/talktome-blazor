using System;
using System.Collections.Generic;

namespace TalkToMe.Shared.Models;

public partial class Configuration
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string? Description { get; set; }

    public string Type { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
