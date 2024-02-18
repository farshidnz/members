using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SettingsAPI.EF
{
    public class Person
    {
        [Key]
        public int PersonId { get; set; }

        public Guid CognitoId { get; set; }

        public int PremiumStatus { get; set; }

        public virtual IEnumerable<CognitoMember> CognitoMember { get; set; }
    }
}
