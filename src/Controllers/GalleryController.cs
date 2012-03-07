using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Xml;
using PhotoVoterMvc.Extenders;
using PhotoVoterMvc.Services;
using PhotoVoterMvc.Services.Exceptions;
using PhotoVoterMvc.Services.Model;

namespace PhotoVoterMvc.Controllers
{
   [HandleError(View = "Error")]
   public class GalleryController : Controller
   {
      internal readonly IGalleryService GalleryService;

      public GalleryController(IGalleryService galleryService)
      {
         GalleryService = galleryService;
      }

      /// <summary>
      /// Initializes data that might not be available when the constructor is called.
      /// </summary>
      /// <param name="requestContext">The HTTP context and route data.</param>
      protected override void Initialize(System.Web.Routing.RequestContext requestContext)
      {
         base.Initialize(requestContext);
      }

      /// <summary>
      /// Adds a new vote for a gallery image
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="imageName"></param>
      /// <returns></returns>
      [GalleryAuthorize]
      public ActionResult Like(string galleryName, string imageName)
      {
         return Vote(galleryName, imageName, false);
      }

      /// <summary>
      /// Removes the vote for a gallery image
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="imageName"></param>
      /// <returns></returns>
      [GalleryAuthorize]
      public ActionResult Dislike(string galleryName, string imageName)
      {
         return Vote(galleryName, imageName, true);
      }

      [GalleryCache(GalleryNameParameter = "galleryName")]
      public ActionResult Feed(string format, string galleryName)
      {
         var baseUri = Request.Url;
         
         var settings = GalleryService.GetGallerySettings(galleryName);
         var gallery = GalleryService.GetGallery(galleryName, true, User, null, "bydate");
         var galleryUrl = Url.Action("Show", "Gallery", new { galleryName });

         var contentFormat = "<img src=\"{0}\" />" + (settings.StatsEnabled ? "<div>Voted by {1} user(s), Uploaded by {2}</div>" : null);

         var items = from photo in gallery.Photos
                     let title = photo.Title ?? photo.Name
                     let itemUri = new Uri(baseUri, galleryUrl + "#" + photo.GetHash())
                     let imageUri = new Uri(baseUri, Url.ThumbnailUrl(galleryName, photo.Name))
                     let author = photo.User != null && settings.StatsEnabled ? Regex.Replace(photo.User, ".*\\\\(.*)|(.*)@.*", "$1$2") : "Anonymous" // remove domain / email            
                     let description = new TextSyndicationContent(string.Format(contentFormat, imageUri, photo.TotalVotes, author), TextSyndicationContentKind.Html)
                     select new SyndicationItem(title, description, itemUri, itemUri.ToString(), photo.Date)
                     {
                        Title = new TextSyndicationContent(title),
                        Summary = description,
                        Authors = { new SyndicationPerson { Name = author } },
                        PublishDate = photo.PublishDate
                     };

         var gallerySummary = string.Format("Publish date: {0}, {1} vote(s), {2} photo(s) by {3} user(s)", gallery.PublishDate.ToShortDateString(), gallery.TotalVotes, gallery.Photos.Count(), gallery.TotalUsers);
         var galleryUri = new Uri(baseUri, galleryUrl);
         var feed = new SyndicationFeed(galleryName, gallerySummary, galleryUri, items)
                     {
                        Id = galleryUri.ToString(),
                        Title = new TextSyndicationContent(galleryName),
                        LastUpdatedTime = gallery.Date,
                        Description = new TextSyndicationContent(gallerySummary),
                     };

         return new SyndicationActionResult { Feed = feed, Format = format };
      }

      /// <summary>
      /// Toggles the vote (like or unlinke) for a gallery image
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="imageName"></param>
      /// <returns></returns>
      [HttpGet]
      [GalleryAuthorize]
      public ActionResult ToggleVote(string galleryName, string imageName)
      {
         return Vote(galleryName, imageName);
      }

      /// <summary>
      /// Resizes a gallery image
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="imageName"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <returns></returns>
      [HttpGet]
      [OutputCache(Location = OutputCacheLocation.Client, Duration = 60)]
      public ActionResult Thumbnail(string galleryName, string imageName, int width, int height)
      {
         try
         {
            var thumbnailFile = GalleryService.Thumbnail(galleryName, imageName, width, height);

            return File(thumbnailFile, "image/jpeg");
         }
         catch (FileNotFoundException)
         {
            return HttpNotFound();
         }
         catch (Exception ex)
         {
            return HttpNotFound(ex.Message);
         }
      }

      /// <summary>
      /// Creates the model and renders the specified gallery
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="filter"></param>
      /// <param name="sort"></param>
      /// <returns></returns>
      [HttpGet]
      [GalleryCache(GalleryNameParameter = "galleryName")]
      public ActionResult Show(string galleryName, string filter, string sort)
      {
         var gallery = GalleryService.GetGallery(galleryName, true, User, filter, sort);
         var settings = GalleryService.GetGallerySettings(galleryName);

         // add some data to view data dictionary
         ViewBag.Filter = filter;
         ViewBag.SortOrder = sort;
         ViewBag.AllowUpload = settings.UploadEnabled;
         ViewBag.VotingEnabled = settings.VotingEnabled;
         ViewBag.StatsEnabled = settings.StatsEnabled;
         ViewBag.IsAdminUser = GalleryService.IsAdminUser(User);

         if (Request.IsAjaxRequest())
         {
            return PartialView("_GalleryImages", gallery);
         }
         else
         {
            return View("Show", gallery);
         }
      }

