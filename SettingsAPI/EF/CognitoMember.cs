using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SettingsAPI.EF
{
    public class CognitoMember
    {
        [Key]
        public int MappingId { get; set; }

        [Column(TypeName = "nvarchar(200)")]
        public string CognitoId { get; set; }

        public bool Status { get; set; }

        [NotMapped]
        public int? PersonId { get; set; }

        public virtual Person Person { get; set; }

        
        public int MemberId { get; set; }

    

        public virtual Member Member { get; set; }
    }
}