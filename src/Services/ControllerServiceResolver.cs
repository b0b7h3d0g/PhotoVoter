using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using MefContrib.Hosting.Conventions;
using MefContrib.Hosting.Conventions.Configuration;

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
         // Create MEF catalog based on the contents of ~/bin.
         //
         // In latest MEF (.NET 4.5) it will be posibile to use convention model
         // var registration = new RegistrationBuilder();
         // registration.Implements<IController>().Export<IController>();
         // var catalog = new AssemblyCatalog(typeof(Program).Assembly, registration);
         var controllersRegistry = new PartRegistry();
         
         controllersRegistry.Scan(scan => 
         {
            scan.Assembly(Assembly.GetExecutingAssembly());
            scan.Directory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin"));
         });

         controllersRegistry
            .Part()
            .ForTypesAssignableFrom<IController>()
            .MakeNonShared()
            .ExportAs<IController>()
            .Export()
            .ImportConstructor();

         var conventionCatalog = new ConventionCatalog(controllersRegistry);
         var defaultCatalog = new DirectoryCatalog("bin");

         //var filteredCatalog = new FilteringCatalog(defaultCatalog,
         //   def => !def.Metadata.ContainsKey("ServiceType") || string.Equals((string)def.Metadata["ServiceType"], "...", StringComparison.OrdinalIgnoreCase));
         
         var catalog = new AggregateCatalog(defaultCatalog, conventionCatalog);
         
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