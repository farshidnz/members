using NUnit.Framework;
using SettingsAPI.Model.Rest;
using FluentValidation.TestHelper;
using System;
using Newtonsoft.Json;

namespace SettingsAPI.Tests.Models
{
   
    public class ChangePasswordRequestValidatorTests
    {
        
        [Test]
        [TestCase("NewPassword","")]
        [TestCase("NewPassword", "below-8")]
        [TestCase("MobileOtp", "")]
        public void Validator_ShouldThrowException_GivenInvalidData(string prop, string value)
        {
            var validator = new ChangePasswordRequestValidator();

            var model = new ChangePasswordRequest()
            {
                NewPassword = "newpassword",
                MobileOtp = "1234"
            };
            model.GetType().GetProperty(prop).SetValue(model, value);
            
            var result = validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(prop);
        }

        [Test]
        [TestCase("NewPassword", "password-1")]
        [TestCase("MobileOtp", "123456")]
        public void Validator_ShouldNotThrowException_GivenValidData(string prop, string value)
        {
            var validator = new ChangePasswordRequestValidator();

            var model = new ChangePasswordRequest()
            {
                NewPassword = "newpassword",
                MobileOtp = "123456"
            };
            model.GetType().GetProperty(prop).SetValue(model, value);

            var result = validator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
