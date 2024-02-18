using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SettingsAPI.EF
{
    public class EntityAudit
    {
        [Key]
        public int AuditId { get; set; }

        public int EntityId { get; set; }
        public string EntityType { get; set; }
        public int UserId { get; set; }
        public DateTime DateCreated { get; set; }
        public string Comment { get; set; }
        public string FieldsAffected { get; set; }
    }
}
