namespace SettingsAPI.Error
{
    public class InvalidAmountException : ValidationException
    {
        public InvalidAmountException(string message) : base(message)
        {
        }
        
    }
}