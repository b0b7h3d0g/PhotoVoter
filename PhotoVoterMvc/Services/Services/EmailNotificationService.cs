using System.ComponentModel.Composition;
using System.Configuration;
using System.Net.Mail;

namespace PhotoVoterMvc.Services
{
   [Export(typeof(INotificationService))]
   public class EmailNotificationService : INotificationService
   {
      private string _mailServer;

      public string MailServer
      {
         get { return _mailServer ?? (_mailServer = ConfigurationManager.AppSettings["MailServer"]); }
         set { _mailServer = value; }
      }

      public void Notify(IEmailNotification emailNotification)
      {
         using (var mail = new MailMessage())
         {
            mail.To.Add(emailNotification.To);
            mail.From = new MailAddress(emailNotification.To);
            mail.Subject = emailNotification.Subject;
            mail.Body = emailNotification.Body;
            mail.IsBodyHtml = true;

            try
            {
               var sc = new SmtpClient(MailServer);
               sc.Send(mail);
            }
            catch (SmtpException)
            {
               throw new NotificationException();
            }
         }
      }
   }
}