using Plex.Data;
using Plex.Web.Controllers;
using Plex.WebSite.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Plex.WebSite.Areas.PlexAdmin.Controllers
{
    [Authorize(Roles = "plx:admin")]
    public class ControllerController : ApiController
    {
        /// <summary>
        /// Gets an index of all controllers, except those defined within 'areas'.
        /// </summary>
        /// <returns></returns>
        [ActionName("index")]
        public IEnumerable<ControllerInfo> Index()
        {
            return typeof(HomeController).Assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(System.Web.Mvc.Controller)) && !type.FullName.Contains(".Areas."))
                .Select(type => new ControllerInfo
                {
                    ID = type.Name.Replace("Controller", ""),
                    IsPageController = type.IsSubclassOf(typeof(GenericPageController))
                });
        }
    }
}
