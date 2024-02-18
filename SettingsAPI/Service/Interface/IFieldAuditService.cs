using SettingsAPI.EF;
using SettingsAPI.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SettingsAPI.Service.Interface
{
    public interface IFieldAuditService
    {
        List<FieldAudit> GetUpdateMemberFieldAudits(Member updatedMember, Member existingMember);

        FieldAudit GetUpdateMobileFieldAudit(string updatedMobileNumber, string existingMobileNumber);
    }
}
