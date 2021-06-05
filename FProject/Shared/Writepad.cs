using FProject.Shared.Models;
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
        public int Id { get; set; }
        public PointerType PointerType { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public WritepadStatus Status { get; set; }
        public WritepadType Type { get; set; }
        public Hand Hand { get; set; }
        public int SpecifiedNumber { get; set; }
        public WritepadCommentsStatus CommentsStatus { get; set; }

        public int? TextId { get; set; }
        public Text Text { get; set; }

        public UserDTO Owner { get; set; }

        public ICollection<DrawingPoint> Points { get; set; }
        public ICollection<CommentDTO> Comments { get; set; }
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
        public WritepadType Type { get; set; }
        [Display(Name = "نوع دست")]
        public Hand Hand { get; set; }
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
        [Display(Name = "قلم دیجیتالی")]
        Pen,
        [Display(Name = "لمس")]
        Touch,
        [Display(Name = "قلم لمسی")]
        TouchPen
    }

    public enum WritepadStatus
    {
        [Display(Name = "پیش‌نویس")]
        Draft,
        [Display(Name = "منتظر تأیید")]
        WaitForAcceptance,
        [Display(Name = "تأیید شده")]
        Accepted,
        [Display(Name = "نیازمند اصلاح")]
        NeedEdit
    }

    public enum WritepadType
    {
        [Display(Name = "متن")]
        Text,
        [Display(Name = "گروه کلمات")]
        WordGroup,
        [Display(Name = "امضاء")]
        Sign
    }

    public enum Hand
    {
        [Display(Name = "دست راست")]
        Right,
        [Display(Name = "دست چپ")]
        Left,
    }

    public enum WritepadCommentsStatus
    {
        None,
        NewFromUser,
        NewFromAdmin,
    }

    public enum DrawingMode
    {
        Non,
        Draw,
        Erase,
        Move
    }

    public enum WritepadCreationError
    {
        NoReason,
        SignNotAllowed,
    }

    public enum WritepadEditionError
    {
        NoReason,
        SignNotAllowed
    }
}
