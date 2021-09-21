using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Shared.Models
{
    public class EmailSenderDTO
    {
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string TextDescription { get; set; }
        [Required]
        public IEnumerable<string> Tos { get; set; }
    }
}
