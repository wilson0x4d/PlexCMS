using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Plex.Data
{
    [DataContract]
    public class PageInfo
    {
        // unique
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "controllerId")]
        public string ControllerID { get; set; }

        // non-unique
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "layoutId")]
        public string LayoutID { get; set; }

        [DataMember(Name = "sections")]
        public IEnumerable<PageSectionInfo> Sections { get; set; }

        [DataMember(Name = "body")]
        public string Body { get; set; }
    }

    [DataContract]
    public class PageSectionInfo
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "pageId")]
        public string PageID { get; set; }

        [DataMember(Name = "controllerId")]
        public string ControllerID { get; set; }

        [DataMember(Name = "ordinal")]
        public int Ordinal { get; set; }

        [DataMember(Name = "isPresent")]
        public bool IsPresent { get; set; }

        [DataMember(Name = "modules")]
        public IEnumerable<PageModuleInfo> Modules { get; set; }
    }

    [DataContract]
    public class PageModuleInfo
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "pageId")]
        public string PageID { get; set; }

        [DataMember(Name = "controllerId")]
        public string ControllerID { get; set; }

        [DataMember(Name = "sectionId")]
        public string SectionID { get; set; }

        [DataMember(Name = "ordinal")]
        public int Ordinal { get; set; }
    }
}
