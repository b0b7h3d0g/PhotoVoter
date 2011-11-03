using System.Web;
using System.Web.Mvc;

namespace PhotoVoterMvc
{
   public static class HttpSubstitution
   {
      public delegate string MvcCacheCallback(HttpContextBase context);

      public static object Substitute(this HtmlHelper html, MvcCacheCallback cb)
      {
         html.ViewContext.HttpContext.Response.WriteSubstitution(c => HttpUtility.HtmlEncode(cb(new HttpContextWrapper(c))));
         return null;
      }
   }
}