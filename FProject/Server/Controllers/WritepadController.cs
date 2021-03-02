using FProject.Server.Data;
using FProject.Server.Models;
using FProject.Server.Services;
using FProject.Shared;
using FProject.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using LZStringCSharp;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FProject.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WritepadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WritepadController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/<WritepadController>
        [HttpGet]
        public async Task<IEnumerable<WritepadDTO>> GetAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var writepads = await _context.Writepads
                .Where(w => w.OwnerId == userId)
                .Include(w => w.Text)
                .ToListAsync();
            return writepads.Select(w => (WritepadDTO)w);
        }

        // GET api/<WritepadController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> Get(int id, bool withPoints)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IQueryable<Writepad> writepadQuery = _context.Writepads
                .Where(w => w.Id == id && w.OwnerId == userId)
                .Include(w => w.Text);
            if (withPoints)
            {
                writepadQuery = writepadQuery.Include(w => w.Points);
            }
            var writepad = await writepadQuery.FirstOrDefaultAsync();
            if (writepad is null)
            {
                return NotFound();
            }
            if (withPoints)
                return LZString.CompressToBase64(JsonSerializer.Serialize((WritepadDTO)writepad));
            else
                return JsonSerializer.Serialize((WritepadDTO)writepad);
            //TODO: convert lz to a custom input and output formatter
        }

        // POST api/<WritepadController>
        [HttpPost]
        public async Task<IActionResult> Post(PointerType pointerType, TextType textType, [FromServices] TextProvider textProvider)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var text = await textProvider.GetNewText(userId, textType);
            var newWritepad = new Writepad
            {
                PointerType = pointerType,
                LastModified = DateTimeOffset.UtcNow,
                TextId = text.Id,
                OwnerId = userId
            };
            _context.Writepads.Add(newWritepad);
            await _context.SaveChangesAsync();

            newWritepad.Text = text;
            return CreatedAtAction(nameof(Get), new { id = newWritepad.Id }, (WritepadDTO)newWritepad);
        }

        // POST api/<WritepadController>/{id}
        [HttpPost("{id}")]
        public async Task<IActionResult> SavePoints(int id, [FromBody] string savePointsDTOCompressedJson)
        {
            //var savePointsDTO = JsonSerializer.Deserialize<SavePointsDTO>(savePointsDTOCompressedJson);
            var savePointsDTO = JsonSerializer.Deserialize<SavePointsDTO>(LZString.DecompressFromBase64(savePointsDTOCompressedJson));
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var writepad = await _context.Writepads
                .Where(w => w.Id == id && w.OwnerId == userId).FirstOrDefaultAsync();
            if (writepad is null)
            {
                return NotFound();
            }
            else if ((writepad.LastModified - savePointsDTO.LastModified) > TimeSpan.FromMilliseconds(1))
            {
                return BadRequest();
            }

            foreach (var p in savePointsDTO.NewPoints) p.WritepadId = id;
            _context.Points.AddRange(savePointsDTO.NewPoints);

            if (!savePointsDTO.DeletedDrawings.IsNullOrEmpty()) {
                foreach (var deletedDrawing in savePointsDTO.DeletedDrawings)
                {
                    for (int i = deletedDrawing.StartingNumber; i <= deletedDrawing.EndingNumber; i++)
                    {
                        var point = new DrawingPoint
                        {
                            WritepadId = id,
                            Number = i,
                            IsDeleted = true
                        };
                        var entry = _context.Points.Attach(point);
                        entry.Property(p => p.IsDeleted).IsModified = true;
                    }
                }
            }

            writepad.LastModified = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { LastModified = writepad.LastModified });
        }

        // DELETE api/<WritepadController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var writepad = await _context.Writepads
                .Where(w => w.Id == id && w.OwnerId == userId)
                .FirstOrDefaultAsync();
            if (writepad is null)
            {
                return NotFound();
            }

            writepad.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT api/<WritepadController>/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, WritepadStatus status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var writepad = await _context.Writepads
                .Where(w => w.Id == id && w.OwnerId == userId)
                .FirstOrDefaultAsync();
            if (writepad is null)
            {
                return NotFound();
            }
            
            if (writepad.Status != WritepadStatus.Accepted)
            {
                if (status == WritepadStatus.Accepted && !User.IsInRole(IdentityRoleConstants.Admin))
                {
                    return BadRequest();
                }

                writepad.Status = status;
            }
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
