using System;
using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidDateOfBirthException : ValidationException
    {
        public InvalidDateOfBirthException(string message) : base(message)
        {
        }
    }
}