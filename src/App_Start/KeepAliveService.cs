using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Web.Configuration;

namespace PhotoVoterMvc.App_Start
{
   public static class KeepAliveService
   {
      private const int TimerInterval = 600000;
      private static Timer _timer;
      private static readonly object _sync = new object();

      public static void Start()
      {
         lock (_sync)
         {
            if (_timer != null)
            {
               return;
            }

            var applicationUrl = WebConfigurationManager.AppSettings["KeepAliveUrl"];
            if (string.IsNullOrWhiteSpace(applicationUrl))
            {
               return;
            }

            _timer = new Timer(Callback, applicationUrl, TimerInterval, TimerInterval);
         }
      }

      private static async void Callback(object url)
      {
         try
         {
            var request = WebRequest.CreateHttp((string) url);

            request.KeepAlive = false;
            request.Timeout   = 90000;

            using (var response = await request.GetResponseAsync())
            {
               var cache = response.IsFromCache;
            }
         }
         catch (Exception ex)
         {
            Trace.TraceError("Keep alive: {0}", ex);
         }
      }

      public static void Stop()
      {
         lock (_sync)
         {
            if (_timer == null)
            {
               return;
            }

            _timer.Dispose();
            _timer = null;
         }
      }
   }
}