using FProject.Server.Data;
using FProject.Server.Models;
using FProject.Shared;
using FProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FProject.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/<CommentController>
        [HttpGet]
        public async Task<IEnumerable<CommentDTO>> BatchGet(int writepadId, bool admin = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(IdentityRoleConstants.Admin);
            var adminMode = admin && isAdmin;

            IQueryable<Writepad> writepadQuery = _context.Writepads;
            if (adminMode)
            {
                writepadQuery = writepadQuery
                    .Where(w => w.Id == writepadId);
            }
            else
            {
                writepadQuery = writepadQuery
                    .Where(w => w.UserSpecifiedNumber == writepadId
                        && w.OwnerId == userId);
            }
            var writepad = await writepadQuery.FirstOrDefaultAsync();

            if (writepad is null)
            {
                return new CommentDTO[] { };
            }

            var comments = await _context.Comments
                .Where(c => c.WritepadId == writepad.Id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            if (writepad.CommentsStatus != WritepadCommentsStatus.None)
            {
                if (writepad.CommentsStatus == WritepadCommentsStatus.NewFromUser && adminMode)
                {
                    writepad.CommentsStatus = WritepadCommentsStatus.None;
                }
                else if (writepad.CommentsStatus == WritepadCommentsStatus.NewFromAdmin && !adminMode)
                {
                    writepad.CommentsStatus = WritepadCommentsStatus.None;
                }
                await _context.SaveChangesAsync();
            }

            return comments.Select(c => (CommentDTO)c);
        }

        // GET: api/<CommentController>/5
        //[HttpGet("{id}")]
        //public async Task<IActionResult> Get(int id, bool admin = false)
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var isAdmin = User.IsInRole(IdentityRoleConstants.Admin);

        //    IQueryable<Comment> query = _context.Comments
        //        .Where(c => c.Id == id);
        //    if (!admin || !isAdmin)
        //    {
        //        query = query
        //            .Where(c => c.Writepad.OwnerId == userId);
        //    }
        //    var comment = await query.FirstOrDefaultAsync();

        //    if (comment is null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok((CommentDTO)comment);
        //}

        // POST api/<CommentController>
        [HttpPost]
        public async Task<IActionResult> Post(CommentDTO newComment, bool admin = false)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(IdentityRoleConstants.Admin);
            var adminMode = admin && isAdmin;

            var comment = new Comment
            {
                WritepadId = (int)newComment.WritepadId,
                CreatedAt = DateTimeOffset.UtcNow,
                Text = newComment.Text,
            };

            IQueryable<Writepad> writepadQuery = _context.Writepads;
            if (adminMode)
            {
                writepadQuery = writepadQuery
                    .Where(w => w.Id == comment.WritepadId);
            }
            else
            {
                writepadQuery = writepadQuery
                    .Where(w => w.UserSpecifiedNumber == comment.WritepadId
                        && w.OwnerId == userId);
            }
            var writepad = await writepadQuery.FirstOrDefaultAsync();

            if (writepad is null)
            {
                return NotFound();
            }

            if (adminMode)
            {
                comment.FromAdmin = true;
                writepad.CommentsStatus = WritepadCommentsStatus.NewFromAdmin;
            }
            else
            {
                comment.WritepadId = writepad.Id;
                writepad.CommentsStatus = WritepadCommentsStatus.NewFromUser;
            }

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(BatchGet), new { writepadId = (int)newComment.WritepadId, admin = admin });
        }

        // DELETE api/<WritepadController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, bool admin = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(IdentityRoleConstants.Admin);

            IQueryable<Comment> query = _context.Comments
                .Where(c => c.Id == id);
            if (!isAdmin || !admin)
            {
                query = query
                    .Where(c => c.Writepad.OwnerId == userId);
            }
            var comment = await query.FirstOrDefaultAsync();

            if (comment is null)
            {
                return NotFound();
            }

            comment.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
