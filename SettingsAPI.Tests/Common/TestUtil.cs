using System;
using Xunit;
using SettingsAPI.Common;

namespace SettingsAPI.Tests.Common
{
    public class TestUtil
    {
        public TestUtil()
        {
        }

        [Fact]
        public void TestSanitizeMobilePhone()
        {
            Assert.Equal("+61 436798098", Util.SanitizeMobilePhone("+61436798098"));
            Assert.Equal("+61 436798098", Util.SanitizeMobilePhone("+61 436798098"));
            Assert.Equal("+61 436798098", Util.SanitizeMobilePhone("+610436798098"));
            Assert.Equal("+64 226798098", Util.SanitizeMobilePhone("+64226798098"));
            Assert.Equal("+64 226798098", Util.SanitizeMobilePhone("+64 226798098"));
            Assert.Equal("+64 226798098", Util.SanitizeMobilePhone("+64 226 798 098"));
            Assert.Equal("+64 226798098", Util.SanitizeMobilePhone("+64 0226 798 098"));
        }

        [Fact]
        public void TestToMaskedMobileNumber()
        {
            Assert.Equal("+61 *** *** 098", Util.ToMaskedMobileNumber("+61436798098"));
            Assert.Equal("+64 *** *** 321", Util.ToMaskedMobileNumber("+64 226 798 321"));
        }

        [Fact]
        public void TestConvertPhoneToInternationFormatShouldNotChangeAU()
        {
            // Phone with 04 and 02 in the value
            var phone = "+61 404798028";
            Assert.Equal(phone, Util.ConvertPhoneToInternationFormat(phone));
        }

        [Fact]
        public void TestConvertPhoneToInternationFormatShouldNotChangeNZ()
        {
            // Phone with 04 and 02 in the value
            var phone = "+64 504798028";
            Assert.Equal(phone, Util.ConvertPhoneToInternationFormat(phone));
        }

        [Fact]
        public void TestConvertPhoneToInternationFormatStartingWith04()
        {
            var phone1 = "0404798028";
            var expected1 = "+61 404798028";

            Assert.Equal(expected1, Util.ConvertPhoneToInternationFormat(phone1));

            var phone2 = "04    34798028";
            var expected2 = "+61 434798028";

            Assert.Equal(expected2, Util.ConvertPhoneToInternationFormat(phone2));

        }

        [Fact]
        public void TestConvertPhoneToInternationFormatStartingWith02()
        {
            var phone1 = "0204798028";
            var expected1 = "+64 204798028";

            Assert.Equal(expected1, Util.ConvertPhoneToInternationFormat(phone1));

            var phone2 = "02    34798028";
            var expected2 = "+64 234798028";

            Assert.Equal(expected2, Util.ConvertPhoneToInternationFormat(phone2));

        }

        [Fact]
        public void TestToHashedSurveyEmail()
        {
            Assert.Equal("15d59058b65acdf568a7b5bf85e2091ea0c226c16f0a530d22e81501c19ae071", Util.ToHashedSurveyEmail("sample@gmail.com", "xyz"));
            Assert.Equal("842d0a984681b30dbd0652bd747c6b1e7d1fbcf7f9993e2cec1cec88e819746d", Util.ToHashedSurveyEmail("hashedemail@test.com", "xyz"));
            Assert.Throws<Exception>(() => Util.ToHashedSurveyEmail(null, "xyz"));
            Assert.Throws<Exception>(() => Util.ToHashedSurveyEmail(null, null));
            Assert.Throws<Exception>(() => Util.ToHashedSurveyEmail("abc@gmail.com", null));
        }
    }
}
