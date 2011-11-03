using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Data;
using System.Data.Objects;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Web;
using PhotoVoterMvc.Extenders;
using PhotoVoterMvc.Models;
using PhotoVoterMvc.Services.Exceptions;
using PhotoVoterMvc.Services.Model;

namespace PhotoVoterMvc.Services
{
   [Export(typeof(IGalleryService))]
   public class GalleryService : IGalleryService
   {
      private static readonly object UploadSyncContext = new object();

      private readonly string _physicalApplicationPath;

      /// <summary>
      /// Initializes data that might not be available when the service is called.
      /// </summary>
      public GalleryService()
      {
         // TODO: Read from configuration or find another way to initialize 
         _physicalApplicationPath = HttpContext.Current.Request.PhysicalApplicationPath;
      }

      public bool IsAdminUser(IPrincipal user)
      {
         return user.Identity.IsAuthenticated && user.IsInRole("Administrators");
      }

      private static int UserUploadLimit
      {
         get
         {
            int limit;

            var limitString = ConfigurationManager.AppSettings["UserUploadLimit"];

            Int32.TryParse(limitString, out limit);

            return limit;
         }
      }

      private static Size GetMaxImageSize()
      {
         int width = 0;
         int height = 0;

         var limitString = ConfigurationManager.AppSettings["ResizeImage"];

         if (!string.IsNullOrEmpty(limitString))
         {
            var splitted = limitString.Split('x', ':');

            Int32.TryParse(splitted[0], out width);
            Int32.TryParse(splitted[1], out height);
         }

         return new Size(width, height);
      }


      private static void SetLastChange(DirectoryInfo galleryDirectory)
      {
         var tempFilePath = Path.Combine(galleryDirectory.FullName, Path.GetRandomFileName());
         using (File.Create(tempFilePath, 1, FileOptions.DeleteOnClose))
         {
            // create a temp file in order to change the directory last write time
         }
      }

