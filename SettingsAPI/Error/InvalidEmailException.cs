using System;
using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidEmailException : ValidationException
    {
        public InvalidEmailException() : base(string.Format(AppMessage.FieldInvalid, "Email"))
        {
        }
    }
}