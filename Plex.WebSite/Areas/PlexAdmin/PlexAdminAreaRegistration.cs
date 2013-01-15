using System.Web.Mvc;
using System.Web.Optimization;

namespace Plex.WebSite.Areas.PlexAdmin
{
    public class PlexAdminAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "PlexAdmin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            BundleTable.Bundles.Add(new StyleBundle("~/PlexAdmin/css").Include(
                "~/Areas/PlexAdmin/Content/Area.css"));
            context.MapRoute(
                "PlexAdmin_default",
                "PlexAdmin/{controller}/{action}",
                new { controller = "Default", action = "Index" }
            );
        }
    }
}
