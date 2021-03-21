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
        public async Task<WritepadsDTO> BatchGet(int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var writepads = await _context.Writepads
                .Where(w => w.OwnerId == userId)
                .Include(w => w.Text)
                .OrderBy(w => w.Status)
                .ThenByDescending(w => w.UserSpecifiedNumber)
                .Skip((page-1)*10)
                .Take(10)
                .ToListAsync();
            var allCount = await _context.Writepads
               .Where(w => w.OwnerId == userId)
               .CountAsync();
            return new WritepadsDTO { Writepads = writepads.Select(w => (WritepadDTO)w), AllCount = allCount };
        }

        // GET api/<WritepadController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> Get(int id, bool withPoints)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IQueryable<Writepad> writepadQuery = _context.Writepads
                .Where(w => w.UserSpecifiedNumber == id && w.OwnerId == userId)
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
            var writepadDTO = (WritepadDTO)writepad;
            if (withPoints)
                return LZString.CompressToBase64(JsonSerializer.Serialize(writepadDTO));
            else
            {
                //if (withNumber)
                //{
                //    var writepads = await _context.Writepads
                //        .Where(w => w.OwnerId == userId)
                //        .Select(w => w.Id)
                //        .ToListAsync();
                //    var number = writepads.IndexOf(writepadDTO.Id) + 1;
                //    return JsonSerializer.Serialize(new WritepadWithNumberDTO { Writepad = writepadDTO, Number = number });
                //}
                //else
                //{
                return JsonSerializer.Serialize(writepadDTO);
                //}
            }
            //TODO: convert lz to a custom input and output formatter
        }

        // POST api/<WritepadController>
        [HttpPost]
        public async Task<IActionResult> Post(NewWritepadDTO newWritepad, [FromServices] TextProvider textProvider)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var texts = await textProvider.GetNewText(userId, newWritepad);

            var lastUserSpecifiedId = 0;
            try
            {
                lastUserSpecifiedId = await _context.Writepads
                .Where(w => w.OwnerId == userId)
                .MaxAsync(w => w.UserSpecifiedNumber);
            }
            catch (InvalidOperationException) { }

            var newWritepads = texts.Select(t => new Writepad
            {
                UserSpecifiedNumber = ++lastUserSpecifiedId,
                PointerType = newWritepad.PointerType,
                LastModified = DateTimeOffset.UtcNow,
                Type = newWritepad.TextType,
                TextId = t.Id,
                OwnerId = userId
            }).ToList();
            _context.Writepads.AddRange(newWritepads);
            await _context.SaveChangesAsync();

            for (int i = 0; i < newWritepad.Number; i++)
            {
                newWritepads[i].Text = texts[i];
            }
            return Ok(newWritepads.Select(w => (WritepadDTO)w));
        }

        // POST api/<WritepadController>/{id}
        [HttpPost("{id}")]
        public async Task<IActionResult> SavePoints(int id, [FromBody] string savePointsDTOCompressedJson)
        {
            //var savePointsDTO = JsonSerializer.Deserialize<SavePointsDTO>(savePointsDTOCompressedJson);
            var savePointsDTO = JsonSerializer.Deserialize<SavePointsRequestDTO>(LZString.DecompressFromBase64(savePointsDTOCompressedJson));
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var writepad = await _context.Writepads
                .Where(w => w.UserSpecifiedNumber == id && w.OwnerId == userId).FirstOrDefaultAsync();
            if (writepad is null)
            {
                return NotFound();
            }
            else if (writepad.Status == WritepadStatus.Accepted || (writepad.LastModified - savePointsDTO.LastModified) > TimeSpan.FromMilliseconds(1))
            {
                return BadRequest();
            }

            foreach (var p in savePointsDTO.NewPoints) p.WritepadId = writepad.Id;
            _context.Points.AddRange(savePointsDTO.NewPoints);

            if (!savePointsDTO.DeletedDrawings.IsNullOrEmpty()) {
                foreach (var deletedDrawing in savePointsDTO.DeletedDrawings)
                {
                    for (int i = deletedDrawing.StartingNumber; i <= deletedDrawing.EndingNumber; i++)
                    {
                        var point = new DrawingPoint
                        {
                            WritepadId = writepad.Id,
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

            return Ok(new SavePointsResponseDTO { LastModified = writepad.LastModified });
        }

        // DELETE api/<WritepadController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var writepad = await _context.Writepads
                .Where(w => w.UserSpecifiedNumber == id && w.OwnerId == userId)
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
                .Where(w => w.UserSpecifiedNumber == id && w.OwnerId == userId)
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
