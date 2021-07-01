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
using FProject.Shared.Models;
using System.Linq.Expressions;

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

        [HttpGet("CurrentTime")]
        public ActionResult<long> CurrentTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // GET: api/<WritepadController>
        [HttpGet]
        public async Task<WritepadsDTO> BatchGet(int page = 1, bool admin = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(IdentityRoleConstants.Admin);
            var adminMode = admin && isAdmin;

            IQueryable<Writepad> writepadsQuery = _context.Writepads
                .Include(w => w.Text);
            if (adminMode)
            {
                var customOrder = new WritepadStatus[] {
                    WritepadStatus.WaitForAcceptance,
                    WritepadStatus.NeedEdit,
                    WritepadStatus.Draft,
                    WritepadStatus.Accepted
                };
                writepadsQuery = writepadsQuery
                    .Include(w => w.Owner)
                    //.OrderBy(w => Array.IndexOf(customOrder, w.Status)) // Array.IndexOf could not get translated
                    .OrderByCustomOrder(w => w.Status, customOrder)
                    .ThenByDescending(w => w.Id);
            }
            else
            {
                var customOrder = new WritepadStatus[] {
                    WritepadStatus.NeedEdit,
                    WritepadStatus.Draft,
                    WritepadStatus.WaitForAcceptance,
                    WritepadStatus.Accepted
                };
                writepadsQuery = writepadsQuery
                    .Where(w => w.OwnerId == userId)
                    .OrderByCustomOrder(w => w.Status, customOrder)
                    .ThenByDescending(w => w.UserSpecifiedNumber);
            }
            var writepads = await writepadsQuery
                .Skip((page - 1) * 10)
                .Take(10)
                .ToListAsync();

            var allCount = 0;
            if (adminMode)
            {
                allCount = await _context.Writepads
                    .CountAsync();
            }
            else
            {
                allCount = await _context.Writepads
                   .Where(w => w.OwnerId == userId)
                   .CountAsync();
            }

            return new WritepadsDTO { Writepads = writepads.Select(w => adminMode ? Writepad.ToAdminWritepadDTO(w) : (WritepadDTO)w),
                AllCount = allCount };
        }

        // GET api/<WritepadController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> Get(int id, bool withPoints, bool admin = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(IdentityRoleConstants.Admin);

            IQueryable<Writepad> writepadQuery = _context.Writepads
                .Include(w => w.Text);
            if (admin && isAdmin)
            {
                writepadQuery = writepadQuery
                    .Where(w => w.Id == id);
            }
            else
            {
                writepadQuery = writepadQuery
                    .Where(w => w.UserSpecifiedNumber == id && w.OwnerId == userId);
            }
            if (withPoints)
            {
                writepadQuery = writepadQuery
                    .Include(w => w.Points);
            }
            var writepad = await writepadQuery.FirstOrDefaultAsync();
            if (writepad is null)
            {
                return NotFound();
            }
            var writepadDTO = (WritepadDTO)writepad;
            if (withPoints)
            {
                var lastSavedDrawingNumber = await _context.Points
                    .IgnoreQueryFilters()
                    .Where(p => p.WritepadId == writepad.Id)
                    .Select(p => p.Number)
                    .OrderBy(v => v)
                    .LastOrDefaultAsync();
                writepadDTO.LastSavedDrawingNumber = lastSavedDrawingNumber == 0 ? -1 : lastSavedDrawingNumber;

                return LZString.CompressToBase64(JsonSerializer.Serialize(writepadDTO));
            }
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
            var texts = new List<Text>();
            if (newWritepad.Type == WritepadType.Sign)
            {
                var newSignCount = (await _context.Writepads
                    .Where(w => w.OwnerId == userId
                        && w.Type == WritepadType.Sign
                        && w.PointerType == newWritepad.PointerType
                        && w.Hand == newWritepad.Hand)
                    .OrderByDescending(w => w.LastModified)
                    .Take(7)
                    .ToListAsync())
                    .Where(w => DateTimeOffset.UtcNow - w.LastModified < TimeSpan.FromHours(12))
                    .Count();

                if (newSignCount >= 7)
                {
                    return BadRequest(WritepadCreationError.SignNotAllowed);
                }

                newWritepad.Number = Math.Min(7 - newSignCount, newWritepad.Number);
                for (int i = 0; i < newWritepad.Number; i++)
                {
                    texts.Add(new Text());
                }
            }
            else
            {
                texts = await textProvider.GetNewText(userId, newWritepad);
            }

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
                Type = newWritepad.Type,
                Hand = newWritepad.Hand,
                TextId = t.Id == 0 ? null : t.Id,
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
            if (writepad.Type == WritepadType.Sign)
            {
                var lastEditedSign = await _context.Writepads
                    .Where(w => w.OwnerId == userId
                        && w.Type == WritepadType.Sign
                        && w.PointerType == writepad.PointerType
                        && w.Hand == writepad.Hand
                        && w.UserSpecifiedNumber != id)
                    .OrderByDescending(w => w.LastModified)
                    .Skip(6)
                    .FirstOrDefaultAsync();
                if (lastEditedSign is not null && DateTimeOffset.UtcNow - lastEditedSign.LastModified < TimeSpan.FromHours(12))
                {
                    return BadRequest(WritepadEditionError.SignNotAllowed);
                }
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

            var lastPoint = savePointsDTO.NewPoints.LastOrDefault();
            return Ok(new SavePointsResponseDTO {
                LastModified = writepad.LastModified,
                LastSavedDrawingNumber = lastPoint is null ? -1 : lastPoint.Number
            });
        }

        // DELETE api/<WritepadController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, bool admin = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(IdentityRoleConstants.Admin);

            IQueryable<Writepad> writepadQuery = _context.Writepads
                .Include(w => w.Owner)
                .Include(w => w.Text);
            if (isAdmin && admin)
            {
                writepadQuery = writepadQuery
                    .Where(w => w.Id == id);
            }
            else
            {
                writepadQuery = writepadQuery
                    .Where(w => w.UserSpecifiedNumber == id && w.OwnerId == userId);
            }
            var writepad = await writepadQuery.FirstOrDefaultAsync();

            if (writepad is null)
            {
                return NotFound();
            }

            if (writepad.Status == WritepadStatus.Accepted)
            {
                writepad.Owner.AcceptedWordCount -= writepad.Text?.WordCount ?? 1;
            }
            writepad.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT api/<WritepadController>/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, WritepadStatus status, bool admin = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(IdentityRoleConstants.Admin);
            var adminMode = isAdmin && admin;

            Writepad writepad;
            if (adminMode)
            {
                writepad = await _context.Writepads
                    .Where(w => w.Id == id)
                    .Include(w => w.Owner)
                    .Include(w => w.Text)
                    .FirstOrDefaultAsync();
            }
            else
            {
                writepad = await _context.Writepads
                    .Where(w => w.UserSpecifiedNumber == id && w.OwnerId == userId)
                    .FirstOrDefaultAsync();
            }
            
            if (writepad is null)
            {
                return NotFound();
            }
            
            if ((writepad.Status == WritepadStatus.Accepted
                || status == WritepadStatus.Accepted
                || status == WritepadStatus.NeedEdit) && !adminMode)
            {
                return BadRequest();
            }

            if (writepad.LastCheck is not null && status == WritepadStatus.Draft)
            {
                status = WritepadStatus.NeedEdit;
            }

            if (writepad.LastCheck is not null && (status == WritepadStatus.Accepted || status == WritepadStatus.NeedEdit))
            {
                writepad.LastCheck = DateTimeOffset.UtcNow;
            }

            if (status == WritepadStatus.Accepted && writepad.Status != WritepadStatus.Accepted)
            {
                writepad.Owner.AcceptedWordCount += writepad.Text?.WordCount ?? 1;
            }
            else if (status != WritepadStatus.Accepted && writepad.Status == WritepadStatus.Accepted)
            {
                writepad.Owner.AcceptedWordCount -= writepad.Text?.WordCount ?? 1;
            }

            writepad.Status = status;
            await _context.SaveChangesAsync();

            return Ok(status);
        }
    }
}
