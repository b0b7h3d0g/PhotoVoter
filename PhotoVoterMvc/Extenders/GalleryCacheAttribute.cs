using System;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PhotoVoterMvc.Controllers;

namespace PhotoVoterMvc
{
   /// <summary>
   /// Caches the result of gallery controller using conditional caching (ETag).
   /// </summary>
   [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
   public class GalleryCacheAttribute : ActionFilterAttribute
   {
      /// <summary>
      /// Defines the parameter that contains the gallery name. This is used to cache output per gallery.
      /// </summary>
      /// <remarks>If not used caches the output for all galleries</remarks>
      public string GalleryNameParameter { get; set; }
      
      public override void OnActionExecuting(ActionExecutingContext filterContext)
      {
         var httpContext = filterContext.HttpContext;

         var controller = filterContext.Controller as GalleryController;
         if (controller == null)
         {
            throw new InvalidOperationException("This attribute can only be used for GalleryController");
         }

         DateTime lastModified;
         
         if (!string.IsNullOrEmpty(GalleryNameParameter))
         {
            // Get the last change date for the specified gallery
            var galleryName = filterContext.ActionParameters[GalleryNameParameter] as string;
            lastModified = controller.GalleryService.GetLastChange(galleryName);
         }
         else
         {
            // Get the last changed date for all galleries
            lastModified = controller.GalleryService.GetLastChange();
         }

         var lastLogin = httpContext.Session["LastLoginDate"] as DateTime?;
         if (lastLogin != null && lastLogin > lastModified)
         {
            lastModified = lastLogin.Value;
         }
         
         //
         // decide if the page should be rendered again or not, use ETAG
         //
         var etag = string.Format("\"{0}\"", lastModified.ToFileTime());
         lastModified = new DateTime(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second);

         var incomingDate = httpContext.Request.Headers["If-Modified-Since"];

         DateTime incommingParsed;
         if (DateTime.TryParse(incomingDate, out incommingParsed) && incommingParsed == lastModified)
         {
            filterContext.Result = new HttpStatusCodeResult((int)HttpStatusCode.NotModified);
            return; // Terminate action, abort all further processing
         }

         var ifNoneMatch = httpContext.Request.Headers["If-None-Match"];
         if (ifNoneMatch != null && ifNoneMatch.Contains(","))
         {
            ifNoneMatch = ifNoneMatch.Substring(0, ifNoneMatch.IndexOf(",", StringComparison.Ordinal));
         }
         if (etag == ifNoneMatch)
         {
            filterContext.Result = new HttpStatusCodeResult((int)HttpStatusCode.NotModified);
            return; // Terminate action, abort all further processing
         }

         httpContext.Response.Cache.SetCacheability(HttpCacheability.Private);
         httpContext.Response.Expires = -600;
         //httpContext.Response.Cache.SetNoStore();
         httpContext.Response.AddHeader("ETag", etag);
         httpContext.Response.AddHeader("Last-Modified", lastModified.ToUniversalTime().ToString("r"));

         // Continue processing
         base.OnActionExecuting(filterContext);
      }
   }
}