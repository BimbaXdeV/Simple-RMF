using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public readonly struct EventResult
    {
        public readonly bool IsSuccess;
        public readonly string? ExceptionMessage;

        public EventResult(bool isSuccess, string? exceptionMessage = null)
        {
            IsSuccess = isSuccess;
            ExceptionMessage = exceptionMessage;
        }

        public static EventResult Success()
        {
            return new EventResult(true);
        }

        public static EventResult Failure(string exceptionMessage)
        {
            return new EventResult(false, exceptionMessage);
        }
    }
}
