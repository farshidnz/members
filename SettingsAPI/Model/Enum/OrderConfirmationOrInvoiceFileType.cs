using System.ComponentModel;
using System.Runtime.Serialization;

namespace SettingsAPI.Model.Enum
{
    public enum OrderConfirmationOrInvoiceFileType
    {
        //Image type
        [Description("image/jpeg")] [EnumMember] Jpg,
        [Description("image/png")] [EnumMember] Png,
        [Description("image/gif")] [EnumMember] Gif,
        [Description("image/tiff")] [EnumMember] Tiff,
        //Document type
        [Description("application/pdf")] [EnumMember] Pdf,
        [Description("application/vnd.ms-excel")] [EnumMember] Xls,
        [Description("application/msword")] [EnumMember] Doc,
        [Description("application/vnd.openxmlformats-officedocument.wordprocessingml.document")] [EnumMember] Docx,
        [Description("application/vnd.ms-xpsdocument")] [EnumMember] Xps
    }
}