using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal interface IPathManager
    {
        string GetResolvedPath(
            string key,
            string? fileName = null,
            string? fileFormat = null,
            string? endPoint = null,
            bool UpdateCachedDate = false
        );
    }
}
