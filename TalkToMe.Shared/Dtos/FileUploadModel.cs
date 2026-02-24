using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TalkToMe.Shared.Dtos
{
    public class FileUploadModel
    {
        public string FileName { get; set; } = string.Empty;
        public Stream FileStream { get; set; } = Stream.Null;
        public long Size { get; set; }
    }
}
