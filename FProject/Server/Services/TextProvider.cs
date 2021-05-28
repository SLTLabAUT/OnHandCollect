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
            int maxUser = 1;
            try
            {
                maxUser = await _context.Writepads
                    .Where(w => w.OwnerId == userId
                        && w.PointerType == newWritepad.PointerType
                        && w.Type == newWritepad.Type)
                    .GroupBy(w => w.TextId)
                    .MaxAsync(e => e.Count());
            }
            catch (InvalidOperationException) {}
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
            var textType = newWritepad.Type.ToTextType();
            var allText = _context.Text
                .Where(t => t.Type == textType)
                .Select(t => new { TextId = t.Id });
            var userTextCount = _context.Writepads
                .Where(w => w.OwnerId == userId
                    && w.PointerType == newWritepad.PointerType
                    && w.Type == newWritepad.Type)
                .GroupBy(w => w.TextId)
                .Select(g => new { g.Key, Count = (float?)g.Count() });
            //var userAllTextCount = _context.Writepads
            //    .Where(w => w.OwnerId == userId)
            //    .GroupBy(w => w.TextId)
            //    .Select(g => new { g.Key, Count = (int?)g.Count() });
            var writepadsAcceptedTextCount = _context.Writepads
                .Where(w => w.Status == WritepadStatus.Accepted
                    && w.Type == newWritepad.Type)
                .GroupBy(w => w.TextId)
                .Select(g => new { g.Key, Count = (float?)g.Count() });
            var firstJoin = from t in allText
                            join w in writepadsAcceptedTextCount on t.TextId equals w.Key into gj
                            from tw in gj.DefaultIfEmpty()
                            select new { TextId = t.TextId, AllAcceptedUseRank = (tw.Count ?? 0f)/maxWritepads };
            //var secondQuery = from tu in firstQuery
            //                 join u in userTextCount on tu.TextId equals u.Key into gj
            //                 from tuu in gj.DefaultIfEmpty()
            //                 select new { TextId = tu.TextId, TextRank = tu.TextRank, UserUseCount = tu.UserUseCount, AllUserUseCount = tuu.Count ?? 0 };
            var secondJoin = from tw in firstJoin
                             join u in userTextCount on tw.TextId equals u.Key into gj
                             from twu in gj.DefaultIfEmpty()
                             select new { TextId = tw.TextId, AllAcceptedUseRank = tw.AllAcceptedUseRank, UserUseRank = (twu.Count ?? 0f)/maxUser };
            var Ids = await secondJoin
                .Select(e => new { TextId = e.TextId, Rank = - e.UserUseRank*2 - e.AllAcceptedUseRank })
                .OrderByDescending(e => e.Rank)
                .Take(newWritepad.Number)
                .Select(e => e.TextId)
                .ToListAsync();
            var texts = await _context.Text
                .Where(t => Ids.Contains(t.Id))
                .ToListAsync();
            return texts;
        }
    }
}
