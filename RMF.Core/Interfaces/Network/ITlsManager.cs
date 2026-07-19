using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces.Network
{
    public interface ITlsManager
    {
        bool TryLoadCertificate(string path);
        X509Certificate2 GetOrCreateCertificate();
    }
}
