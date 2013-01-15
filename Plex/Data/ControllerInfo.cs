using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Plex.Data
{
    [DataContract]
    public class ControllerInfo
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }

        /// <summary>
        /// <para>True if Controller can support new content pages without being recompiled (such as with GenericPageController).</para>
        /// <para>When False, user pages can only be created for Controller Actions which already exist. Pages which do not have a corresponding Action will not be discovered during Admin.</para>
        /// <para>When False, 'create new page' functionality should provide a droplist instead of a text box</para>
        /// </summary>
        // TODO: not currently enforced by client
        [DataMember(Name = "isPageController")]
        public bool IsPageController { get; set; }
    }
}
