using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class TokenExpiredException : ValidationException
    {
        public TokenExpiredException() : base(AppMessage.TokenExpired)
        {
        }
    }
}