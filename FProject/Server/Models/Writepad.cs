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

        public int TextId { get; set; }
        public Text Text { get; set; }

        public ICollection<DrawingPoint> Points { get; set; }

        public string OwnerId { get; set; }
        public ApplicationUser Owner { get; set; }

        public static explicit operator WritepadDTO(Writepad writepad)
        {
            return new WritepadDTO
            {
                Id = writepad.Id,
                PointerType = writepad.PointerType,
                LastModified = writepad.LastModified,
                TextId = writepad.TextId,
                Text = writepad.Text,
                Points = writepad.Points
            };
        }
    }
}
