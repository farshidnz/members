namespace SettingsAPI.Model.Dto
{
    public class PremiumDto
    {
        public decimal Commission { get; set; }
        public bool IsFlatRate { get; set; }
        public string ClientCommissionString { get; set; }
    }
}