using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SettingsAPI.Service
{
    public interface ILeanplumService
    {
        public Task SetMemberAttribute(Guid leanplumMemberId, string key, bool value);

        public Task SetMemberAttribute(Guid leanplumMemberId, string key, string value);

    }
}
