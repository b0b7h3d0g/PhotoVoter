using System.ComponentModel.DataAnnotations;

namespace PhotoVoterMvc.Services.Model
{
   public class ContactData
   {
      [Required]
      public string Name { get; set; }

      [Required, ValidateEmail(ErrorMessage = "Valid email is required.")]
      public string Email { get; set; }

      [Required]
      public string Subject { get; set; }

      [Required, DataType(DataType.MultilineText)]
      public string Message { get; set; }

      // Remote Validator - new in MVC3
      //
      // [Remote("UserNameAvailable", "Users")]
      // [AdditionalMetadata("AdminOnly", true)] - new in MVC3
      // public string UserName { get; set; }
      //
      // ... and on the controller
      // public bool UserNameAvailable(string username) 
      // { 
      //    if(MyRepository.UserNameExists(username)) 
      //    { 
      //        return "false"; 
      //    } 
      //    return "true"; 
      // } 
   }
}