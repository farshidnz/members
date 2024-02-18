namespace SettingsAPI.Model.Dto
{
    public class WelcomeBonusTransaction
    {
        public int TransactionId { get; set; }

        public decimal Amount { get; set; }

        public int TransactionStatus { get; set; }
    }
}