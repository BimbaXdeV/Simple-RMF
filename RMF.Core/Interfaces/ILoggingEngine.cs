using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces
{
    public interface ILoggingEngine
    {
        void CreateHistory(int bufferLength);
        Task RunExecutor(CancellationToken token);
        
        void Output(string message, bool toHistory = true);
        void Warning(string message, bool toHistory = true);
        void Error(string message, bool toHistory = true);
        void Separator();
        
        void SaveBackup(string path, bool appendBelow = false);
    }
}
