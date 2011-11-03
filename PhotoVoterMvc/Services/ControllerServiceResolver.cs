using System;
using System.Collections.Generic;
using System.Web.Mvc;
using PhotoVoterMvc.Controllers;

namespace PhotoVoterMvc.Services
{
   public class ControllerServiceResolver : IDependencyResolver
   {
      /// <summary>
      /// Resolves singly registered services that support arbitrary object creation.
      /// </summary>
      /// <returns>
      /// The requested service or object.
      /// </returns>
      /// <param name="serviceType">The type of the requested service or object.</param>
      public object GetService(Type serviceType)
      {
         if (serviceType == typeof(GalleryController))
         {
            // TODO: Use an IoC framework (MEFContrib.MVC, Unity, NInject)
            return new GalleryController(new GalleryService());
         }
         
         if (serviceType == typeof(ContactController))
         {
            return new ContactController(new EmailNotificationService());
         }

         return null;
      }

      /// <summary>
      /// Resolves multiply registered services.
      /// </summary>
      /// <returns>
      /// The requested services.
      /// </returns>
      /// <param name="serviceType">The type of the requested services.</param>
      public IEnumerable<object> GetServices(Type serviceType)
      {
         return new object[] {};
      }
   }
}