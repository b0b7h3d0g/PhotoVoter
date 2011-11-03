using System;
using System.Configuration;
using System.Web;
using PhotoVoterMvc.Models;

namespace PhotoVoterMvc.Services.Model
{
    public class EmailNotification : IEmailNotification
    {
        private readonly ContactData _view;

        public EmailNotification(ContactData view)
        {
            _view = view;
        }

        public string To
        {
            get { return ConfigurationManager.AppSettings["ContactFormTo"]; }
        }

        public string Subject
        {
            get { return _view.Subject; }
        }

        public string Body
        {
            get { return string.Format("IP Address: {5}<br/>Date Sent: {0}<br/>Name: {1}<br/>Email: {2}<br/>Subject: {3}<br/><br/>Comments:<br/>{4}", DateTime.Now, _view.Name, _view.Email, _view.Subject, _view.Message, HttpContext.Current.Request.UserHostAddress); }
        }
    }
}