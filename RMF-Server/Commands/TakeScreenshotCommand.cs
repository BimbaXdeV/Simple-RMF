using Microsoft.Extensions.Logging;
using RMF.Core.Network;
using RMF.Core.Packets.Server;
using RMF_Server.Configurations;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class TakeScreenshotCommand : InlineCommand
    {
        private readonly IServerSessionManager _sessionManager;
        private readonly StreamingConfig _streamingConfig;

        public override string Category => "Management";
        public override string Name => "screenshot";
        public override string Description => "Sends a request to the remote client to obtain a current screenshot";
        public override string[]? Parameters => ["ip:port"];

        public TakeScreenshotCommand(
            IServerSessionManager sessionManager,
            ILogger<CommandDispatcher> cmLogger,
            CommandConfig commandConfig,
            StreamingConfig streamingConfig
        ) : base(cmLogger, commandConfig)
        {
            _sessionManager = sessionManager;
            _streamingConfig = streamingConfig;
        }

        public override Task ExecuteAsync(string[] args, CancellationToken token)
        {
            if (args.Length != 1)
            {
                CmLogger.LogError("Fill in the screenshot command according to the structure shown below:\n{Syntax}", ToString());
                return Task.CompletedTask;
            }

            string targetEndPoint = args[0];
            if (_sessionManager.GetClientSession(targetEndPoint, out IServerClientSession? session))
            {
                ScreenshotRequest screenshotRequest = new()
                {
                    FormatID = (byte)_streamingConfig.ScreenshotFrameFormat,
                    QualityPercent = (byte)_streamingConfig.ScreenshotQualityPercentage
                };
                session!.SendPacket(screenshotRequest);
                CmLogger.LogInformation("Successfully sent to {EndPoint}, waiting for remote screenshot...", targetEndPoint);
            }
            else
            {
                CmLogger.LogError("No connection found named \"{EndPoint}\"", targetEndPoint);
            }
            return Task.CompletedTask;
        }
    }
}
