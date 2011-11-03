using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Encoder = System.Drawing.Imaging.Encoder;

namespace PhotoVoterMvc.Extenders
{
   public static class ExtensionMethods
   {      
      //
      // Image
      //
      private static ImageCodecInfo GetEncoder(this ImageFormat format)
      {
         return ImageCodecInfo.GetImageEncoders().SingleOrDefault(c => c.FormatID == format.Guid);
      }

      /// <summary>
      /// Saves an <see cref="Image"/> to the specified file.
      /// </summary>
      /// <param name="img"></param>
      /// <param name="fileName"></param>
      /// <param name="quality"></param>
      /// <param name="format"></param>
      public static void Save(this Image img, string fileName, Int64 quality, ImageFormat format = null)
      {
         if (format == null)
         {
            format = ImageFormat.Jpeg;
         }

         using (var param = new EncoderParameters { Param = new[] { new EncoderParameter(Encoder.Quality, quality) }})
         {
            img.Save(fileName, format.GetEncoder(), param);
         }
      }

      public static Image Resize(this Image img, int width, int height, bool crop = false, bool preserveProperties = true)
      {
         if (width == 0 || height == 0 || (width > img.Width && height > img.Height))
         {
            return img;
         }

         var percentW = (width / (float)img.Width);
         var percentH = (height / (float)img.Height);

         var percent = crop ? Math.Max(percentH, percentW) : Math.Min(percentH, percentW);

         var resizeWidth = (int)(img.Width * percent);
         var resizeHeight = (int)(img.Height * percent);

         Image resizedImage = crop ? new Bitmap(width, height) : new Bitmap(resizeWidth, resizeHeight);
         
         using (var g = Graphics.FromImage(resizedImage))
         {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(img, (resizedImage.Width - resizeWidth) / 2, (resizedImage.Height - resizeHeight) / 2, resizeWidth, resizeHeight);
         }

         if (preserveProperties)
         {
            // Keep image properties
            foreach (var propertyItem in img.PropertyItems)
            {
               resizedImage.SetPropertyItem(propertyItem);
            }            
         }

         return resizedImage;
      }

      /// <summary>
      /// Returns Title and Description stored along with the image
      /// </summary>
      /// <param name="image"></param>
      /// <returns></returns>
      public static string GetImageTitle(this Image image)
      {
         //Title/Description = 0x010E
         //Title = 0x9C9B
         //Comments = 0x9C9C
         //Keywords\Tag = 0x9C9D   This is what you are looking for
         //Subject = 0x9C9E
         //DateTaken = 0x9003
         //ExposureBias = 0x9204
         //MaxAperture = 0x9205
         //LightSource = 0x9208
         //Flash = 0x9209
         //FocalLength = 0x920A
         //FNumber = 0x829D
         //ExposureTime = 0x829A
         //MeteringMode = 0x9207
         var title = (from prop in image.PropertyItems
                      where prop.Value != null && prop.Len > 0 && (prop.Id == 0x010E || prop.Id == 0x9C9B)
                      select prop).FirstOrDefault();

         if (title == null)
         {
            return null;
         }
         
         var result = (title.Type == 1) ? Encoding.Unicode.GetString(title.Value) : Encoding.UTF8.GetString(title.Value);

         return result.Trim(' ', '\t', '\n', '\0');
      }

      public static string ThumbnailUrl(this UrlHelper helper, string galleryName, string imageName)
      {
         return helper.Content("~/Content/" + HttpUtility.UrlPathEncode(galleryName) + "/thumb/" + HttpUtility.UrlPathEncode(imageName));
      }

      public static string ImageUrl(this UrlHelper helper, string galleryName, string imageName)
      {
         return helper.Content("~/Content/" + HttpUtility.UrlPathEncode(galleryName) + "/" + HttpUtility.UrlPathEncode(imageName));
      }

      //
      // Collections
      //
      private static Random _random;

      public static T GetRandomElement<T>(this IEnumerable<T> list)
      {
         if (list == null)
         {
            throw new ArgumentNullException("list");
         }

         if (_random == null)
         {
            _random = new Random();
         }

         var count = list.Count();

         // If there are no elements in the collection, return the default value of T
         return count == 0 ? default(T) : list.ElementAt(_random.Next(count));
      }
      
      public static IEnumerable<FileInfo> EnumerateImages(this DirectoryInfo directoryInfo)
      {
         return 
            from file in directoryInfo.EnumerateFiles()
            where (file.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden &&
                  (file.Attributes & FileAttributes.System) != FileAttributes.System              
            select file;
      }
   }
}