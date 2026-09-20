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
    internal sealed class StreamRequestCommand : InlineCommand
    {
        private readonly IAvaloniaManager _avaloniaManager;
        private readonly IServerSessionManager _sessionManager;
        private readonly AppearanceConfig _appearanceConfig;
        private readonly StreamingConfig _streamingConfig;

        public override string Category => "Management";
        public override string Name => "stream";
        public override string Description => "Sends a request to a remote client to start active streaming";
        public override string[]? Parameters => ["ip:port"];

        public StreamRequestCommand(
            IAvaloniaManager avaloniaManager,
            IServerSessionManager sessionManager,
            ILogger<CommandDispatcher> cmLogger,
            AppearanceConfig appearanceConfig,
            CommandConfig commandConfig,
            StreamingConfig streamingConfig
        ) : base(cmLogger, commandConfig)
        {
            _avaloniaManager = avaloniaManager;
            _sessionManager = sessionManager;
            _appearanceConfig = appearanceConfig;
            _streamingConfig = streamingConfig;
        }

        public override async Task ExecuteAsync(string[] args, CancellationToken token)
        {
            if (args.Length != 1)
            {
                CmLogger.LogError("Fill in the stream command according to the structure shown below:\n{Syntax}", ToString());
                return;
            }

            string targetEndPoint = args[0];
            if (_sessionManager.GetClientSession(targetEndPoint, out IServerClientSession? session))
            {
                StreamingRequest streamingRequest = new()
                {
                    IsActive = true,
                    FormatID = (byte)_streamingConfig.StreamingFrameFormat,
                    Quality = (byte)_streamingConfig.StreamingQualityPercentage,
                    FrameUpdateRate = _streamingConfig.StreamingFrameUpdateRate,
                    TargetFPS = (short)_streamingConfig.StreamingTargetFPS
                };
                session!.SendPacket(streamingRequest);

                _avaloniaManager.StreamingClientEndPoint = session.RemoteEndPoint;
                await _avaloniaManager.ShowWindow();
                _avaloniaManager.SetWindowTitle(_appearanceConfig.WindowTitle + " | " + targetEndPoint);
                CmLogger.LogInformation("Streaming session started with {EndPoint}", session.RemoteEndPoint);
            }
            else
            {
                CmLogger.LogError("No connection found named \"{EndPoint}\"", targetEndPoint);
            }
            return;
        }
    }
}
