using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SettingsAPI.Error;
using SettingsAPI.Model.Enum;
using SettingsAPI.Service;
using Xunit;

namespace SettingsAPI.Tests.Service
{
    public class TestValidationService
    {
        const string Email = "abctest@cashrewards.com";
        private static ValidationService InitValidationServiceMock()
        {
            var mobileOtpMock = new Mock<IMobileOptService>();

            //Mock otp map with phone

            //Australia country
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+6142111111", "111111", Email)).Returns(true);
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+61 42111111", "111111", Email)).Returns(true);

            //Newzealand country
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+6421111111", "222222", Email)).Returns(true);
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+64 21111111", "222222", Email)).Returns(true);
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+6422111111", "333333", Email)).Returns(true);
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+64 22111111", "333333", Email)).Returns(true);
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+6427111111", "444444", Email)).Returns(true);
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+64 27111111", "444444", Email)).Returns(true);
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+6429111111", "555555", Email)).Returns(true);
            mobileOtpMock.Setup(mobile => mobile.VerifyMobileOtp("+64 29111111", "555555", Email)).Returns(true);

            var awsServiceMock = new Mock<IAwsService>();
            // string[]  bsbList = {"12345678 abc xyz"};

            string[] bsbData = {"123456", "456789"};
            var bsbList = Task.FromResult<IEnumerable<string>>(bsbData);
            awsServiceMock.Setup(x => x.FetchBsbData()).Returns(bsbList);

            var settings = new Settings {MinRedemptionAmount = (decimal) 10.01, MaxRedemptionAmount = 5000};
            var optionsMock = new Mock<IOptions<Settings>>();

            optionsMock.Setup(s => s.Value).Returns(settings);

            var timeService = new Mock<ITimeService>();

            timeService.Setup(t => t.GetCurrentDateTimeToday()).Returns(new DateTime(2020, 6, 29));

            var validationService =
                new ValidationService(mobileOtpMock.Object, awsServiceMock.Object, optionsMock.Object, timeService.Object);

            return validationService;
        }

