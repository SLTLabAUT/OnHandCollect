using FProject.Shared;
using FProject.Shared.Models;
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
        public DateTimeOffset? LastCheck { get; set; }
        public WritepadStatus Status { get; set; }
        public WritepadType Type { get; set; }
        public Hand Hand { get; set; }
        public int UserSpecifiedNumber { get; set; }
        public WritepadCommentsStatus CommentsStatus { get; set; }

        public int? TextId { get; set; }
        public Text Text { get; set; }

        public ICollection<DrawingPoint> Points { get; set; }
        public ICollection<Comment> Comments { get; set; }

        public string OwnerId { get; set; }
        public ApplicationUser Owner { get; set; }

        public WritepadDTO ToAdminWritepadDTO()
        {
            return new WritepadDTO
            {
                Id = Id,
                SpecifiedNumber = UserSpecifiedNumber,
                PointerType = PointerType,
                Type = Type,
                Hand = Hand,
                LastModified = LastModified,
                Status = Status,
                TextId = TextId,
                Text = Text,
                Points = Points,
                Owner = (UserDTO) Owner,
                CommentsStatus = CommentsStatus,
            };
        }

        public static explicit operator WritepadDTO(Writepad writepad)
        {
            return new WritepadDTO
            {
                SpecifiedNumber = writepad.UserSpecifiedNumber,
                PointerType = writepad.PointerType,
                Type = writepad.Type,
                Hand = writepad.Hand,
                LastModified = writepad.LastModified,
                Status = writepad.Status,
                TextId = writepad.TextId,
                Text = writepad.Text,
                Points = writepad.Points,
                CommentsStatus = writepad.CommentsStatus,
            };
        }
    }
}
