using System.Web.Mvc;
using PhotoVoterMvc.Services;

namespace PhotoVoterMvc.App_Start
{
   public class DependencyConfig
   {
      public static void RegisterDependencyResolvers()
      {
         DependencyResolver.SetResolver(new ControllerServiceResolver());
      }
   }
}