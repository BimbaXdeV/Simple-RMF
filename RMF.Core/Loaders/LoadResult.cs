using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Loaders
{
    public readonly struct LoadResult<T>
    {
        public readonly bool IsSuccess;
        public readonly T? Data;
        public readonly int Loaded;
        public readonly int Total;
        public readonly string? ExceptionMessage;

        public LoadResult(bool isSuccess, T? data, int loaded, int total, string? exceptionMessage)
        {
            this.IsSuccess = isSuccess;
            this.Data = data;
            this.Loaded = loaded;
            this.Total = total;
            this.ExceptionMessage = exceptionMessage;
        }

        public static LoadResult<T> Success(T data, int loaded, int total)
        {
            return new LoadResult<T>(true, data, loaded, total, null);
        }

        public static LoadResult<T> Failure(string exceptionMessage)
        {
            return new LoadResult<T>(false, default, 0, 0, exceptionMessage);
        }
    }
}
