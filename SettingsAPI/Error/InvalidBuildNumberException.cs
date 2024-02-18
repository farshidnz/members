using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidBuildNumberException : ValidationException
    {
        public InvalidBuildNumberException() : base(string.Format(AppMessage.FieldInvalid,
            "Build number"))
        {
        }
    }
}