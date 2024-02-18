using SettingsAPI.Data;
using SettingsAPI.EF;
using SettingsAPI.Model;
using SettingsAPI.Model.Enum;
using SettingsAPI.Service.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public class EntityAuditService : IEntityAuditService
    {
        private readonly ShopGoContext _shopGoContext;

        public EntityAuditService(ShopGoContext shopGoContext)
        {
            _shopGoContext = shopGoContext;
        }
        public async Task LogEntityAudit(int entityId, string entityType, AuditActionType type, List<FieldAudit> fieldAudits)
        {
            var audit = GetAuditEntity(entityId, entityType, type, fieldAudits);
            _shopGoContext.EntityAudit.Add(audit);
            await _shopGoContext.SaveChangesAsync();
        }

        private EntityAudit GetAuditEntity(int entityId, string entityType, AuditActionType type, List<FieldAudit> fieldAudits)
        {
            return new EntityAudit()
            {
                Comment = type.ToString(),
                DateCreated = DateTime.Now,
                EntityId = entityId,
                EntityType = entityType,
                FieldsAffected = ConvertFieldsAuditsToJSON(fieldAudits)
            };
        }

        private string ConvertFieldsAuditsToJSON(List<FieldAudit> fieldAudits)
        {
            string json = string.Empty;
            if (fieldAudits.Count > 0)
            {
                var serializer = new DataContractJsonSerializer(typeof(List<FieldAudit>));
                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, fieldAudits);
                    json = Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            return json;
        }

    }
}
