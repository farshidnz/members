using System;
using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidGenderException : ValidationException
    {
        public InvalidGenderException() : base(string.Format(AppMessage.FieldInvalid, "Gender"))
        {
        }
    }
}