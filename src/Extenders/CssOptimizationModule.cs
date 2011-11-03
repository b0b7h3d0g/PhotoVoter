using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace PhotoVoterMvc
{
   /// <summary>
   /// You will need to configure this module in the web.config file of your
   /// web and register it with IIS before being able to use it. For more information
   /// see the following link: http://go.microsoft.com/?linkid=8101007
   /// </summary>
   public class CssOptimizationModule : IHttpModule
   {
      private const int MAX_FILE_SIZE = 10240;
      private static readonly Regex _urlReplaceExpression = new Regex(@"url\((?<name>[^\)]*)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

      public void Dispose()
      {
         // clean-up code here.
      }

      public void Init(HttpApplication context)
      {
         context.BeginRequest += BeginRequest;
      }

      static void BeginRequest(object sender, EventArgs e)
      {
         var application = (HttpApplication)sender;
         
         var context = application.Context;
         var request = context.Request;
         var response = context.Response;
         var filePath = request.PhysicalPath;

         // Not a CSS file? 
         if (!filePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) return;

         // IE6, IE7 does not support data urls; simply use the original css
         if (request.Browser.Browser.Equals("IE") && request.Browser.MajorVersion < 8) return;

         var originalCssFile = new FileInfo(filePath);
         var lastModified = originalCssFile.LastWriteTime;

         var incomingDate = request.Headers["If-Modified-Since"];

         DateTime incommingParsed;
         if (DateTime.TryParse(incomingDate, out incommingParsed) && incommingParsed == lastModified)
         {
            response.StatusCode = (int)HttpStatusCode.NotModified;
            response.End();
            return;
         }

         var ifNoneMatch = request.Headers["If-None-Match"];
         if (ifNoneMatch != null && ifNoneMatch.Contains(","))
         {
            ifNoneMatch = ifNoneMatch.Substring(0, ifNoneMatch.IndexOf(",", StringComparison.Ordinal));
         }
         
         var etag = string.Format("\"{0}\"", lastModified.ToFileTime());
         if (etag == ifNoneMatch)
         {
            response.StatusCode = (int)HttpStatusCode.NotModified;
            response.End();
            return;
         }
         
         // No ETAG or Modified-Since match, continue processing
         var minifiedCssFileName = Path.GetFileNameWithoutExtension(filePath) + ".min" + Path.GetExtension(filePath);
         var minifiedCssFile = new FileInfo(Path.Combine(originalCssFile.DirectoryName, minifiedCssFileName));

         if (!minifiedCssFile.Exists || minifiedCssFile.LastWriteTime != originalCssFile.LastWriteTime)
         {
            EmbedCssImages(originalCssFile, minifiedCssFile);
         }
         
         response.Cache.SetCacheability(HttpCacheability.Private);
         response.AddHeader("ETag", etag);
         response.AddHeader("Last-Modified", lastModified.ToUniversalTime().ToString("r"));
         response.ContentType = "text/css";
         response.WriteFile(minifiedCssFile.FullName);
         response.End();
      }

      private static void EmbedCssImages(FileInfo originalCssFile, FileInfo minifiedCssFile)
      {
         var cssRootPath = originalCssFile.DirectoryName;
         var cssContent = File.ReadAllText(originalCssFile.FullName);

         var content = _urlReplaceExpression.Replace(cssContent, match =>
         {
            var imageUrl = match.Groups[1].Value.Trim('\'', '"');
            if (imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
               // Cannot embed this URL, already embedded
               return match.ToString();
            }
            
            var imagePath = Path.Combine(cssRootPath, imageUrl);
            var imageFile = new FileInfo(imagePath);

            if (!imageFile.Exists || imageFile.Length >= MAX_FILE_SIZE)
            {
               // Cannot embed this URL, too large
               return match.ToString();
            }
            
            var imageBytes = File.ReadAllBytes(imagePath);
            return string.Format("url(data:image/{0};base64,{1})", imageFile.Extension.TrimStart('.'), Convert.ToBase64String(imageBytes));
         });

         File.WriteAllText(minifiedCssFile.FullName, content);
         minifiedCssFile.LastWriteTime = originalCssFile.LastWriteTime;
      }
   }
}


