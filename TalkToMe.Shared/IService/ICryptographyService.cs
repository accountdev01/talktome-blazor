using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TalkToMe.Shared.IService
{
    public interface ICryptographyService
    {
        void GenerateKeys();
        string Protect(string plainText);
        string Unprotect(string cipherText);
    }
}
