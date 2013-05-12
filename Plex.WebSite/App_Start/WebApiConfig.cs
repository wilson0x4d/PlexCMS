using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Plex.WebSite
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "RestfulApi",
                routeTemplate: "api/{controller}"
            );
            config.Routes.MapHttpRoute(
                name: "PostApi",
                routeTemplate: "api/{controller}/{action}"
            );
        }
    }
}
