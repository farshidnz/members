using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidQueryConditionException : ValidationException
    {
        public InvalidQueryConditionException(string field) : base(string.Format(AppMessage.FieldInvalid, field))
        {
        }
    }
}