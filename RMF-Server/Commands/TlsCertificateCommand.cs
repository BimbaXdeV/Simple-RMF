using Microsoft.Extensions.Logging;
using RMF.Core.Security;
using RMF_Server.Configurations;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class TlsCertificateCommand : InlineCommand
    {
        private readonly ITlsManager _tlsManager;

        public override string Category => "Performance";
        public override string Name => "tls";
        public override string Description => "Displays information about the current TLS certificate";
        public override string[]? Parameters => null;

        public TlsCertificateCommand(
            ITlsManager tlsManager,
            ILogger<CommandDispatcher> cmLogger,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _tlsManager = tlsManager;
        }

        public override Task ExecuteAsync(string[] args, CancellationToken token)
        {
            X509Certificate2 certificate = _tlsManager.GetOrCreateCertificate();
            CmLogger.LogInformation("Server TLS Certificate:");
            CmLogger.LogInformation("- Subject    : {Subject}", certificate.Subject);
            CmLogger.LogInformation("- Issuer     : {Issuer}", certificate.Issuer);
            CmLogger.LogInformation("- Signature  : {Algorithm}", certificate.SignatureAlgorithm.FriendlyName);
            CmLogger.LogInformation("- Version    : v{Version}", certificate.Version);
            CmLogger.LogInformation("- Expiration : {StartTime} - {ExpirationTime}",
                certificate.NotBefore.ToString(RmfConstants.DateTimeFormatYmdHms), certificate.NotAfter.ToString(RmfConstants.DateTimeFormatYmdHms)
            );
            CmLogger.LogInformation("- Fingerprint: {Fingerprint} ", certificate.Thumbprint);
            return Task.CompletedTask;
        }
    }
}
