using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public readonly struct EventStartupResult
    {
        public readonly bool IsSuccess;
        public readonly string? ExceptionMessage;

        public EventStartupResult(bool isSuccess, string? exceptionMessage = null)
        {
            IsSuccess = isSuccess;
            ExceptionMessage = exceptionMessage;
        }

        public static EventStartupResult Success()
        {
            return new EventStartupResult(true);
        }

        public static EventStartupResult Failure(string exceptionMessage)
        {
            return new EventStartupResult(false, exceptionMessage);
        }
    }
}
