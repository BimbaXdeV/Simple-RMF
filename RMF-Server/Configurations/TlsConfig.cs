using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class TlsConfig
    {
        public string? CertificateName;
        public string? CertificateFileName;
        public string? CertificatePassword;
        public int CertificateDurationDays;
    }
}
