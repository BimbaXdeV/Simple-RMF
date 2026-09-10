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
        public const string InitComponentLogTemplate = "{Indent}{Label, -16}: {StartColor}{Loaded} / {Total}{EndColor}";

        public const string DateTimeFormatYmdHms = @"yyyy\.MM\.dd HH\:mm\:ss";
        public const string DateTimeFormatHms = @"HH\:mm\:ss";
        public const string TimeSpanFormatDhms = @"dd\.hh\:mm\:ss";
        public const string TimeSpanFormatDhm = @"dd\.hh\:mm";
        public const string TimeSpanFormatHms = @"hh\:mm\:ss";
    }
}
