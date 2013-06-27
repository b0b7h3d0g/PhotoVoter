using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace PhotoVoterMvc.Services
{
   public class ControllerServiceResolver : IDependencyResolver
   {
      public CompositionContainer Container { get; set; }

      /// <summary>
      /// Gets the default gallery service type from configuration
      /// </summary>
      public Type DefaultGalleryServiceType
      {
         get
         {
            var configType = ConfigurationManager.AppSettings["DefaultGalleryServiceType"];
            return Type.GetType(configType, false, true) ?? typeof (GalleryService);
         }
      }

      public ControllerServiceResolver()
      {         
         // Create MEF catalog based on existing exports and convention model
         var registration = new RegistrationBuilder();

         registration.ForTypesDerivedFrom<Controller>().SetCreationPolicy(CreationPolicy.NonShared).Export();

         //registration.ForTypesMatching(t => t.FullName.StartsWith(Assembly.GetExecutingAssembly().GetName().Name + ".Parts."))
         //   .SetCreationPolicy(CreationPolicy.NonShared)
         //   .ExportInterfaces(x => x.IsPublic);

         //var filteredCatalog = new FilteringCatalog(defaultCatalog,
         //   def => !def.Metadata.ContainsKey("ServiceType") || string.Equals((string)def.Metadata["ServiceType"], "...", StringComparison.OrdinalIgnoreCase));
         
         var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly(), registration);
         var defaults = new CatalogExportProvider(new TypeCatalog(DefaultGalleryServiceType));

         Container = new CompositionContainer(catalog, defaults);
         
         defaults.SourceProvider = Container;

         Container.ComposeParts(this);
      }


      /// <summary>
      /// Resolves singly registered services that support arbitrary object creation.
      /// </summary>
      /// <returns>
      /// The requested service or object.
      /// </returns>
      /// <param name="serviceType">The type of the requested service or object.</param>
      public object GetService(Type serviceType)
      {
         var controller = Container.GetExports(serviceType, null, null).FirstOrDefault();
         return controller != null ? controller.Value : null;
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