      [HttpGet]
      [GalleryCache]
      public ActionResult Index(int top = 6)
      {
         var modelData = GalleryService.GetGalleries(top);
         ViewBag.IsAdminUser = GalleryService.IsAdminUser(User);
         return View("Index", modelData);
      }

      [HttpGet]
      [GalleryAuthorize]
      public ActionResult ToggleSettings(string galleryName, string param)
      {
         try
         {
            var setting = GalleryService.GetGallerySettings(galleryName);

            switch (param)
            {
               case "voting":
                  setting.VotingEnabled = !setting.VotingEnabled;
                  break;
               case "uploading":
                  setting.UploadEnabled = !setting.UploadEnabled;
                  break;
               case "stats":
                  setting.StatsEnabled = !setting.StatsEnabled;
                  break;
            }

            GalleryService.SaveGallerySettings(setting, User);

            return RedirectToAction("Show", new { GalleryName = galleryName });
         }
         catch (HttpException hex)
         {
            return new HttpStatusCodeResult(hex.GetHttpCode(), hex.Message);
         }
      }

      [HttpPost]
      [GalleryAuthorize]
      public ActionResult UploadImage(string galleryName, string qqfile)
      {
         try
         {
            string fileName;
            Stream stream;

            if (Request.Files != null && Request.Files.Count > 0)
            {
               // IE
               var postedFile = Request.Files[0];

               stream = postedFile.InputStream;
               fileName = Path.GetFileName(postedFile.FileName);
            }
            else
            {
               // Webkit, Mozilla
               stream = Request.InputStream;
               fileName = qqfile;
            }

            // queuing uploads
            GalleryService.UploadImage(galleryName, fileName, User, stream);

            return Json(new { success = true }, "text/html");
         }
         catch (Exception ex)
         {
            return Json(new { success = false, message = ex.Message }, "text/html");
         }
      }

      [HttpPost]
      [GalleryAuthorize]
      public ActionResult DeleteImage(string galleryName, string imageName)
      {
         try
         {
            // Remove image
            GalleryService.DeleteImage(galleryName, imageName, User);

            if (Request.IsAjaxRequest())
            {
               return new HttpStatusCodeResult((int)HttpStatusCode.OK);
            }
         }
         catch (Exception ex)
         {
            if (Request.IsAjaxRequest())
            {
               return new HttpStatusCodeResult((int)HttpStatusCode.InternalServerError, ex.Message);
            }

            ModelState.AddModelError("", ex.Message);
         }

         return RedirectToAction("Show", new { GalleryName = galleryName });
      }

      [HttpGet]
      public ActionResult GetImage(string galleryName, string imageName)
      {
         var model = GalleryService.GetGalleryImage(galleryName, imageName, User);
         var settings = GalleryService.GetGallerySettings(galleryName);

         ViewBag.VotingEnabled = settings.VotingEnabled;
         ViewBag.StatsEnabled = settings.StatsEnabled;
         ViewBag.IsAdminUser = GalleryService.IsAdminUser(User);
         ViewBag.AllowUpload = settings.UploadEnabled;

         return PartialView("_GalleryImage", model);
      }

      [HttpPost]
      [GalleryAuthorize]
      public ActionResult Create(GalleryData gallery)
      {
         try
         {
            if (gallery != null && !string.IsNullOrWhiteSpace(gallery.Name))
            {
               gallery = GalleryService.CreateGallery(gallery.Name, User);
            }
            else
            {
               ModelState.AddModelError("Error", "A gallery name is required");
            }
         }
         catch (GalleryAlreadyExistsException)
         {
            ModelState.AddModelError("Error", "A gallery with the same name already exists");  
         }
         catch (Exception ex)
         {
            ModelState.AddModelError("Error", "Unable to create gallery. " + ex.Message);
         }

         return PartialView("_CreateGallery", gallery);
      }

      [HttpGet]
      [GalleryAuthorize]
      public ActionResult Create()
      {
         return RedirectToAction("Index");
      }

      private ActionResult Vote(string galleryName, string imageName, bool? removeVote = null)
      {
         try
         {
            var data = GalleryService.Vote(galleryName, imageName, User, removeVote);

            if (Request.IsAjaxRequest())
            {
               // if ajax request => return json
               return Json(new { data.UserVote, data.TotalVotes }, JsonRequestBehavior.AllowGet);
            }

         }
         catch (HttpException hex)
         {
            if (Request.IsAjaxRequest())
            {
               return new HttpStatusCodeResult(hex.GetHttpCode(), hex.Message);
            }

            ModelState.AddModelError("", hex.Message);
         }

         // not an ajax request, redirect to gallery
         return RedirectToAction("Show", new { GalleryName = galleryName });
      }
   }
}
