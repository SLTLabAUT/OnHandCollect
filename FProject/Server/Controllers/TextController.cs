using FProject.Server.Data;
using FProject.Server.Models;
using FProject.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FProject.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = IdentityRoleConstants.Admin)]
    public class TextController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TextController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/<TextController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Text>> Get(int id)
        {
            var text = await _context.Text.FindAsync(id);
            return text;
        }

        // POST api/<TextController>
        [HttpPost]
        public async Task<IActionResult> Post(Text value)
        {
            value.Id = 0;
            _context.Text.Add(value);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = value.Id }, value);
        }

        // DELETE api/<TextController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _context.Text.Remove(new Text { Id = id });
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
