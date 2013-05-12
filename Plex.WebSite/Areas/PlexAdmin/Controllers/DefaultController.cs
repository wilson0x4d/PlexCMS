using Plex.WebSite.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Plex.WebSite.Areas.PlexAdmin.Controllers
{
    [Authorize(Roles="plx:admin")]
    [InitializeSimpleMembership]
    public class DefaultController : Controller
    {
        //
        // GET: /PlexAdmin/Default/

        public ActionResult Index()
        {
            return View();
        }

    }
}
