using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Plex.Data
{
    [DataContract]
    public class FolderInfo
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "files")]
        public IEnumerable<MediaInfo> Files { get; set; }
    }
}
