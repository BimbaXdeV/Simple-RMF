using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class RmfConstants
    {
        public const string ServerLogo = @"
 .|'''.|   ||                      '||             '||''|.   '||    ||' '||''''| 
 ||..  '  ...  .. .. ..   ... ...   ||    ....      ||   ||   |||  |||   ||  .   
  ''|||.   ||   || || ||   ||'  ||  ||  .|...||     ||''|'    |'|..'||   ||''|   
.     '||  ||   || || ||   ||    |  ||  ||          ||   |.   | '|' ||   ||      
|'....|'  .||. .|| || ||.  ||...'  .||.  '|...'    .||.  '|' .|. | .||. .||.     
                           ||                                                    
                          ''''                                                   
";
        public const string InitComponentLogTemplate = "{Label, -16}: {Loaded} / {Total}";
    }
}
