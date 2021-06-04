using FProject.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool FromAdmin { get; set; }
        [Required]
        public string Text { get; set; }
        public bool IsDeleted { get; set; }

        public int WritepadId { get; set; }
        public Writepad Writepad { get; set; }

        public static explicit operator Comment(CommentDTO model)
        {
            return new Comment
            {
                Id = model.Id,
                CreatedAt = model.CreatedAt,
                Text = model.Text,
                FromAdmin = model.FromAdmin,
                WritepadId = (int)model.WritepadId,
            };
        }

        public static explicit operator CommentDTO(Comment model)
        {
            return new CommentDTO
            {
                Id = model.Id,
                CreatedAt = model.CreatedAt,
                Text = model.Text,
                FromAdmin = model.FromAdmin,
            };
        }
    }
}
