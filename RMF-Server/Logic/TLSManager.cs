using Microsoft.Extensions.Logging;
using RMF.Core.Interfaces;
using RMF.Core.Security;
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
        private readonly ILogger<TlsManager> _logger;
        private readonly TlsConfig _tlsConfig;

        private X509Certificate2? _serverCertificate;

        private string CertificateFilePath => PathResolver.GetResolvedPath(
            this._tlsConfig.CertificateFilePath,
            fileName: this._tlsConfig.CertificateFileName,
            fileFormat: "pfx"
        );

        public TlsManager(ILogger<TlsManager> logger, TlsConfig tlsConfig)
        {
            this._logger = logger;
            this._tlsConfig = tlsConfig;
        }

        public bool TryLoadCertificate()
        {
            return TryLoadCertificateInternal(this.CertificateFilePath);
        }

        private bool TryLoadCertificateInternal(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                this._serverCertificate = X509CertificateLoader.LoadPkcs12FromFile(path, this._tlsConfig.CertificatePassword);
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogCritical("Failed to load TLS certificate from path: \"{FilePath}\": {Exception}", path, ex);
                return false;
            }
        }

        public X509Certificate2 GetOrCreateCertificate()
        {
            if (this._serverCertificate != null)
            {
                return this._serverCertificate;
            }

            string path = this.CertificateFilePath;
            if (TryLoadCertificateInternal(path))
            {
                this._logger.LogInformation("TLS certificate successfully loaded from path: {FilePath}", path);
                return this._serverCertificate!;
            }

            this._logger.LogInformation($"No TLS certificate found, creating a self-signed one, trying to create...");

            using RSA rsa = RSA.Create(2048);
            CertificateRequest request = new(
                "CN=" + this._tlsConfig.CertificateName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            X509Certificate2 cert = request.CreateSelfSigned(
                DateTimeOffset.Now,
                DateTime.Now.AddDays(this._tlsConfig.CertificateDurationDays)
            );
            this._serverCertificate = cert;
            this._logger.LogInformation("TLS certificate \"{CertificateName}\" was successfully created", this._tlsConfig.CertificateName);

            byte[] certBytes = cert.Export(X509ContentType.Pfx, this._tlsConfig.CertificatePassword);
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(path, certBytes);
            this._logger.LogInformation("The new TLS certificate is saved to {FilePath}", path);
            return cert;
        }
    }
}
