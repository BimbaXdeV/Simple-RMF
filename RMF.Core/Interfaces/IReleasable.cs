using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces
{
    public interface IReleasable
    {
        // Not to be confused with "IDisposable"!
        // This is for objects that are pooled and need to be released back to the pool, not for unmanaged resources.
        void Release();
    }
}
