using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;
using DotNetOpenAuth.OpenId.RelyingParty;
using PhotoVoterMvc.Extenders;
using PhotoVoterMvc.Services.Model;

namespace PhotoVoterMvc.Controllers
{
   [HandleError(View = "Error")]
   public class UserController : Controller
   {
      private static readonly OpenIdRelyingParty OpenId = new OpenIdRelyingParty();

      private string RefererUrl
      {
         get { return Request.UrlReferrer != null ? Request.UrlReferrer.PathAndQuery : null; }
      }

      public ActionResult Login(string returnUrl)
      {
         if (string.IsNullOrEmpty(returnUrl))
         {
            returnUrl = RefererUrl;
         }

         // Stage 1: display login form to user
         return View("Login", new LoginData { ReturnUrl = returnUrl });
      }

      public ActionResult Logout(string returnUrl)
      {
         // Signout
         FormsAuthentication.SignOut();

         // Set the last login 
         Session["LastLoginDate"] = DateTime.Now;

         if (string.IsNullOrEmpty(returnUrl))
         {
            returnUrl = RefererUrl;
         }

         if (returnUrl != null)
         {
            return Redirect(returnUrl);
         }

         return new EmptyResult();
      }

      [ValidateInput(false)]
      public ActionResult Authenticate(string provider, string returnUrl)
      {
         var response = OpenId.GetResponse();
         if (response == null)
         {
            // Stage 2: user submitting Identifier
            Identifier openIdIdentifier = null;

            if (string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase))
            {
               openIdIdentifier = WellKnownProviders.Google;
            }
            else if (string.Equals(provider, "Yahoo", StringComparison.OrdinalIgnoreCase))
            {
               openIdIdentifier = WellKnownProviders.Yahoo;
            }
            else if (string.Equals(provider, "MyOpenId", StringComparison.OrdinalIgnoreCase))
            {
               openIdIdentifier = WellKnownProviders.MyOpenId;
            } 
            else if (string.Equals(provider, "Steam", StringComparison.OrdinalIgnoreCase))
            {
               openIdIdentifier = Identifier.Parse("http://steamcommunity.com/openid");
            }
            else if (string.Equals(provider, "Verisign", StringComparison.OrdinalIgnoreCase))
            {
               openIdIdentifier = WellKnownProviders.Verisign;
            }
            else
            {
               ModelState.AddModelError("Error", "Invalid identifier");
               return View("Login", new LoginData { ReturnUrl = returnUrl });
            }

            try
            {
               var request = OpenId.CreateRequest(openIdIdentifier);
               
               // Add the AX request that says Email address is required.
               request.AddExtension(new FetchRequest { Attributes = { new AttributeRequest(WellKnownAttributes.Contact.Email, true) } });
               
               return request.RedirectingResponse.AsActionResult();
            }
            catch (ProtocolException ex)
            {
               ModelState.AddModelError("Error", ex.Message);
               return View("Login", new LoginData { ReturnUrl = returnUrl });
            }
         }

         // Stage 3: OpenID Provider sending assertion response
         switch (response.Status)
         {
            case AuthenticationStatus.Authenticated:
               var fetch = response.GetExtension<FetchResponse>();
               var emailAddress = fetch.GetAttributeValue(WellKnownAttributes.Contact.Email);

               // Set new authorization cookie
               FormsAuthentication.SetAuthCookie(emailAddress ?? response.ClaimedIdentifier, true);

               // Save the user's email address
               Session["UserEmailAddress"] = emailAddress;
               
               // Set the last login date
               Session["LastLoginDate"] = DateTime.Now;

               // Redirect to returnUrl or the default page
               return Redirect(!string.IsNullOrEmpty(returnUrl) ? returnUrl : "~/");

            case AuthenticationStatus.Canceled:
               ModelState.AddModelError("Error", "Canceled at provider.");
               return View("Login", new LoginData { ReturnUrl = returnUrl });

            case AuthenticationStatus.Failed:
               ModelState.AddModelError("Error", response.Exception.Message);
               return View("Login", new LoginData { ReturnUrl = returnUrl });

            default:
               throw new ApplicationException("Validation error, OpenID: " + response.Status);
         }
      }
   }
}
