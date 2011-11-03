using System.Web.Mvc;
using PhotoVoterMvc.Extenders;
using PhotoVoterMvc.Services;
using PhotoVoterMvc.Services.Model;

namespace PhotoVoterMvc.Controllers
{
   [HandleError(View = "Error")]
   public class ContactController : Controller
   {
      internal readonly INotificationService NotificationService;

      public ContactController(INotificationService notificationService)
      {
         NotificationService = notificationService;
      }

      [HttpGet]
      public ActionResult Index()
      {
         var model = new ContactData { Email = Session["UserEmailAddress"] as string };
         return Request.IsAjaxRequest() ? (ActionResult)PartialView("Contact", model) : View("Contact", model);
      }

      [HttpPost, ValidateAntiForgeryToken, ValidateInput(true)]
      public ActionResult Submit(ContactData view)
      {
         if (!ModelState.IsValid)
         {
            return Request.IsAjaxRequest() ? (ActionResult)PartialView("_ContactForm", view) : View("Contact", view);
         }

         try
         {
            NotificationService.Notify(new EmailNotification(view));
            return Request.IsAjaxRequest() ? (ActionResult)PartialView("ThanksForFeedback", view) : View("ThanksForFeedback", view);
         }
         catch (NotificationException)
         {
            ModelState.AddModelError("NofifyError", "Could not connect to mail server.");
         }

         return Request.IsAjaxRequest() ? (ActionResult)PartialView("_ContactForm") : View("Contact", view);
      }
   }
}
