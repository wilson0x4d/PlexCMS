using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Plex.Data
{
    // "ride of the valkyries"
    [DataContract]
    public class MediaInfo
    {
        [DataMember(Name = "folderName")]
        public string FolderName { get; set; }

        [DataMember(Name = "fileName")]
        public string FileName { get; set; }

        [DataMember(Name = "length")]
        public int Length { get; set; }
    }
}
