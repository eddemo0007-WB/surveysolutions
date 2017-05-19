﻿using System.ComponentModel.DataAnnotations;
using WB.UI.Designer.Resources;
using WB.UI.Shared.Web.DataAnnotations;

namespace WB.UI.Designer.Models
{   
    public class LogonModel
    {
        public bool ShouldShowCaptcha { get; set; } = false;
        public string GoogleRecaptchaSiteKey { get; set; }
        public string HomeUrl { get; set; }

        [Required]
        [Display(Name = "User name", Order = 1)]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password", Order = 2)]
        public string Password { get; set; }

        public bool StaySignedIn { get; set; }
    }
}