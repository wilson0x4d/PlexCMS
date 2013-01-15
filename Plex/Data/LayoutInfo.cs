using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Plex.Data
{
    [DataContract]
    public class LayoutInfo
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "sections")]
        public IEnumerable<LayoutSectionInfo> Sections { get; set; }
    }

    [DataContract]
    public class LayoutSectionInfo
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "layoutId")]
        public string LayoutID { get; set; }

        [DataMember(Name = "ordinal")]
        public int Ordinal { get; set; }

        [DataMember(Name = "modules")]
        public IEnumerable<LayoutModuleInfo> Modules { get; set; }
    }

    [DataContract]
    public class LayoutModuleInfo
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "layoutId")]
        public string LayoutID { get; set; }

        [DataMember(Name = "sectionId")]
        public string SectionID { get; set; }

        [DataMember(Name = "ordinal")]
        public int Ordinal { get; set; }
    }
}
