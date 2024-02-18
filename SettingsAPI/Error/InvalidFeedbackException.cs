using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class InvalidFeedbackException : ValidationException
    {
        public InvalidFeedbackException() : base(string.Format(AppMessage.FieldInvalid,
            "Feedback"))
        {
        }
    }
}