using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Security
{
    public interface ITlsManager
    {
        bool TryLoadCertificate();
        X509Certificate2 GetOrCreateCertificate();
    }
}
