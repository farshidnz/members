using SettingsAPI.Model;
using SettingsAPI.Model.Enum;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SettingsAPI.Service.Interface
{
    public interface IEntityAuditService
    {
        public Task LogEntityAudit(int entityId, string entityType, AuditActionType type, List<FieldAudit> fieldAudits);
    }
}
