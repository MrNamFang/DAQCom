using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DAQCom
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                // SỬA: Đổi controller từ "Home" thành "QuanAn"
                defaults: new { controller = "QuanAn", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}