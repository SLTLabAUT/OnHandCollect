using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Shared
{
    public class WritepadDTO
    {
        public int Id { get; set; }
        public PointerType PointerType { get; set; }
        public DateTimeOffset LastModified { get; set; }

        public int TextId { get; set; }
        public Text Text { get; set; }

        public ICollection<DrawingPoint> Points { get; set; }
    }

    public enum PointerType
    {
        Mouse,
        Touchpad,
        Pen,
        Touch
    }
}
