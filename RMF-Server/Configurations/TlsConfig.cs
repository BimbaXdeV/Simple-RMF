using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class TlsConfig : RmfConfig
    {
        private const int _minPasswordLength = 8;
        private const int _maxPasswordLength = 64;

        public string CertificateFilePath = string.Empty;
        public string CertificateName = "TLS-Certifitate";
        public string CertificateFileName = "tls-certificate";
        public string CertificatePassword = "19b3g5fQ7";  // This is JUST a placeholder. Under no circumstances should you use it in production
        public int CertificateDurationDays = 1;           // Seriously, open the "Storage/config.json" file and set a unique password :)

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (string.IsNullOrWhiteSpace(CertificateFilePath))
            {
                exceptionMessage = "CertificateFilePath cannot be empty";
                return false;
            }
            if (string.IsNullOrWhiteSpace(CertificateName))
            {
                exceptionMessage = "CertificateName cannot be empty";
                return false;
            }
            if (string.IsNullOrWhiteSpace(CertificateFileName))
            {
                exceptionMessage = "CertificateFileName cannot be empty";
                return false;
            }
            if (string.IsNullOrWhiteSpace(CertificatePassword))
            {
                exceptionMessage = "CertificatePassword cannot be empty";
                return false;
            }
            if (CertificatePassword.Length < _minPasswordLength || CertificatePassword.Length > _maxPasswordLength)
            {
                exceptionMessage = $"CertificatePassword must be between {_minPasswordLength} and {_maxPasswordLength} characters long";
                return false;
            }
            if (!CertificatePassword.Any(char.IsUpper) || !CertificatePassword.Any(char.IsLower) || !CertificatePassword.Any(char.IsDigit))
            {
                exceptionMessage = "CertificatePassword must contain at least one uppercase letter, one lowercase letter, and one digit";
                return false;
            }
            if (CertificateDurationDays <= 0)
            {
                exceptionMessage = "CertificateDurationDays must be greater than 0";
                return false;
            }
            return true;
        }

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<string>(configDict, nameof(CertificateFilePath), ref loadedFields, v => CertificateFilePath = v);
            ApplyValue<string>(configDict, nameof(CertificateName), ref loadedFields, v => CertificateName = v);
            ApplyValue<string>(configDict, nameof(CertificateFileName), ref loadedFields, v => CertificateFileName = v);
            ApplyValue<string>(configDict, nameof(CertificatePassword), ref loadedFields, v => CertificatePassword = v);
            ApplyValue<int>(configDict, nameof(CertificateDurationDays), ref loadedFields, v => CertificateDurationDays = v);
            return loadedFields;
        }
    }
}