      public DateTime GetLastChange(string galleryName = null)
      {
         //var entities = new DatabaseEntities();
         //var lastWriteTime = (from vote in entities.Votes
         //                     where vote.Gallery == galleryName
         //                     select vote.LastUpdate).FirstOrDefault();
         //return lastWriteTime ?? MinFileTime;

         DateTime lastWriteTime;

         if (galleryName != null)
         {
            var galleryDirectory = GetGalleryDirectory(galleryName);
            lastWriteTime = galleryDirectory.LastWriteTime;
         }
         else
         {
            var galleryDirectories = GetGalleryDirectory();
            lastWriteTime = galleryDirectories.EnumerateDirectories().Max(dir => dir.LastWriteTime);
         }

         return lastWriteTime;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="user"></param>
      /// <param name="filter"></param>
      /// <param name="sort"></param>
      /// <returns></returns>
      public IEnumerable<GalleryData> GetGalleryObjects(string galleryName, IPrincipal user, string filter, string sort)
      {
         var galleryFolder = GetGalleryDirectory(galleryName);
         var galleryImages = galleryFolder.EnumerateImages();

         var entities = new DatabaseEntities();
         var userName = user.Identity.Name;

         // get all votes grouped by image
         var votes = from entity in entities.Votes
                     where entity.Gallery == galleryName
                     group entity by entity.Image
                        into votesGroup
                        select new
                        {
                           ImageName = votesGroup.Key,
                           Count = votesGroup.Count(),
                           UserVote = votesGroup.Any(g => g.User == userName)
                        };
         
         // for each gallery file, join the votes create the model for the view
         // filter by user (if required)
         var modelData = from imageFile in galleryImages
                         join vote in votes on imageFile.Name equals vote.ImageName into joined
                         join upload in entities.Uploads on new { galleryName, imageName = imageFile.Name } equals new { galleryName = upload.Gallery, imageName = upload.Image } into uploads
                         from upload in uploads.DefaultIfEmpty()
                         from vote in joined.DefaultIfEmpty()
                         where ((filter != "user" && filter != "upload") || (filter == "user" && vote != null && vote.UserVote) || (filter == "upload" && upload != null && upload.User == userName))
                         let title = upload != null ? upload.Title : null
                         let author = upload != null ? upload.User : null
                         select new GalleryData
                                   {
                                      FullPath = imageFile.FullName,
                                      Gallery = galleryName,
                                      Title = title,
                                      Name = imageFile.Name,
                                      VoteCount = vote != null ? vote.Count : 0,
                                      UserVote = vote != null && vote.UserVote,
                                      Date = imageFile.LastWriteTimeUtc,
                                      User = author,
                                      PublishDate = imageFile.CreationTimeUtc,
                                      TotalPhotos = 1
                                   };
         
         var rand = new Random();

         // applies sort order
         if (string.Equals(sort, "byvote", StringComparison.OrdinalIgnoreCase))
         {
            var statsEnabled = (from set in entities.Settings where set.Gallery == galleryName select set.StatsEnabled).SingleOrDefault();
            if (!statsEnabled && !IsAdminUser(user))
            {
               throw new SecurityException("Not authorized");
            }

            modelData = modelData.OrderByDescending(t => t.VoteCount);
         }
         else if (string.Equals(sort, "bydate", StringComparison.OrdinalIgnoreCase))
         {
            modelData = modelData.OrderByDescending(t => t.Date);
         }
         else
         {
            modelData = modelData.OrderBy(t => rand.Next(1000));
         }

         return modelData;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="top"></param>
      /// <returns></returns>
      public IEnumerable<GalleryData> GetGalleries(int top)
      {
         var contentDirectory = GetGalleryDirectory();

         var entities = new DatabaseEntities();

         // get available galleries (top x)
         var galleryFolders = from folder in contentDirectory.GetDirectories()
                              orderby folder.CreationTime descending
                              select folder;

         // for each gallery folder, join the votes create the model for the view
         var model = from gallery in galleryFolders.Take(top)
                     join votes in entities.Votes on gallery.Name equals votes.Gallery into joined
                     let stats = from vote in joined group vote by vote.User into groupedByUser select new { User = groupedByUser.Key, Votes = groupedByUser.Count() }
                     select new GalleryData
                               {
                                  FullPath = contentDirectory.FullName,
                                  Gallery = gallery.Name,
                                  TotalPhotos = gallery.EnumerateImages().Count(),
                                  Date = gallery.LastWriteTimeUtc,
                                  PublishDate = gallery.CreationTimeUtc,
                                  VoteCount = stats.Sum(entry => entry.Votes),
                                  UserCount = stats.Count()
                               };

         return model;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public GalleryData GetGallery(string galleryName)
      {
         var gallery = GetGalleryDirectory(galleryName);

         var entities = new DatabaseEntities();

         var votes =
            from vote in entities.Votes
            where galleryName == vote.Gallery
            group vote by vote.User
            into groupedByUser
            select new { User = groupedByUser.Key, Votes = (int?)groupedByUser.Count() };
         
         return new GalleryData
         {
            FullPath = gallery.FullName,
            Gallery = gallery.Name,
            TotalPhotos = gallery.EnumerateImages().Count(),
            Date = gallery.LastWriteTimeUtc,
            PublishDate = gallery.CreationTimeUtc,
            VoteCount = votes.Sum(entry => entry.Votes) ?? 0,
            UserCount = votes.Count()
         };
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="imageName"></param>
      /// <param name="user"></param>
      /// <param name="removeVote"></param>
      /// <exception cref="HttpException">If something went wrong</exception>
      /// <returns></returns>
      public GalleryData Vote(string galleryName, string imageName, IPrincipal user, bool? removeVote = null)
      {
         var galleryDirectory = GetGalleryDirectory(galleryName);

         FileInfo galleryFile;

         try
         {
            galleryFile = galleryDirectory.GetFiles(imageName).First();
         }
         catch (Exception)
         {
            throw new HttpException(512, "The image '" + imageName + "' is not in this gallery");
         }

         try
         {
            bool? voteStatus = null;
            var entities = new DatabaseEntities();

            var setting = (from set in entities.Settings where set.Gallery == galleryName select set).SingleOrDefault();
            if (setting == null || !setting.VotingEnabled)
            {
               throw new HttpException(511, "Voting is disabled for this gallery");
            }

            var ownedByUser = entities.Uploads.Any(upload => upload.Gallery == galleryName && upload.User == user.Identity.Name && upload.Image == imageName);
            if (ownedByUser)
            {
               throw new HttpException(513, "You cannot vote your own picture");
            }

            // gets all votes for this image
            var votes = from vote in entities.Votes
                        where vote.Gallery == galleryName && vote.Image == imageName
                        select vote;

            // ... and filter by user
            var userVote = votes.Where(vote => vote.User == user.Identity.Name).SingleOrDefault();
            if (userVote != null && removeVote != false)
            {
               // Set last change
               SetLastChange(galleryDirectory);

               // remove vote
               entities.DeleteObject(userVote);

               // commit
               entities.SaveChanges();

               // removed
               voteStatus = false;
            }
            else if (userVote == null)
            {
               // Set last change
               SetLastChange(galleryDirectory);

               // add new vote
               entities.Votes.AddObject(new Vote
               {
                  Gallery = galleryDirectory.Name,
                  Image = galleryFile.Name,
                  User = user.Identity.Name,
                  LastUpdate = DateTime.Now,
               });

               // commit
               entities.SaveChanges();

               // added
               voteStatus = true;
            }

            return new GalleryData
                      {
                         FullPath = galleryFile.FullName,
                         Name = galleryFile.Name,
                         UserVote = voteStatus != false, // null or true
                         Gallery = galleryDirectory.Name,
                         VoteCount = setting.StatsEnabled ? votes.Count() : 0,
                         User = user.Identity.Name,
                         TotalPhotos = 1
                      };
         }
         catch (HttpException)
         {
            throw;
         }
         catch (Exception ex)
         {
            throw new HttpException(520, "Unable to save changes. " + ex.Message);
         }
      }

      /// <summary>
      /// Resizes a gallery image
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="imageName"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <exception cref="FileNotFoundException"></exception>
      /// <exception cref="Exception"></exception>
      /// <returns></returns>
      public string Thumbnail(string galleryName, string imageName, int width, int height)
      {
         var galleryFolder = GetGalleryDirectory(galleryName);

         if (imageName.EndsWith(".rnd"))
         {
            // get a random image
            var randomImage = galleryFolder.EnumerateImages().GetRandomElement();
            if (randomImage == null)
            {
               return null;
            }

            imageName = randomImage.Name;
         }

         var thumbnailFile = GetGalleryFile(galleryName, "thumb\\" + imageName);

         if (!thumbnailFile.Exists)
         {
            var src = Path.Combine(galleryFolder.FullName, imageName);
            if (!File.Exists(src))
            {
               throw new FileNotFoundException(src);
            }

            using (var image = Image.FromFile(src))
            {
               using (var resized = image.Resize(width, height, image.Width > image.Height))
               {
                  Directory.CreateDirectory(thumbnailFile.DirectoryName);
                  resized.Save(thumbnailFile.FullName, 75, image.RawFormat);
               }
            }
         }

         return thumbnailFile.FullName;
      }

      /// <summary>
      /// Adds a new image to the gallery
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="fileName"></param>
      /// <param name="user"></param>
      /// <param name="inputStream"></param>
      /// <returns></returns>
      public string UploadImage(string galleryName, string fileName, IPrincipal user, Stream inputStream)
      {
         lock (UploadSyncContext)
         {
            var destinationFile = GetGalleryFile(galleryName, fileName);

            var entities = new DatabaseEntities();
            var uploads = from upload in entities.Uploads
                          where upload.Gallery == galleryName && upload.User == user.Identity.Name
                          select upload;

            var uploadEntity = uploads.Where(upload => upload.Image == fileName).SingleOrDefault();
            if (uploadEntity != null)
            {
               uploadEntity.LastUpdate = DateTime.Now;
               uploadEntity.User = user.Identity.Name;
            }
            else
            {
               if (!IsAdminUser(user))
               {
                  var uploadEnabled = (from set in entities.Settings where set.Gallery == galleryName select set.UploadEnabled).SingleOrDefault();
                  if (uploadEnabled != true)
                  {
                     throw new SecurityException("Not authorized");
                  }

                  if (destinationFile.Exists)
                  {
                     // duplicate file name, not allowed to overwrite other's files
                     throw new DuplicateNameException("A file with same name already exists in this gallery.");
                  }

                  // check upload limit, if any
                  if (UserUploadLimit > 0 && uploads.Count() >= UserUploadLimit)
                  {
                     throw new IndexOutOfRangeException("Upload limit exceeded.");
                  }
               }

               // add new upload record
               uploadEntity = new Upload
               {
                  Gallery = galleryName,
                  Image = fileName,
                  User = user.Identity.Name,
                  LastUpdate = DateTime.Now
               };

               entities.Uploads.AddObject(uploadEntity);
            }

            var success = false;

            try
            {
               // remove thumbnail
               var thumbnailFile = GetGalleryFile(galleryName, "thumb\\" + fileName);
               if (thumbnailFile.Exists)
               {
                  thumbnailFile.Delete();
               }

               var resizeTo = GetMaxImageSize();

               using (var image = Image.FromStream(inputStream))
               {
                  var title = image.GetImageTitle();
                  if (string.IsNullOrWhiteSpace(title))
                  {
                     title = Path.GetFileNameWithoutExtension(destinationFile.Name);
                  }

                  uploadEntity.Title = title;

                  using (var resized = image.Resize(resizeTo.Width, resizeTo.Height))
                  {
                     // resize and save to file
                     resized.Save(destinationFile.FullName, 100, image.RawFormat);
                  }
               }

               // commit db changes
               entities.SaveChanges();

               // done
               success = true;
            }
            finally
            {
               if (!success)
               {
                  destinationFile.Delete();
               }
            }

            return destinationFile.FullName;
         }
      }

      /// <summary>
      /// Adds a new image to the gallery
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="fileName"></param>
      /// <param name="user"></param>
      /// <returns></returns>
      public void DeleteImage(string galleryName, string fileName, IPrincipal user)
      {
         var entities = new DatabaseEntities();
         var entity = (from upload in entities.Uploads
                       where upload.Gallery == galleryName && upload.Image == fileName
                       select upload).SingleOrDefault();

         if (entity != null)
         {
            if (IsAdminUser(user))
            {
               entities.Uploads.DeleteObject(entity);
            }
            else
            {
               var uploadEnabled = (from set in entities.Settings where set.Gallery == galleryName select set.UploadEnabled).SingleOrDefault();
               if (uploadEnabled != true)
               {
                  throw new SecurityException("Not authorized");
               }

               if (string.Equals(entity.User, user.Identity.Name, StringComparison.OrdinalIgnoreCase))
               {
                  entities.Uploads.DeleteObject(entity);
               }
               else
               {
                  throw new SecurityException("Only the owner can remove the image");
               }
            }
         }
         else if (!IsAdminUser(user))
         {
            throw new SecurityException("Only the owner can remove the image");
         }

         var galleryDirectory = GetGalleryDirectory(galleryName);
         var destinationFilePath = Path.Combine(galleryDirectory.FullName, fileName);

         if (File.Exists(destinationFilePath))
         {
            File.Delete(destinationFilePath);
         }

         try
         {
            var thumbnailFilePath = Path.Combine(galleryDirectory.FullName, "thumb\\" + fileName);
            if (File.Exists(thumbnailFilePath))
            {
               File.Delete(thumbnailFilePath);
            }
         }
         catch
         {
            // 
         }

         // Commit database
         entities.SaveChanges();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="imageName"></param>
      /// <param name="user"></param>
      /// <exception cref="HttpException">If something went wrong</exception>
      /// <returns></returns>
      public GalleryData GetGalleryImage(string galleryName, string imageName, IPrincipal user)
      {
         FileInfo galleryFile;

         var galleryDirectory = GetGalleryDirectory(galleryName);

         try
         {
            galleryFile = galleryDirectory.GetFiles(imageName).First();
         }
         catch (Exception)
         {
            throw new HttpException(512, "The image '" + imageName + "' is not in this gallery");
         }

         try
         {
            var entities = new DatabaseEntities();

            // gets all votes for this image
            var votes = entities.Votes.Where(vote => vote.Gallery == galleryName && vote.Image == imageName);

            // filter by this user
            var userVote = votes.Any(vote => vote.User == user.Identity.Name);
            
            // count all votes
            var voteCount = votes.Count();

            // now get some more details about this image
            var uploads = entities.Uploads.Where(upload => upload.Gallery == galleryName && upload.Image == imageName).SingleOrDefault();

            return new GalleryData
            {
               FullPath = galleryFile.FullName,
               Name = galleryFile.Name,
               Title = uploads != null ? uploads.Title : null,
               UserVote = userVote,
               Gallery = galleryDirectory.Name,
               VoteCount = voteCount,
               Date = galleryFile.LastWriteTimeUtc,
               User = uploads != null ? uploads.User : null,
               PublishDate = galleryFile.CreationTimeUtc,
               TotalPhotos = 1
            };
         }
         catch (Exception ex)
         {
            throw new HttpException(520, "Unable to access vote database " + ex.Message);
         }
      }

      //private Upload AddUploadEntityForImage(string galleryName, string imageName, IPrincipal user = null)
      //{
      //   var entities = new DatabaseEntities();
      //   var galleryFile = GetGalleryFile(galleryName, imageName);

      //   Upload upload = null;

      //   try
      //   {
      //      using (var image = Image.FromFile(galleryFile.FullName))
      //      {
      //         // add new upload record
      //         entities.Uploads.AddObject(upload = new Upload
      //         {
      //            Gallery = galleryName,
      //            Image = galleryFile.Name,
      //            User = user != null ? user.Identity.Name : null,
      //            LastUpdate = DateTime.Now,
      //            Title = image.GetImageTitle() ?? Path.GetFileNameWithoutExtension(galleryFile.Name)
      //         });
      //      }

      //      entities.SaveChanges();
      //   }
      //   catch (OptimisticConcurrencyException)
      //   {
      //      entities.Refresh(RefreshMode.ClientWins, upload);
      //      entities.SaveChanges();
      //   }
         
      //   return upload;
      //}

      public Setting GetGallerySettings(string galleryName)
      {
         var entities = new DatabaseEntities();
         var entity = (from setting in entities.Settings where setting.Gallery == galleryName select setting);

         var result = entity.SingleOrDefault();
         if (result == null)
         {
            result = entities.Settings.CreateObject();
            result.Gallery = galleryName;
            entities.SaveChanges();
         }
         return result;
      }

      public void SaveGallerySettings(Setting setting, IPrincipal user)
      {
         if (!IsAdminUser(user))
         {
            throw new SecurityException("Not authorized.");
         }

         if (setting.EntityState == EntityState.Unchanged)
         {
            return;
         }

         var context = new DatabaseEntities();

         try
         {
            if (setting.EntityState == EntityState.Modified)
            {
               var current = (from settings in context.Settings where settings.Id == setting.Id select settings).SingleOrDefault();
               if (current != null)
               {
                  context.Settings.ApplyCurrentValues(setting);
                  setting = current;
               }
            }
            else
            {
               context.Settings.AddObject(setting);
            }

            context.SaveChanges();
         }
         catch (OptimisticConcurrencyException)
         {
            context.Refresh(RefreshMode.ClientWins, setting);
            context.SaveChanges();
         }

         SetLastChange(GetGalleryDirectory(setting.Gallery));
      }

      /// <summary>
      /// Creates a new gallery
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="user"></param>
      /// <exception cref="SecurityException"></exception>
      /// <exception cref="GalleryAlreadyExistsException"></exception>
      public GalleryData CreateGallery(string galleryName, IPrincipal user)
      {
         if (!IsAdminUser(user))
         {
            throw new SecurityException("Not authorized.");
         }

         var gallery = GetGalleryDirectory(galleryName);
         if (gallery.Exists)
         {
            throw new GalleryAlreadyExistsException();
         }

         gallery.Create();

         return GetGallery(galleryName);
      }

      /// <summary>
      /// Deletes an existing gallery
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="user"></param>
      /// <exception cref="SecurityException"></exception>
      public void RemoveGallery(string galleryName, IPrincipal user)
      {
         if (!IsAdminUser(user))
         {
            throw new SecurityException("Not authorized.");
         }

         var gallery = GetGalleryDirectory(galleryName);
         if (gallery.Exists)
         {
            gallery.Delete(true);
         }
      }

      /// <summary>
      /// Returns the physical path for the specified gallery or all galeries
      /// </summary>
      /// <param name="galleryName"></param>
      /// <returns></returns>
      private DirectoryInfo GetGalleryDirectory(string galleryName = null)
      {
         return string.IsNullOrEmpty(galleryName)
                   ? new DirectoryInfo(Path.Combine(_physicalApplicationPath, "Content"))
                   : new DirectoryInfo(Path.Combine(_physicalApplicationPath, "Content", galleryName));
      }

      /// <summary>
      /// Returns the physical path for the specified gallery file
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="fileName"></param>
      /// <returns></returns>
      private FileInfo GetGalleryFile(string galleryName, string fileName)
      {
         var gallery = GetGalleryDirectory(galleryName);
         var file = Path.Combine(gallery.FullName, fileName);

         return new FileInfo(file);
      }
   }
}