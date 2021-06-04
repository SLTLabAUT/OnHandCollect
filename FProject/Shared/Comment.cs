using FProject.Shared.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Shared
{
    public class CommentDTO
    {
        public int Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool FromAdmin { get; set; }
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [Display(Name = "متن پیام")]
        public string Text { get; set; }

        public int? WritepadId { get; set; }
    }
}
