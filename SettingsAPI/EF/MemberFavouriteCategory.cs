using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SettingsAPI.EF
{
    public class MemberFavouriteCategory
    {
        [Key]
        public int MemberFavouriteCategoryId { get; set; }

        public int MemberId { get; set; }

        public int CategoryId { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}
