using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Plex.Web.Controllers
{
    /// <summary>
    /// <para>A basic CMS Page controller.</para>
    /// <para>This controller corresponds to routes "Default1" and "Default2".</para>
    /// <para>This implementation is required for CMS Site to support Page creation without requiring a developer.</para>
    /// <para>It is NOT a requirement that controllers derive from this implementation.</para>
    /// </summary>
    public abstract class GenericPageController : Controller
    {
        public ActionResult ViewPage(string page)
        {
            return View(page);
        }
    }
}
