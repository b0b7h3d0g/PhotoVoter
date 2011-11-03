using System;
using System.Linq;
using System.Configuration;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using PhotoVoterMvc.Extenders;
using PhotoVoterMvc.Models;

namespace PhotoVoterMvc
{
   // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
   // visit http://go.microsoft.com/?LinkId=9394801

   public class MvcApplication : System.Web.HttpApplication
   {
      protected void Application_Start()
      {
         AreaRegistration.RegisterAllAreas();
         RegisterGlobalFilters(GlobalFilters.Filters);
         RegisterRoutes(RouteTable.Routes);

         // This is now done by MefContrib.MVC3
         // RegisterDependencyResolvers();

         var db = new DatabaseEntities();
         if (!db.DatabaseExists())
         {
            // create database
            db.CreateDatabase();
         }
      }

      // Poor's man IoC, replaced by MefContrib.MVC
      // private static void RegisterDependencyResolvers()
      // {
      //    DependencyResolver.SetResolver(new ControllerServiceResolver());
      // }

      public static void RegisterGlobalFilters(GlobalFilterCollection filters)
      {
         filters.Add(new HandleErrorAttribute());
      }

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