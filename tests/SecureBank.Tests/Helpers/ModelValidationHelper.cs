using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SecureBank.Tests.Helpers
{
    public static class ModelValidationHelper
    {
        public static IList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }
    }
}
