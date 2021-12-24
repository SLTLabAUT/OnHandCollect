using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Shared.Models
{
    public enum ExportMode
    {
        [Display(Name = "تخته‌ها")]
        Writepads,
        [Display(Name = "نویسنده‌ها")]
        Writers,
        [Display(Name = "نویسه‌های مرجع")]
        GroundTruths
    }
}
