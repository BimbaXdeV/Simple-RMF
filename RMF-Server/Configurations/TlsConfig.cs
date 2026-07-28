using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class TlsConfig
    {
        public string CertificateName = "TLS-Certifitate";
        public string CertificateFileName = "tls-certificate";
        public string CertificatePassword = "19b3g5fQ7";  // This is JUST a placeholder. Under no circumstances should you use it in production
        public int CertificateDurationDays = 1;           // Seriously, open the "Storage/config.json" file and set a unique password :)
    }
}
