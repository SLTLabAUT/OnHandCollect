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
            // get text type
            var textType = newWritepad.Type.ToTextType();
            // fetch text required data
            var allText = _context.Text
                .Where(t => t.Type == textType)
                .Select(t => new { TextId = t.Id, Rarity = t.Rarity, Priority = t.Priority });
            // calculate counts
            // user writepads
            var userWritepadsTextCount = _context.Writepads
                .Where(w => w.OwnerId == userId
                    && w.PointerType == newWritepad.PointerType
                    && w.Hand == newWritepad.Hand
                    && w.Type == newWritepad.Type)
                .GroupBy(w => w.TextId)
                .Select(g => new { g.Key, Count = (int?)g.Count() });
            var maxUserWritepads = Math.Max((await userWritepadsTextCount.MaxAsync(e => e.Count) ?? 0), 1);
            // active writepads
            var activeWritepadsFraction = 1;
            var activeWritepadsTextCount = _context.Writepads
                .Where(w => (w.Status == WritepadStatus.NeedEdit || w.Status == WritepadStatus.Draft)
                    && DateTimeOffset.UtcNow - w.LastModified < TimeSpan.FromDays(7)
                    && w.PointerType == newWritepad.PointerType
                    && w.Hand == newWritepad.Hand
                    && w.Type == newWritepad.Type)
                .GroupBy(w => w.TextId)
                .Select(g => new { g.Key, Count = (int?)g.Count() });
            var maxActiveWritepads = Math.Max((await activeWritepadsTextCount.MaxAsync(e => e.Count) ?? 0) / activeWritepadsFraction, 1);
            // ready writepads
            int readyWritepadsFraction = 2;
            var readyWritepadsTextCount = _context.Writepads
                .Where(w => (w.Status == WritepadStatus.Accepted || w.Status == WritepadStatus.WaitForAcceptance)
                    && w.PointerType == newWritepad.PointerType
                    && w.Hand == newWritepad.Hand
                    && w.Type == newWritepad.Type)
                .GroupBy(w => w.TextId)
                .Select(g => new { g.Key, Count = (int?)g.Count() });
            var maxReadyWritepads = Math.Max((await readyWritepadsTextCount.MaxAsync(e => e.Count) ?? 0) / readyWritepadsFraction, 1);
            // calculate ranks
            var query = from t in allText
                        join a in readyWritepadsTextCount on t.TextId equals a.Key into gj
                        from ta in gj.DefaultIfEmpty()
                        select new { t.TextId, Rank = -((float)(ta.Count ?? 0) / readyWritepadsFraction) / maxReadyWritepads + t.Rarity + t.Priority };
            query = from ta in query
                    join w in activeWritepadsTextCount on ta.TextId equals w.Key into gj
                    from taw in gj.DefaultIfEmpty()
                    select new { ta.TextId, Rank = -((float)(taw.Count ?? 0) / activeWritepadsFraction) / maxActiveWritepads + ta.Rank };
            query = from taw in query
                    join u in userWritepadsTextCount on taw.TextId equals u.Key into gj
                    from tawu in gj.DefaultIfEmpty()
                    select new { taw.TextId, Rank = -(float)(tawu.Count ?? 0) * 5 / maxUserWritepads + taw.Rank };
            // fetch texts
            var ids = await query
                .OrderByDescending(e => e.Rank)
                .Take(newWritepad.Number)
                .Select(e => e.TextId)
                .ToListAsync();
            var texts = await _context.Text
                .Where(t => ids.Contains(t.Id))
                .AsNoTracking()
                .ToListAsync();
            return texts;
        }
    }
}
