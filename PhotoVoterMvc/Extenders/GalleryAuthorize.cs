using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace PhotoVoterMvc.Extenders
{
   public class GalleryAuthorize : AuthorizeAttribute
   {
      protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
      {
         if (!filterContext.RequestContext.HttpContext.Request.IsAjaxRequest())
         {
            base.HandleUnauthorizedRequest(filterContext);
         }
         else
         {
            // Ajax request doesn't return to login page, it just returns 401 error. 
            var redirectToUrl = FormsAuthentication.LoginUrl;// + "?returnUrl=" + filterContext.HttpContext.Request.RawUrl;

            // Important: Cannot set 401 as asp.net intercepts and returns login page so instead set 530 User access denied            
            filterContext.Result = new HttpStatusCodeResult(530); //User Access Denied
            
            filterContext.HttpContext.Response.AddHeader("X-Redirect-To", redirectToUrl);
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
         }
      }
   }
}