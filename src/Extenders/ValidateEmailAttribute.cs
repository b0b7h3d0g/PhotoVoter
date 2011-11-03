using System.ComponentModel.DataAnnotations;

namespace PhotoVoterMvc
{
   public class ValidateEmailAttribute : RegularExpressionAttribute
   {
      public ValidateEmailAttribute()
         : base(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?")
      {
      }

      public override string FormatErrorMessage(string name)
      {
         return "This must be a valid email address";
      }

      protected override ValidationResult IsValid(object value, ValidationContext validationContext)
      {
         if (value != null)
         {
            if (!base.IsValid(value))
            {
               return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
         }

         return ValidationResult.Success;
      }
   }
}