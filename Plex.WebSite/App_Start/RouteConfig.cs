using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Plex.WebSite
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // TODO: allow user to configure a default page so we're not hardcoding here
            routes.MapRoute(
                name: "Default1",
                url: "",
                defaults: new { controller = "Home", action = "ViewPage", page = "Index" }
            );

            // enumerate all controllers which implement 'GenericPageController' and register them here
            // TODO: find a better solution that doesn't involve third party code
            var controllers = new Areas.PlexAdmin.Controllers.ControllerController();
            controllers.Index()
                .Where(c => c.IsPageController)
                .ToList()
                .ForEach(c=>{
                    routes.MapRoute(
                        name: "PlexCMS_" + c.ID,
                        url: c.ID + "/{page}",
                        defaults: new { controller = c.ID, action = "ViewPage", page = "Index" }
                    );
                });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}