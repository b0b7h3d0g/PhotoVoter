using System;
using System.ServiceModel.Syndication;
using System.Web.Mvc;
using System.Xml;

namespace PhotoVoterMvc.Extenders
{
   public class SyndicationActionResult : ActionResult
   {
      /// <summary>
      /// The syndication feed
      /// </summary>
      public SyndicationFeed Feed { get; set; }

      /// <summary>
      /// Gets or sets the format type
      /// </summary>
      public string Format { get; set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="context"></param>
      public override void ExecuteResult(ControllerContext context)
      {
         context.HttpContext.Response.ContentType = "application/rss+xml";

         using (var writer = XmlWriter.Create(context.HttpContext.Response.Output, new XmlWriterSettings { OmitXmlDeclaration = true }))
         {
            if (string.Equals(Format, "Atom", StringComparison.OrdinalIgnoreCase))
            {
               Feed.GetAtom10Formatter().WriteTo(writer);
            }
            else if (string.Equals(Format, "Rss", StringComparison.OrdinalIgnoreCase))
            {
               Feed.GetRss20Formatter(true).WriteTo(writer);
            }
            else
            {
               writer.WriteRaw(Feed.ToString());
            }

            writer.Flush();
         }
      }
   }
}