using SettingsAPI.Common;
using SettingsAPI.EF;
using SettingsAPI.Model;
using SettingsAPI.Service.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SettingsAPI.Service
{
    public class FieldAuditService : IFieldAuditService
    {
        public List<FieldAudit> GetUpdateMemberFieldAudits(Member updatedMember, Member existingMember)
        {
            var fieldAudits = new List<FieldAudit>();
            if (!string.Equals(updatedMember.FirstName, existingMember.FirstName))
                fieldAudits.Add(new FieldAudit()
                {
                    FieldName = "FirstName",
                    FromValue = existingMember.FirstName,
                    ToValue = updatedMember.FirstName
                });

            if (!string.Equals(updatedMember.LastName, existingMember.LastName))
                fieldAudits.Add(new FieldAudit()
                {
                    FieldName = "LastName",
                    FromValue = existingMember.LastName,
                    ToValue = updatedMember.LastName
                });

            if (!string.Equals(updatedMember.PostCode, existingMember.PostCode))
            {
                fieldAudits.Add(new FieldAudit()
                {
                    FieldName = "PostCode",
                    FromValue = ObfuscatePostCode(existingMember.PostCode),
                    ToValue = ObfuscatePostCode(updatedMember.PostCode)
                });
            }

            if (updatedMember.DateOfBirth.HasValue &&
                !DateTime.Equals(updatedMember.DateOfBirth, existingMember.DateOfBirth))
                fieldAudits.Add(new FieldAudit()
                {
                    FieldName = "DateOfBirth",
                    FromValue = ObfuscateDOB(existingMember.DateOfBirth),
                    ToValue = ObfuscateDOB(updatedMember.DateOfBirth)
                });

            return fieldAudits;
        }

        public FieldAudit GetUpdateMobileFieldAudit(string updatedMobileNumber, string existingMobileNumber)
        {
            if (!string.Equals(updatedMobileNumber, existingMobileNumber))
            {
                return new FieldAudit()
                {
                    FieldName = "Mobile",
                    FromValue = ObfuscateMobile(existingMobileNumber),
                    ToValue = ObfuscateMobile(updatedMobileNumber)
                };
            }
            return null;
        }

        private string ObfuscatePostCode(string postCode)
        {
            if (string.IsNullOrEmpty(postCode)) return string.Empty;
            if (!Regex.IsMatch(postCode, Constant.PostCodeRegex)) return string.Empty;

            return Regex.Replace(postCode, Constant.PostCodeObfuscateRegex, 
                m => $"**{m.Groups["trailingDigits"]}") ;
        }

        private string ObfuscateMobile(string mobile)
        {
            var regexAusPhone = new Regex(Constant.AustraliaPhoneRegex);
            var regexNZPhone = new Regex(Constant.NewzealandPhoneRegex);

            if (string.IsNullOrEmpty(mobile)) return string.Empty;
            if (!regexAusPhone.IsMatch(mobile) && !regexNZPhone.IsMatch(mobile))
                return string.Empty;

            return Regex.Replace(mobile, Constant.MobileObfuscateRegex,
                m => $"{m.Groups["first2digits"]}****{m.Groups["trailingDigits"]}");
        }

        private string ObfuscateDOB(DateTime? dob)
        {
            if (!dob.HasValue) return string.Empty;
            return $"**/**/{dob.Value.Year}";
        }
    }
}
