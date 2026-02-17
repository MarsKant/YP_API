using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YP_API.Data;
using YP_API.Models;

namespace YP_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly RecipePlannerContext _context;
        public UsersController(RecipePlannerContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Выводит список пользователей
        /// </summary>
        [HttpGet("get")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.Where(t=> !t.IsAdmin).ToListAsync();
            return Ok(new
            {
                success = true,
                data = users.Select(r => new {
                    Id = r.Id,
                    Username = r.Username,
                    Password = r.Password,
                    Email = r.Email,
                    IsAdmin = r.IsAdmin
                })
            });
        }
    }
}
