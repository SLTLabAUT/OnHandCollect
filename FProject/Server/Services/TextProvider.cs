using FProject.Server.Data;
using FProject.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Services
{
    public class TextProvider
    {
        private static readonly Random rnd = new Random();

        private readonly ApplicationDbContext _context;

        public TextProvider(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Text> GetNewText(string userId, TextType type)
        {
            var usedTextIds = await _context.Writepads
                .Where(w => w.OwnerId == userId)
                .Select(w => w.TextId)
                .ToListAsync();
            var candidateIds = await _context.Text
                .Where(t => t.Type == type &&
                    !usedTextIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync();
            var textId = candidateIds[rnd.Next(candidateIds.Count)];
            var text = await _context.Text.FindAsync(textId);
            return text;
        }
    }
}
