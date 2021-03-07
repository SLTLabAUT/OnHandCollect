using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
        public WritepadStatus Status { get; set; }

        public int TextId { get; set; }
        public Text Text { get; set; }

        public ICollection<DrawingPoint> Points { get; set; }
    }

    public class WritepadWithNumberDTO
    {
        public WritepadDTO Writepad { get; set; }
        public int Number { get; set; }
    }

    public enum PointerType
    {
        [Display(Name = "موس")]
        Mouse,
        [Display(Name = "تاچ‌پد")]
        Touchpad,
        [Display(Name = "قلم")]
        Pen,
        [Display(Name = "لمس")]
        Touch
    }

    public enum WritepadStatus
    {
        [Display(Name = "پیش‌نویس")]
        Editing,
        [Display(Name = "منتظر تأیید")]
        WaitForAcceptance,
        [Display(Name = "تأیید شده")]
        Accepted
    }

    public enum DrawingMode
    {
        Non,
        Draw,
        Erase,
        Move
    }
}