        [Fact]
        public void TestValidateQueryConditions()
        {
            var validationService = InitValidationServiceMock();

            //Invalid limit param
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<ApiUsed>()));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ApiUsed>()));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                -1, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ApiUsed>()));

            //Invalid offset param
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ApiUsed>()));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, -1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ApiUsed>()));

            //Invalid dateFrom param (not format YYYY-mm-dd)
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10/02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020/10/02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020/10-02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-02/10", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020/02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "10/02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "10-02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));

            //Invalid dateTo param ((not format YYYY-mm-dd)
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10-02", "2020-10/02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10-02", "2020/10/02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10-02", "2020/10-02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10-02", "2020-02/10", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10-02", "2020-02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10-02", "10/02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10-02", "10-02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10-02", "2020/02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));

            //Invalid dateFrom and dateTo param (cause dateFrom > dateTo)
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                0, 0, "2020-10-02", "2020-10-01", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()));

            //Validate order by fail
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiUsed>()
            ));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Date222", It.IsAny<string>(), It.IsAny<ApiUsed>()
            ));


            //Validate sort direction fail
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Date", It.IsAny<string>()
                , ApiUsed.Transaction));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Name", It.IsAny<string>()
                , ApiUsed.Transaction));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Amount", It.IsAny<string>()
                , ApiUsed.Transaction));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Status", "Ascccc"
                , ApiUsed.Transaction));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Date", "Descc"
                , ApiUsed.Transaction));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Name", "abc"
                , ApiUsed.Transaction));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Amount", "xyz"
                , ApiUsed.Transaction));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Status", "AscDesc"
                , ApiUsed.Transaction));


            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Date", It.IsAny<string>()
                , ApiUsed.MemberClickHistory));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Date", "Ascccc"
                , ApiUsed.MemberClickHistory));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Date", "Descc"
                , ApiUsed.MemberClickHistory));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Date", "abc"
                , ApiUsed.MemberClickHistory));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Date", "xyz"
                , ApiUsed.MemberClickHistory));

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditions(
                1, 0, "2020-10-01", "2020-10-02", "Date", "AscDesc"
                , ApiUsed.MemberClickHistory));


            //Valid
            try
            {
                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Date", "Asc"
                    , ApiUsed.Transaction);
                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Name", "Asc"
                    , ApiUsed.Transaction);
                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Amount", "Asc"
                    , ApiUsed.Transaction);
                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Status", "Asc"
                    , ApiUsed.Transaction);
                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Date", "Desc"
                    , ApiUsed.Transaction);
                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Name", "Desc"
                    , ApiUsed.Transaction);
                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Amount", "Desc"
                    , ApiUsed.Transaction);
                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Status", "Desc"
                    , ApiUsed.Transaction);


                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Date", "Asc"
                    , ApiUsed.MemberClickHistory);
                validationService.ValidateQueryConditions(
                    1, 0, "2020-10-01", "2020-10-02", "Date", "Desc"
                    , ApiUsed.MemberClickHistory);
            }
            catch (InvalidQueryConditionException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public void TestQueryConditionsForTotalCount()
        {
            var validationService = InitValidationServiceMock();

            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10/02", It.IsAny<string>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020/10/02", It.IsAny<string>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020/10-02", It.IsAny<string>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-02/10", It.IsAny<string>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-02", It.IsAny<string>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020/02", It.IsAny<string>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "10/02", It.IsAny<string>()));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "10-02", It.IsAny<string>()));

            //Invalid dateTo param ((not format YYYY-mm-dd)
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10-02", "2020-10/02"));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10-02", "2020/10/02"));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10-02", "2020/10-02"));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10-02", "2020-02/10"));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10-02", "2020-02"));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10-02", "10/02"));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10-02", "10-02"));
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10-02", "2020/02"));

            //Invalid dateFrom and dateTo param (cause dateFrom > dateTo)
            Assert.Throws<InvalidQueryConditionException>(() => validationService.ValidateQueryConditionsForTotalCount(
                "2020-10-02", "2020-10-01"));

            //Valid
            try
            {
                validationService.ValidateQueryConditionsForTotalCount(
                    "2020-10-01", "2020-10-02");
            }
            catch (InvalidQueryConditionException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public void TestValidateEmail()
        {
            var validationService = InitValidationServiceMock();
            var emailInvalidList = new[]
            {
                It.IsAny<string>(),
                "@example.com",
                "Joe Smith <email@example.com>",
                "email.example.com",
                "email@example@example.com",
                "#@%^%#$@#$@#.com",
                "email@example.com (Joe Smith)",
                @"much.”more\ unusual”@example.com",
                "very.unusual.”@”.unusual.com@example.com",
                "plainaddress"
            };

            var emailValidList = new[]
            {
                "email@example.com",
                "firstname.lastname@example.com",
                "email@subdomain.example.com",
                "firstname+lastname@example.com",
                "email@123.123.123.123",
                "email@[123.123.123.123]",
                "1234567890@example.com",
                "email@example-one.com",
                "email@example-one.com",
                "email@example.name",
                "email@example.museum",
                "email@example.co.jp",
                "firstname-lastname@example.com",
            };


            //Invalid email

            foreach (var email in emailInvalidList)
            {
                Assert.Throws<InvalidEmailException>(() => validationService.ValidateEmail(email));
            }


            //Valid
            foreach (var email in emailValidList)
            {
                try
                {
                    validationService.ValidateEmail(email);
                }
                catch (InvalidEmailException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Theory]
        [InlineData("Male")]
        [InlineData("Female")]
        [InlineData("Other")]
        [InlineData("Prefer not to say")]
        public void ValidateGenderDescription_ShouldValidated_GivenValidDescription(string description)
        {
            var validationService = InitValidationServiceMock();
            var result = validationService.IsEnumDescriptionValid<Gender>(description);
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("invlid description")]
        public void ValidateGenderDescription_ShouldNotValidated_GivenInValidDescription(string description)
        {
            var validationService = InitValidationServiceMock();
            var result = validationService.IsEnumDescriptionValid<Gender>(description);
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateGender()
        {
            var validationService = InitValidationServiceMock();
            var genderInvalidList = new[]
            {
                "abc", "xyz"
            };

            var genderValidList = new[]
            {
                "Male", "Female", "Other", "Prefer not to say"
            };


            //Invalid email

            foreach (var gender in genderInvalidList)
            {
                Assert.Throws<InvalidGenderException>(() => validationService.ValidateGender(gender));
            }


            //Valid
            foreach (var gender in genderValidList)
            {
                try
                {
                    validationService.ValidateGender(gender);
                }
                catch (InvalidGenderException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateAndParseDateOfBirth()
        {
            var validationService = InitValidationServiceMock();
            var dateOfBirthInvalidList = new[]
            {
                "1993/05/05",
                "1993/05-05",
                "05-05-1993",
                "05/1993",
                "05051993",
                "1993-05/05",
                "05-05",
                "05/05",
                "05/05-1993",
                //Wrong date of birth (> date now)
                "2020-07-01",
                // Date if birth not allow (cause <= 14 ages)
                "2007-06-29",
                "2006-06-30"
            };

            var dateOfBirthValidList = new[]
            {
                "1993-05-05",
                "1994-09-11"
            };

            //Invalid date of birth

            foreach (var dateOfBirth in dateOfBirthInvalidList)
            {
                Assert.Throws<InvalidDateOfBirthException>(() =>
                    validationService.ValidateAndParseDateOfBirth(dateOfBirth));
            }

            //Valid
            foreach (var dateOfBirth in dateOfBirthValidList)
            {
                try
                {
                    validationService.ValidateAndParseDateOfBirth(dateOfBirth);
                }
                catch (InvalidDateOfBirthException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidatePhone()
        {
            var validationService = InitValidationServiceMock();
            var phoneInvalidList = new[]
            {
                /* Phone number is any string*/
                It.IsAny<string>(),
                /* Phone number not start with +61 (Australia country) or +64 (Newzealand country)*/
                "12467888889999",
                "0981234567",

                // Australia phone number invalid

                /* Case: phone number start with +61 (Australia country) but number next is different 4
                 and have white space or not white space between country code and next number
                 */
                "+61111111111",
                "+61 111111111",

                /* Case: phone number start with +61 (Australia country) number next is 4 but digits number (start point at 4)
                 less than 9 (8 digits) and have white space or not white space between country code and next number*/
                "+6141111111",
                "+61 41111111",

                /*Case: phone number start with +61 (Australia country) number next is 4 but digits number (start point at 4)
                 greater than 9 (10 digits) and have white space or not white space between country code and next number*/
                "+614111111111",
                "+61 4111111111",

                //Newzealand phone number invalid

                /* Phone number start with +64 (Newzealand country) but number next is different (21, 22, 27, 29)
                  and have white space or not white space between country code and next number */

                "+6411111111",
                "+64 111111111",

                /* Phone number start with +64 (Newzealand country) number next is 21 but digits number (start point at 21) less than 9 (8 digits)
                 and have white space or not white space between country code and next number */

                "+6121111111",
                "+61 21111111",

                /* Phone number start with +64 (Newzealand country) number next is 21 but digits number (start point at 21) greater than 9 (10 digits)
                 and have white space or not white space between country code and next number */
                "+612111111111",
                "+61 2111111111",

                /* Phone number start with +64 (Newzealand country) number next is 22 but digits number (start point at 22) less than 9 (8 digits)
                and have white space or not white space between country code and next number */

                "+6122111111",
                "+61 22111111",

                /* Phone number start with +64 (Newzealand country) number next is 22 but digits number (start point at 22) greater than 9 (10 digits)
                 and have white space or not white space between country code and next number */
                "+612211111111",
                "+61 2211111111",

                /* Phone number start with +64 (Newzealand country) number next is 27 but digits number (start point at 27) less than 9 (8 digits)
                and have white space or not white space between country code and next number */

                "+6127111111",
                "+61 27111111",

                /* Phone number start with +64 (Newzealand country) number next is 27 but digits number (start point at 27) greater than 9 (10 digits)
                 and have white space or not white space between country code and next number */
                "+612711111111",
                "+61 2711111111",

                /* Phone number start with +64 (Newzealand country) number next is 29 but digits number (start point at 29) less than 9 (8 digits)
                and have white space or not white space between country code and next number */

                "+6129111111",
                "+61 29111111",

                /* Phone number start with +64 (Newzealand country) number next is 29 but digits number (start point at 29) greater than 9 (10 digits)
                 and have white space or not white space between country code and next number */
                "+612911111111",
                "+61 2911111111"
            };
            var phoneValidList = new[]
            {
                /* Australia phone valid */
                "+61411111111",
                "+61 411111111",
                /* Newzealand phone valid */
                "+64211111111",
                "+64 211111111",
                "+64221111111",
                "+64 221111111",
                "+64271111111",
                "+64 271111111",
                "+64291111111",
                "+64 291111111",
                "+64 28987898",
                "+64 212345678",
                "+64 2123456789"
            };

            //Invalid phone number

            foreach (var phone in phoneInvalidList)
            {
                Assert.Throws<InvalidMobileNumberException>(() =>
                    validationService.ValidatePhone(phone));
            }


            //Valid
            foreach (var phone in phoneValidList)
            {
                try
                {
                    validationService.ValidatePhone(phone);
                }
                catch (InvalidMobileNumberException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateOtp()
        {
            var validationService = InitValidationServiceMock();

            //Otp invalid
            Assert.Throws<InvalidMobileOtpException>(() =>
                validationService.ValidateOtp("+6122111111", It.IsAny<string>(), Email));
            Assert.Throws<InvalidMobileOtpException>(() => validationService.ValidateOtp("+6122111111", "123457", Email));
            Assert.Throws<InvalidMobileOtpException>(() => validationService.ValidateOtp("+61 22111111", "123457", Email));

            //Valid
            try
            {
                validationService.ValidateOtp("+6142111111", "111111", Email);
                validationService.ValidateOtp("+61 42111111", "111111", Email);

                validationService.ValidateOtp("+6421111111", "222222", Email);
                validationService.ValidateOtp("+64 21111111", "222222", Email);
                validationService.ValidateOtp("+6422111111", "333333", Email);
                validationService.ValidateOtp("+64 22111111", "333333", Email);
                validationService.ValidateOtp("+6427111111", "444444", Email);
                validationService.ValidateOtp("+64 27111111", "444444", Email);
                validationService.ValidateOtp("+6429111111", "555555", Email);
                validationService.ValidateOtp("+64 29111111", "555555", Email);
            }
            catch (InvalidMobileOtpException ex)
            {
                Assert.True(ex == null);
            }
        }

        [Fact]
        public void TestValidateAccountNumber()
        {
            var validationService = InitValidationServiceMock();
            var accountNumberInvalidList = new[]
            {
                It.IsAny<string>(),
                "abc###(_=*!",
                "12345",
                "12345678910"
            };

            var accountNumberValidList = new[]
            {
                "123456",
                "1234567890",
                "abc1234"
            };


            //Invalid account number

            foreach (var accountNumber in accountNumberInvalidList)
            {
                Assert.Throws<BankAccountValidationException>(() =>
                    validationService.ValidateAccountNumber(accountNumber));
            }


            //Valid
            foreach (var accountNumber in accountNumberValidList)
            {
                try
                {
                    validationService.ValidateAccountNumber(accountNumber);
                }
                catch (BankAccountValidationException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public async Task TestValidateBsb()
        {
            var validationService = InitValidationServiceMock();
            var bsbInvalidList = new[]
            {
                It.IsAny<string>(),
                "45689",
                "890256"
            };

            var bsbValidList = new[]
            {
                "123456", "456789"
            };

            //Invalid bsb

            foreach (var bsb in bsbInvalidList)
            {
                await Assert.ThrowsAsync<BankAccountValidationException>(() =>
                    validationService.ValidateBsb(bsb));
            }


            //Valid
            foreach (var bsb in bsbValidList)
            {
                try
                {
                    await validationService.ValidateBsb(bsb);
                }
                catch (BankAccountValidationException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateAccountName()
        {
            var validationService = InitValidationServiceMock();

            var accountNameInvalidList = new[]
            {
                It.IsAny<string>(),
                string.Empty,
                " ",
                "bac123",
                "bac#$"
            };

            var accountNameValidList = new[]
            {
                "abc",
                "foo bar"
            };

            //Invalid account name

            foreach (var accountName in accountNameInvalidList)
            {
                Assert.Throws<BankAccountValidationException>(() =>
                    validationService.ValidateAccountName(accountName));
            }


            //Valid
            foreach (var accountName in accountNameValidList)
            {
                try
                {
                    validationService.ValidateAccountName(accountName);
                }
                catch (BankAccountValidationException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidatePostCode()
        {
            var validationService = InitValidationServiceMock();

            var postCodeInvalidList = new[]
            {
                "abc1",
                "123a",
                "@111",
                "@abc",
                null,
                string.Empty,
                " "
            };

            var postCodeValidList = new[]
            {
                "1234",
                "5678"
            };

            //Invalid post code

            foreach (var postCode in postCodeInvalidList)
            {
                Assert.Throws<InvalidPostCodeException>(() =>
                    validationService.ValidatePostCode(postCode));
            }


            //Valid
            foreach (var postCode in postCodeValidList)
            {
                try
                {
                    validationService.ValidatePostCode(postCode);
                }
                catch (InvalidPostCodeException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateAmount()
        {
            var validationService = InitValidationServiceMock();

            /* Invalid amount and balance */

            //Case amount < 0
            Assert.Throws<InvalidAmountException>(() => validationService.ValidateAmount(-1, It.IsAny<decimal>()));

            //Case amount < Convert.ToDecimal(MinRedemptionAmount)
            Assert.Throws<InvalidAmountException>(() =>
                validationService.ValidateAmount((decimal) 9.01, It.IsAny<decimal>()));

            //Case amount > Math.Round(balance, 2)
            Assert.Throws<InvalidAmountException>(() =>
                validationService.ValidateAmount((decimal) 12.01, (decimal) 8.51));
            
            //Case amount >= Convert.ToInt32(MaxRedemptionAmount)
            Assert.Throws<InvalidAmountException>(() =>
                validationService.ValidateAmount((decimal) 6000, (decimal) 608.51));

            /* Valid */
            try
            {
                validationService.ValidateAmount((decimal) 15.01, (decimal) 16.05);
                validationService.ValidateAmount((decimal) 100.01, (decimal) 120.05);
                validationService.ValidateAmount((decimal)10.01, (decimal)10.01);
                validationService.ValidateAmount((decimal)10.08, (decimal)10.08);
            }
            catch (InvalidAmountException ex)
            {
                Assert.True(ex == null);
            }
            
        }
        [Fact]
        public void TestValidatePaymentMethod()
        {
            var validationService = InitValidationServiceMock();
            
            var paymentMethodInvalids = new[]
            {
                "abc","xyz","none","all","bank","paypal"
            };
            
            //Valid
            var paymentMethodValids = new[]
            {
                "Bank", "PayPal"
            };

            //Invalid
            foreach (var paymentMethod in paymentMethodInvalids)
            {
                Assert.Throws<InvalidPaymentMethodException>(() =>
                    validationService.ValidatePaymentMethod(paymentMethod));
            }

            //Valid
            foreach (var paymentMethod in paymentMethodValids)
            {
                try
                {
                    validationService.ValidatePaymentMethod(paymentMethod);
                }
                catch (InvalidPaymentMethodException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestChangePassword()
        {
            var validationService = InitValidationServiceMock();

            var passwordInvalids = new[]
            {
                "abc", "xyz", "1234567","        "//8 spaces
            };

            //Valid
            var passwordValids = new[]
            {
                "12345678", "abc123456789", "dumydumydumy"
            };
            //Invalid
            foreach (var password in passwordInvalids)
            {
                Assert.Throws<InvalidPasswordException>(() =>
                    validationService.ValidatePassword(password));
            }

            //Valid
            foreach (var password in passwordValids)
            {
                try
                {
                    validationService.ValidatePassword(password);
                }
                catch (InvalidPasswordException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateUri()
        {
            var validationService = InitValidationServiceMock();
            
            var invalidUriList = new[]
            {
                "abc",
                "http",
                "https",
            };

            var validUriList = new[]
            {
                "http://abc.com",
                "https://abc.com",
                "https://abc.com:3000/fdfds/fdfsd",
                "cashrewards://paypal"
            };
            
            //Invalid
            foreach (var uri in invalidUriList)
            {
                Assert.Throws<InvalidUriException>(() =>
                    validationService.ValidateUri(uri));
            }

            //Valid
            foreach (var uri in validUriList)
            {
                try
                {
                    validationService.ValidateUri(uri);
                }
                catch (InvalidUriException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateEmailVerificationCode()
        {
            var validationService = InitValidationServiceMock();

            var invalidCodes = new[]
            {
                "",
                " ",
                "abc1234",
                It.IsAny<string>()
            };
            var memberId = "1234";
            var random = "fdsfsdfsd$fdsfsdfsd";
            var validCodes = new[]
            {
               $"{memberId}${random}"
            };

            //Invalid
            foreach (var code in invalidCodes)
            {
                Assert.Throws<InvalidEmailVerificationCodeException>(() =>
                    validationService.ValidateEmailVerificationCode(code, out _, out _));
            }

            //Valid
            foreach (var code in validCodes)
            {
                try
                {
                    validationService.ValidateEmailVerificationCode(code, out var id , out var hash);
                    Assert.Equal(memberId, id);
                    Assert.Equal(random, hash);
                }
                catch (InvalidEmailVerificationCodeException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateCheckMobileLinkCode()
        {
            var validationService = InitValidationServiceMock();

            var invalidCodes = new[]
            {
                "",
                " ",
                "&&&",
                "abc$122$dff",
                "adc23$abd$dff",
                "122$adb$dff",
                "12$33"
            };

            var validCodes = new[]
            {
               "121212$343434$fadgdgh1adfj"
            };

            //Invalid
            foreach (var code in invalidCodes)
            {
                Assert.Throws<BadRequestException>(() =>
                    validationService.ValidateCheckMobileLinkCode(code));
            }

            //Valid
            foreach (var code in validCodes)
            {
                try
                {
                    validationService.ValidateCheckMobileLinkCode(code);
                }
                catch (BadRequestException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateName()
        {
            var validationService = InitValidationServiceMock();
            var invalidNames = new[]
            {
                It.IsAny<string>(), null, "", " ",
                "a", // Length <=1
                "123456789123456789123456789123456789123456789123456789123456789" //Length > 50
            };

            var validNames = new[]
            {
                "abc", "null", "abc123"
            };

            //Invalid
            foreach (var name in invalidNames)
            {
                Assert.Throws<InvalidNameException>(() =>
                    validationService.ValidateName(name, name));
            }

            //Valid
            foreach (var name in validNames)
            {
                try
                {
                    validationService.ValidateName(name, name);
                }
                catch (InvalidNameException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateFeedback()
        {
            var validationService = InitValidationServiceMock();
            var invalidFeedbacks = new[]
            {
                It.IsAny<string>(),
                null,
                "",
                " "
            };

            var validFeedbacks = new[]
            {
                "abc", "null"
            };

            //Invalid
            foreach (var feedback in invalidFeedbacks)
            {
                Assert.Throws<InvalidFeedbackException>(() =>
                    validationService.ValidateFeedback(feedback));
            }

            //Valid
            foreach (var feedback in validFeedbacks)
            {
                try
                {
                    validationService.ValidateFeedback(feedback);
                }
                catch (InvalidFeedbackException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateAppVersion()
        {
            var validationService = InitValidationServiceMock();
            var invalidAppVersions = new[]
            {
                It.IsAny<string>(),
                null,
                "",
                " "
            };

            var validAppVersions = new[]
            {
                "abc", "null"
            };

            //Invalid
            foreach (var appVersion in invalidAppVersions)
            {
                Assert.Throws<InvalidAppVersionException>(() =>
                    validationService.ValidateAppVersion(appVersion));
            }

            //Valid
            foreach (var appVersion in validAppVersions)
            {
                try
                {
                    validationService.ValidateAppVersion(appVersion);
                }
                catch (InvalidAppVersionException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateDeviceModel()
        {
            var validationService = InitValidationServiceMock();
            var invalidDeviceModels = new[]
            {
                It.IsAny<string>(),
                null,
                "",
                " "
            };

            var validDeviceModels = new[]
            {
                "abc", "null"
            };

            //Invalid
            foreach (var deviceModel in invalidDeviceModels)
            {
                Assert.Throws<InvalidAppVersionException>(() =>
                    validationService.ValidateAppVersion(deviceModel));
            }

            //Valid
            foreach (var deviceModel in validDeviceModels)
            {
                try
                {
                    validationService.ValidateAppVersion(deviceModel);
                }
                catch (InvalidDeviceModelException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateOperatingSystem()
        {
            var validationService = InitValidationServiceMock();
            var invalidOperatingSystems = new[]
            {
                It.IsAny<string>(),
                null,
                "",
                " "
            };

            var validOperatingSystems = new[]
            {
                "abc", "null"
            };

            //Invalid
            foreach (var operating in invalidOperatingSystems)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    validationService.ValidateOperatingSystem(operating));
            }

            //Valid
            foreach (var operating in validOperatingSystems)
            {
                try
                {
                    validationService.ValidateOperatingSystem(operating);
                }
                catch (InvalidOperationException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }

        [Fact]
        public void TestValidateBuildNumber()
        {
            var validationService = InitValidationServiceMock();
            var invalidBuildNumbers = new[]
            {
                It.IsAny<string>(),
                null,
                "",
                " "
            };

            var validBuildNumbers = new[]
            {
                "abc", "null"
            };

            //Invalid
            foreach (var buildNumber in invalidBuildNumbers)
            {
                Assert.Throws<InvalidBuildNumberException>(() =>
                    validationService.ValidateBuildNumber(buildNumber));
            }

            //Valid
            foreach (var buildNumber in validBuildNumbers)
            {
                try
                {
                    validationService.ValidateBuildNumber(buildNumber);
                }
                catch (InvalidBuildNumberException ex)
                {
                    Assert.True(ex == null);
                }
            }
        }
    }
}
