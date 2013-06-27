using System.Web.Mvc;
using System.Web.Routing;

namespace PhotoVoterMvc.App_Start
{
   public class RouteConfig
   {
      public static void RegisterRoutes(RouteCollection routes)
      {
         routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

         routes.MapRoute(
            "Feed",
            "Feed/{galleryName}/{format}",
            new { controller = "Gallery", action = "Feed", format = "RSS" }
         );

         routes.MapRoute(
            "Authenticate",
            "User/{action}/{provider}",
            new { controller = "User", action = "Authenticate", provider = UrlParameter.Optional }
         );

         routes.MapRoute(
             "Contact",
             "Contact/{action}",
             new { controller = "Contact", action = "Index" }
         );

         routes.MapRoute(
            "Thumbnails", // Route name
            "Content/{galleryName}/Thumb/{imageName}", // URL with parameters
            new { controller = "Gallery", action = "Thumbnail", galleryName = "Photos", imageName = UrlParameter.Optional, width = 220, height = 150 } // Parameter defaults
            );

         routes.MapRoute(
             "Vote",
             "{controller}/{action}/{galleryName}/{imageName}",
             new { controller = "Gallery", action = "Index", imageName = UrlParameter.Optional } // Parameter defaults
         );

         routes.MapRoute(
             "Main",
             "{controller}/{action}/{galleryName}",
             new { controller = "Gallery", action = "Index", galleryName = UrlParameter.Optional }
         );
      }
   }
}