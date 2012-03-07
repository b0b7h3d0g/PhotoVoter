using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PhotoVoterMvc.Services.Model
{
   [DataContract]
   public class GalleryData
   {
      public string Name { get; set; }
      public string Title { get; set; }
      public string FullPath { get; set; }
      public DateTime Date { get; set; }
      public DateTime PublishDate { get; set; }
      public int TotalVotes { get; set; }
      public int TotalUsers { get; set; }
      public IEnumerable<GalleryItem> Photos { get; set; } 
   }

   [DataContract]
   public class GalleryItem
   {
      public string Name { get; set; }
      public string Title { get; set; }
      public string FullPath { get; set; }
      public bool UserVote { get; set; }
      public string User { get; set; }
      public string Gallery { get; set; }
      public DateTime Date { get; set; }
      public DateTime PublishDate { get; set; }
      public int TotalVotes { get; set; }

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