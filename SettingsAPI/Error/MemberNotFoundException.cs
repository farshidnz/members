using System;
using SettingsAPI.Common;

namespace SettingsAPI.Error
{
    public class MemberNotFoundException : ValidationException
    {
        public MemberNotFoundException() : base(string.Format(AppMessage.ObjectNotFound, "Member"))
        {
        }
    }
}