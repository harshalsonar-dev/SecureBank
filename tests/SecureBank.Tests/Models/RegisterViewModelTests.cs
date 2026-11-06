using System.Linq;
using SecureBank.Models;
using SecureBank.Tests.Helpers;
using Xunit;

namespace SecureBank.Tests.Models
{
    public class RegisterViewModelTests
    {
        private static RegisterViewModel CreateValidModel()
        {
            return new RegisterViewModel
            {
                FullName = "John Doe",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                InitialDeposit = 100.00m
            };
        }

        [Fact]
        public void ValidModel_PassesValidation()
        {
            var model = CreateValidModel();
            var results = ModelValidationHelper.Validate(model);
            Assert.Empty(results);
        }

        #region Username Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Username_WhenNullOrEmpty_FailsValidation(string? username)
        {
            var model = CreateValidModel();
            model.Username = username!;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Username)));
        }

        [Theory]
        [InlineData("a")]
        [InlineData("ab")]
        public void Username_WhenShorterThan3Chars_FailsValidation(string username)
        {
            var model = CreateValidModel();
            model.Username = username;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Username)));
        }

        [Fact]
        public void Username_WhenExceeds50Chars_FailsValidation()
        {
            var model = CreateValidModel();
            model.Username = new string('a', 51);

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Username)));
        }

        [Theory]
        [InlineData("john doe")]    // spaces not allowed
        [InlineData("john@doe")]    // @ not allowed
        [InlineData("john#123")]    // # not allowed
        [InlineData("john$")]       // $ not allowed
        [InlineData("john!")]       // ! not allowed
        public void Username_WithInvalidCharacters_FailsValidation(string username)
        {
            var model = CreateValidModel();
            model.Username = username;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Username)));
        }

        [Theory]
        [InlineData("john_doe")]
        [InlineData("john-doe")]
        [InlineData("johndoe123")]
        [InlineData("JOHNDOE_99")]
        public void Username_WithValidCharacters_PassesValidation(string username)
        {
            var model = CreateValidModel();
            model.Username = username;

            var results = ModelValidationHelper.Validate(model);

            Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Username)));
        }

        #endregion

        #region Email Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Email_WhenNullOrEmpty_FailsValidation(string? email)
        {
            var model = CreateValidModel();
            model.Email = email!;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Email)));
        }

        [Theory]
        [InlineData("plainaddress")]
        [InlineData("@missingusername.com")]
        [InlineData("missingdomain@.com")]
        [InlineData("missingat.com")]
        public void Email_WithInvalidFormat_FailsValidation(string email)
        {
            var model = CreateValidModel();
            model.Email = email;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Email)));
        }

        [Fact]
        public void Email_WhenExceeds100Chars_FailsValidation()
        {
            var model = CreateValidModel();
            model.Email = new string('a', 90) + "@example.com"; // > 100 chars

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Email)));
        }

        #endregion

        #region FullName Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FullName_WhenNullOrEmpty_FailsValidation(string? fullName)
        {
            var model = CreateValidModel();
            model.FullName = fullName!;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.FullName)));
        }

        [Fact]
        public void FullName_WhenSingleChar_FailsValidation()
        {
            var model = CreateValidModel();
            model.FullName = "A";

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.FullName)));
        }

        [Fact]
        public void FullName_WhenExceeds100Chars_FailsValidation()
        {
            var model = CreateValidModel();
            model.FullName = new string('A', 101);

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.FullName)));
        }

        #endregion

        #region Password & ConfirmPassword Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Password_WhenNullOrEmpty_FailsValidation(string? password)
        {
            var model = CreateValidModel();
            model.Password = password!;
            model.ConfirmPassword = password!;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Password)));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1234567")]
        public void Password_WhenShorterThan8Chars_FailsValidation(string password)
        {
            var model = CreateValidModel();
            model.Password = password;
            model.ConfirmPassword = password;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.Password)));
        }

        [Fact]
        public void ConfirmPassword_WhenMismatch_FailsValidation()
        {
            var model = CreateValidModel();
            model.Password = "StrongPass123";
            model.ConfirmPassword = "DifferentPass123";

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
        }

        [Fact]
        public void ConfirmPassword_WhenMatching_PassesValidation()
        {
            var model = CreateValidModel();
            model.Password = "StrongPass123";
            model.ConfirmPassword = "StrongPass123";

            var results = ModelValidationHelper.Validate(model);

            Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
        }

        #endregion

        #region InitialDeposit Tests

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-100)]
        [InlineData(-1)]
        public void InitialDeposit_WhenNegative_FailsValidation(double deposit)
        {
            var model = CreateValidModel();
            model.InitialDeposit = (decimal)deposit;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.InitialDeposit)));
        }

        [Fact]
        public void InitialDeposit_WhenExceedsMillion_FailsValidation()
        {
            var model = CreateValidModel();
            model.InitialDeposit = 1000000.01m;

            var results = ModelValidationHelper.Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.InitialDeposit)));
        }

        [Theory]
        [InlineData(0.00)]
        [InlineData(100.00)]
        [InlineData(50000.50)]
        [InlineData(1000000.00)]
        public void InitialDeposit_WithinValidRange_PassesValidation(double deposit)
        {
            var model = CreateValidModel();
            model.InitialDeposit = (decimal)deposit;

            var results = ModelValidationHelper.Validate(model);

            Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(RegisterViewModel.InitialDeposit)));
        }

        #endregion
    }
}
