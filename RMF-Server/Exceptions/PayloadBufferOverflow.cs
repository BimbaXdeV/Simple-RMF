using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Exceptions
{
    internal class PayloadBufferOverflow : Exception
    {
        public PayloadBufferOverflow(string message) : base(message) { }
    }
}
