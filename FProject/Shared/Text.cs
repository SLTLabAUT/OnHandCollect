using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Shared
{
    public class Text
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int WordCount { get; set; }
        public float Rarity { get; set; }
        public float Priority { get; set; }
        public TextType Type { get; set; }
    }

    public enum TextType
    {
        [Display(Name = "متن")]
        Text,
        [Display(Name = "گروه کلمات تکی")]
        WordGroup,
        [Display(Name = "گروه کلمات دوتایی")]
        WordGroup2,
        [Display(Name = "گروه کلمات سه‌تایی")]
        WordGroup3,
        [Display(Name = "گروه ارقام")]
        NumberGroup,
    }
}
