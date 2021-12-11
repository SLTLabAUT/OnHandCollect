using FProject.Server.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace FProject.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Stats : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public Stats(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<Shared.Stats> Get()
        {
            var AcceptedWordCounts = await _context.Users
                .Select(u => u.AcceptedWordCount)
                .ToListAsync();

            var stats = new Shared.Stats()
            {
                AcceptedWordCount = AcceptedWordCounts.Sum(),
                Participant150Count = AcceptedWordCounts.Where(c => c >= 150).Count(),
                ParticipantCount = AcceptedWordCounts.Where(c => c >= 1).Count()
            };

            return stats;
        }
    }
}
