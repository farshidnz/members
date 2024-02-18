using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class UnauthorizedException : System.Exception
    {
        public UnauthorizedException() : base(AppMessage.Unauthorized)
        {
        }
    }
}
