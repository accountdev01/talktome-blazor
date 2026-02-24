using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalkToMe.Shared.CustomEntites;

namespace TalkToMe.Shared.IService
{
    public interface IAuthADService
    {
        Task<ADUserInfo> LoginADAsync(string username, string password);
    }
}
