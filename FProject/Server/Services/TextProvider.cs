using FProject.Server.Data;
using FProject.Shared;
using FProject.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Services
{
    public class TextProvider
    {
        private readonly ApplicationDbContext _context;

        public TextProvider(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Text>> GetNewText(string userId, NewWritepadDTO newWritepad)
        {
            // fetch maximums
            int maxUser = 1;
            try
            {
                maxUser = await _context.Writepads
                    .Where(w => w.OwnerId == userId
                        && w.PointerType == newWritepad.PointerType
                        && w.Hand == newWritepad.Hand
                        && w.Type == newWritepad.Type)
                    .GroupBy(w => w.TextId)
                    .MaxAsync(e => e.Count());
            }
            catch (InvalidOperationException) { }
            int maxWritepads = 1;
            try
            {
                maxWritepads = await _context.Writepads
                    .Where(w => w.Status == WritepadStatus.Accepted
                        && w.Type == newWritepad.Type)
                    .GroupBy(w => w.TextId)
                    .MaxAsync(e => e.Count());
            }
            catch (InvalidOperationException) { }
            int maxAcceptedWritepads = 1;
            try
            {
                maxAcceptedWritepads = await _context.Writepads
                    .Where(w => w.Status == WritepadStatus.Accepted
                        && w.Type == newWritepad.Type)
                    .GroupBy(w => w.TextId)
                    .MaxAsync(e => e.Count());
            }
            catch (InvalidOperationException) { }
            // get text type
            var textType = newWritepad.Type.ToTextType();
            // fetch text required data
            var allText = _context.Text
                .Where(t => t.Type == textType)
                .Select(t => new { TextId = t.Id, Rarity = t.Rarity, Priority = t.Priority });
            // calculate counts
            var userTextCount = _context.Writepads
                .Where(w => w.OwnerId == userId
                    && w.PointerType == newWritepad.PointerType
                    && w.Hand == newWritepad.Hand
                    && w.Type == newWritepad.Type)
                .GroupBy(w => w.TextId)
                .Select(g => new { g.Key, Count = g.Count() });
            var writepadsTextCount = _context.Writepads
                .Where(w => w.Type == newWritepad.Type)
                .GroupBy(w => w.TextId)
                .Select(g => new { g.Key, Count = g.Count() });
            var writepadsAcceptedTextCount = _context.Writepads
                .Where(w => w.Status == WritepadStatus.Accepted
                    && w.Type == newWritepad.Type)
                .GroupBy(w => w.TextId)
                .Select(g => new { g.Key, Count = g.Count() });
            // calculate ranks
            var query = from t in allText
                        join a in writepadsAcceptedTextCount on t.TextId equals a.Key into gj
                        from ta in gj.DefaultIfEmpty()
                        select new { t.TextId, Rank = -(float)(ta.Count / 3) / maxAcceptedWritepads + t.Rarity + t.Priority };
            query = from ta in query
                    join w in writepadsTextCount on ta.TextId equals w.Key into gj
                    from taw in gj.DefaultIfEmpty()
                    select new { ta.TextId, Rank = -(float)(taw.Count / 7) / maxWritepads + ta.Rank };
            query = from taw in query
                    join u in userTextCount on taw.TextId equals u.Key into gj
                    from tawu in gj.DefaultIfEmpty()
                    select new { taw.TextId, Rank = -5 * (float)tawu.Count / maxUser + taw.Rank };
            // fetch texts
            var Ids = await query
                .OrderByDescending(e => e.Rank)
                .Take(newWritepad.Number)
                .Select(e => e.TextId)
                .ToListAsync();
            var texts = await _context.Text
                .Where(t => Ids.Contains(t.Id))
                .AsNoTracking()
                .ToListAsync();
            return texts;
        }
    }
}
