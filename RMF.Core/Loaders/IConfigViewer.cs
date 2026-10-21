using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Loaders
{
    public interface IConfigViewer
    {
        int RegisteredCategoriesCount { get; }
        int RegisteredFieldsCount { get; }
    }
}
