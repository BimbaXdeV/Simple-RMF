using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal class SecurityConfig
    {
        public string CertificateFingerprint = string.Empty;
        public int TlsHandshakeTimeoutSecs = 1;
        public int MaxPacketBufferKB = int.MaxValue;
    }
}
