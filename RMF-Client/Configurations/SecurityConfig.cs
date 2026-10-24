using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal class SecurityConfig : RmfConfig
    {
        public string CertificateFingerprint = string.Empty;
        public int TlsHandshakeTimeoutSecs = 1;
        public int MaxPacketBufferKB = int.MaxValue;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (string.IsNullOrWhiteSpace(CertificateFingerprint))
            {
                exceptionMessage = "CertificateFingerprint cannot be empty";
                return false;
            }
            if (TlsHandshakeTimeoutSecs <= 0)
            {
                exceptionMessage = "TlsHandshakeTimeoutSecs must be greater than 0";
                return false;
            }
            if (MaxPacketBufferKB <= 0)
            {
                exceptionMessage = "MaxPacketBufferKB must be greater than 0";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<string>(configDict, nameof(CertificateFingerprint), ref loadedFields, v => CertificateFingerprint = v);
            ApplyValue<int>(configDict, nameof(TlsHandshakeTimeoutSecs), ref loadedFields, v => TlsHandshakeTimeoutSecs = v);
            ApplyValue<int>(configDict, nameof(MaxPacketBufferKB), ref loadedFields, v => MaxPacketBufferKB = v);
            return loadedFields;
        }
    }
}
