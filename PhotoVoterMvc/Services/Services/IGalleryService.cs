using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Web;
using PhotoVoterMvc.Models;
using PhotoVoterMvc.Services.Exceptions;
using PhotoVoterMvc.Services.Model;

namespace PhotoVoterMvc.Services
{
   public interface IGalleryService
   {
      bool IsAdminUser(IPrincipal user);
      DateTime GetLastChange(string galleryName = null);

      /// <summary>
      /// 
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="user"></param>
      /// <param name="filter"></param>
      /// <param name="sort"></param>
      /// <returns></returns>
      IEnumerable<GalleryData> GetGalleryObjects(string galleryName, IPrincipal user, string filter, string sort);

      /// <summary>
      /// 
      /// </summary>
      /// <param name="top"></param>
      /// <returns></returns>
      IEnumerable<GalleryData> GetGalleries(int top);

      /// <summary>
      /// 
      /// </summary>
      /// <param name="galleryName"></param>
      /// <returns></returns>
      GalleryData GetGallery(string galleryName);

      /// <summary>
      /// 
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="imageName"></param>
      /// <param name="user"></param>
      /// <param name="removeVote"></param>
      /// <exception cref="HttpException">If something went wrong</exception>
      /// <returns></returns>
      GalleryData Vote(string galleryName, string imageName, IPrincipal user, bool? removeVote = null);

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
      string Thumbnail(string galleryName, string imageName, int width, int height);

      /// <summary>
      /// Adds a new image to the gallery
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="fileName"></param>
      /// <param name="user"></param>
      /// <param name="inputStream"></param>
      /// <returns></returns>
      string UploadImage(string galleryName, string fileName, IPrincipal user, Stream inputStream);

      /// <summary>
      /// Adds a new image to the gallery
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="fileName"></param>
      /// <param name="user"></param>
      /// <returns></returns>
      void DeleteImage(string galleryName, string fileName, IPrincipal user);

      /// <summary>
      /// 
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="imageName"></param>
      /// <param name="user"></param>
      /// <exception cref="HttpException">If something went wrong</exception>
      /// <returns></returns>
      GalleryData GetGalleryImage(string galleryName, string imageName, IPrincipal user);

      /// <summary>
      /// 
      /// </summary>
      /// <param name="galleryName"></param>
      /// <returns></returns>
      Setting GetGallerySettings(string galleryName);

      /// <summary>
      /// 
      /// </summary>
      /// <param name="setting"></param>
      /// <param name="user"></param>
      void SaveGallerySettings(Setting setting, IPrincipal user);

      /// <summary>
      /// Creates a new gallery
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="user"></param>
      /// <exception cref="SecurityException"></exception>
      /// <exception cref="GalleryAlreadyExistsException"></exception>
      GalleryData CreateGallery(string galleryName, IPrincipal user);

      /// <summary>
      /// Deletes an existing gallery
      /// </summary>
      /// <param name="galleryName"></param>
      /// <param name="user"></param>
      /// <exception cref="SecurityException"></exception>
      void RemoveGallery(string galleryName, IPrincipal user);
   }
}