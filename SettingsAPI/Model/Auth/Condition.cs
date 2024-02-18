using System.Collections.Generic;

namespace SettingsAPI.Model.Auth
{
    public class Condition
    {
        public ConditionOperator Operator { get; set; }
        public IDictionary<ConditionKey, string> KeyPairs { get; set; }
    }
}
