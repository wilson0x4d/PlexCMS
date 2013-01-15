using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Plex.Data
{
    [DataContract]
    public class ModuleInfo
    {
        // unique
        [DataMember(Name = "id")]
        public string ID { get; set; }

        // non-unique
        [DataMember(Name = "text")]
        public string Text { get; set; }
    }
}
