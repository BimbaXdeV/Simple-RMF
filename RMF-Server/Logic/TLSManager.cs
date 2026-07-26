using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class TlsManager : ITlsManager
    {
        private readonly ILoggingEngine? _logger;
        private readonly TlsConfig? _tlsConfig;

        private X509Certificate2? ServerCertificate;

        public TlsManager(ILoggingEngine? logger = null, TlsConfig? tlsConfig = null)
        {
            this._logger = logger;
            this._tlsConfig = tlsConfig;
        }

        public bool TryLoadCertificate(string path)
        {
            if (this._tlsConfig?.CertificateFileName == null || this._tlsConfig?.CertificatePassword == null)
            {
                this._logger?.Error("Certificate file name or password is not set in the configuration. Unable to load TLS certificate");
                return false;
            }

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                this.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile(path, this._tlsConfig?.CertificatePassword);
                return true;
            }
            catch (Exception ex)
            {
                this._logger?.Error($"Failed to load TLS certificate from path: \"{path}\": {ex}");
                return false;
            }
        }

        public X509Certificate2 GetOrCreateCertificate()
        {
            if (this.ServerCertificate != null)
            {
                return this.ServerCertificate;
            }

            string certPath = PathManager.GetResolvedPath(
                "Certificate",
                fileName: this._tlsConfig?.CertificateFileName,
                fileFormat: "pfx"
            );

            if (TryLoadCertificate(certPath))
            {
                this._logger?.Output($"TLS certificate successfully loaded from path: \"{certPath}\"");
                return this.ServerCertificate!;
            }

            this._logger?.Output($"No TLS certificate found, creating a self-signed one, trying to create...");

            using RSA rsa = RSA.Create(2048);
            CertificateRequest request = new(
                "CN=" + this._tlsConfig?.CertificateName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            X509Certificate2 cert = request.CreateSelfSigned(
                DateTimeOffset.Now,
                DateTime.Now.AddDays(this._tlsConfig?.CertificateDurationDays ?? 365)
            );
            this.ServerCertificate = cert;
            this._logger?.Output($"TLS certificate \"{this._tlsConfig?.CertificateName}\" was successfully created");

            byte[] certBytes = cert.Export(X509ContentType.Pfx, this._tlsConfig?.CertificatePassword);
            string? directory = Path.GetDirectoryName(certPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(certPath, certBytes);
            this._logger?.Output($"The new TLS certificate is saved to {certPath}");
            return cert;

        }
    }
}
