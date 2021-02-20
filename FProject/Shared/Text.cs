using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Shared
{
    public class Text
    {
        public int Id { get; set; }
        public TextType Type { get; set; }
        public string Content { get; set; }
    }

    public enum TextType
    {
        Text,
        WordGroups
    }
}
