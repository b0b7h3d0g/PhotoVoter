using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Principal;
using PhotoVoterMvc.Extenders;

namespace PhotoVoterMvc.Services.Model
{
   [DataContract]
   public class GalleryData
   {
      public string Title { get; set; }
      public string FullPath { get; set; }
      public string Name { get; set; }
      public bool UserVote { get; set; }
      public string User { get; set; } 
      public string Gallery { get; set; }
      public DateTime Date { get; set; }
      public DateTime PublishDate { get; set; }
      public int VoteCount { get; set; }
      public int UserCount { get; set; }
      public int TotalPhotos { get; set; }

      /// <summary>
      /// Serves as a hash function for a particular type. 
      /// </summary>
      /// <returns>
      /// A hash code for the current <see cref="T:System.Object"/>.
      /// </returns>
      /// <filterpriority>2</filterpriority>
      public override int GetHashCode()
      {
         return (Name != null) ? Name.GetHashCode() : 0;
      }

      /// <summary>
      /// Serves as a hash function for a particular type. 
      /// </summary>
      /// <returns>
      /// A hex hash for the current <see cref="T:System.Object"/>.
      /// </returns>
      /// <filterpriority>2</filterpriority>
      public string GetHash()
      {
         return GetHashCode().ToString("x");
      }
   }
}