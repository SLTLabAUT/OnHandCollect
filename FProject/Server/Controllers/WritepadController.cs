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
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WritepadController : ControllerBase
    {
        // GET: api/<WritepadController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<WritepadController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<WritepadController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // POST api/<WritepadController>/{id}
        //[HttpPost("{id}")]
        //public async Task<ActionResult<int>> AddDrawingPoints(int id, [FromBody] IEnumerable<DrawingPoint> points)
        //{
        //}

        // DELETE api/<WritepadController>/5
        [HttpDelete("{id}")]
        public void Delete(int id, [FromBody] IEnumerable<int> numbers)
        {
        }
    }
}
