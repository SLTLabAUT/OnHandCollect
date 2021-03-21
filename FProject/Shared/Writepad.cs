using FProject.Shared.Resources;
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
        public PointerType PointerType { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public WritepadStatus Status { get; set; }
        public TextType Type { get; set; }
        public int UserSpecifiedNumber { get; set; }

        public int TextId { get; set; }
        public Text Text { get; set; }

        public ICollection<DrawingPoint> Points { get; set; }
    }

    public class WritepadsDTO
    {
        public IEnumerable<WritepadDTO> Writepads { get; set; }
        public int AllCount { get; set; }
    }

    public class NewWritepadDTO
    {
        [Display(Name = "نوع ورودی")]
        public PointerType PointerType { get; set; }
        [Display(Name = "نوع داده")]
        public TextType TextType { get; set; }
        [Range(1, 25, ErrorMessageResourceName = "Range", ErrorMessageResourceType = typeof(ErrorMessageResource))]
        [Display(Name = "تعداد")]
        public int Number { get; set; } = 1;
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
