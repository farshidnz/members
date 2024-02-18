namespace SettingsAPI.Error
{
    public class MemberPaypalException : ValidationException
    {
        public MemberPaypalException(string message) : base(message)
        {
        }
    }
}