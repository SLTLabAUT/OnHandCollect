using FProject.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Server.Models
{
    public class Writepad
    {
        public int Id { get; set; }
        public bool IsDeleted { get; set; }
        public PointerType PointerType { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public WritepadStatus Status { get; set; }
        public TextType Type { get; set; }
        public int UserSpecifiedNumber { get; set; }

        public int? TextId { get; set; }
        public Text Text { get; set; }

        public ICollection<DrawingPoint> Points { get; set; }

        public string OwnerId { get; set; }
        public ApplicationUser Owner { get; set; }

        public static WritepadDTO ToAdminWritepadDTO(Writepad writepad)
        {
            return new WritepadDTO
            {
                SpecifiedNumber = writepad.Id,
                PointerType = writepad.PointerType,
                Type = writepad.Type,
                LastModified = writepad.LastModified,
                Status = writepad.Status,
                TextId = writepad.TextId,
                Text = writepad.Text,
                Points = writepad.Points
            };
        }

        public static explicit operator WritepadDTO(Writepad writepad)
        {
            return new WritepadDTO
            {
                SpecifiedNumber = writepad.UserSpecifiedNumber,
                PointerType = writepad.PointerType,
                Type = writepad.Type,
                LastModified = writepad.LastModified,
                Status = writepad.Status,
                TextId = writepad.TextId,
                Text = writepad.Text,
                Points = writepad.Points
            };
        }
    }
}
