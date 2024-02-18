using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using SaaSRestAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SaaSRestAPI.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly SaaSRestAPIDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        //private readonly IOptions<JwtOptions> _jwtOptions;

        public HomeController(SaaSRestAPIDBContext context, IConfiguration configuration, IDistributedCache cache, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _cache = cache;
            _contextAccessor = contextAccessor;
        }


        [HttpPost("/signup"), AllowAnonymous]
        public JsonResult CreateUser(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return new JsonResult(Ok(user));
        }
        [HttpPost("/auth/login"), AllowAnonymous]
        public JsonResult Login(User user)
        {
            var userInDb = _context.Users.FirstOrDefault(m=>m.UserName == user.UserName && m.Password == user.Password); //<--Na dokimaso na to kano toList()

            if (userInDb == null)
            {
                return new JsonResult(NotFound());
            }
            return new JsonResult(Ok(CreateJWT(user)));
        }
        [HttpPost("/auth/logout")]
        public async Task<JsonResult> LogoutAsync()
        {
            if (!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if (isActiveAsync(GetCurrentAsync()).Result)
            {
                await DeactivateCurrent();
                return new JsonResult(Ok());
            }

            return new JsonResult(BadRequest());
        }
        private async Task DeactivateCurrent() //Prosoxi edo p
        {
            await DeactivateAsync(GetCurrentAsync());
        }
        private async Task<bool> isActiveAsync(string token)
        {
            return await _cache.GetStringAsync(GetKey(token)) == null;
        }
        private async Task DeactivateAsync(string token)
        {
            await _cache.SetStringAsync(GetKey(token), " ", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
            });
        }
        private string GetCurrentAsync()
        {
            var authorizationHeader = _contextAccessor.HttpContext.Request.Headers["Authorization"];

            return authorizationHeader == StringValues.Empty ? 
                string.Empty : authorizationHeader.Single().Split(" ").Last();
        }
        private static string GetKey(string token)
        {
            return $"tokens:{token}:deactivated";
        }




        [HttpGet("/todos")]
        public JsonResult All()
        {
            if(!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if(UserName() == null)
            {
                return new JsonResult(NotFound());
            }

            var user = _context.Users.FirstOrDefault(m=>m.UserName == UserName());

            var todosInDb = _context.Todos.Include(r=>r.Items).Where(m=>m.CreatedBy == user.UserId); //<--Na dokimaso na to kano toList()

            if (todosInDb == null)
            {
                return new JsonResult(NotFound());
            }

            foreach (var todo in todosInDb)
            {
                todo.CreatedByNavigation = null;
                foreach(var item in todo.Items)
                {
                    item.BelongsToNavigation = null;
                }
            }

            return new JsonResult(Ok(todosInDb)); //JsonResult(todoInDb);
        }
        [HttpPost("/todos")]
        public JsonResult Create( Todo todo )
        {
            if (!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if (UserName() == null)
            {
                return new JsonResult(NotFound());
            }
            var user = _context.Users.FirstOrDefault(m => m.UserName == UserName());

            if(todo.CreatedBy != user.UserId)
            {
                todo.CreatedBy = user.UserId;
                //return new JsonResult(BadRequest("Field 'createdBy' does not contain your Id!"));
            }

            _context.Todos.Add( todo );
            _context.SaveChanges();
            todo.CreatedByNavigation = null;
            return new JsonResult(Ok(todo));
        }
        [HttpGet("/todos/{id}")]
        public JsonResult Read(int id)//(string name)
        {
            if (!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if (UserName() == null)
            {
                return new JsonResult(NotFound());
            }
            var user = _context.Users.FirstOrDefault(m => m.UserName == UserName());

            var todoInDb = _context.Todos.Include(r=>r.Items).FirstOrDefault(m => m.TodoId == id); //TodoName == name);

            if( todoInDb == null)
            {
                return new JsonResult(NotFound());
            }

            if( todoInDb.CreatedBy != user.UserId)
            {
                return new JsonResult(BadRequest("Field 'createdBy' of this todo in the database does not contain your Id!"));
            }

            foreach (var item in todoInDb.Items)
            {
                item.BelongsToNavigation = null;
            }
            todoInDb.CreatedByNavigation = null;
            return new JsonResult(Ok(todoInDb)); //JsonResult(todoInDb);
        }
        [HttpPut("/todos/{id}")]
        public JsonResult Update(int id, Todo todo)//(string name)
        {
            if (!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if (UserName() == null)
            {
                return new JsonResult(NotFound());
            }
            var user = _context.Users.FirstOrDefault(m => m.UserName == UserName());

            if (id != todo.TodoId)
            {
                return new JsonResult(BadRequest("The id you provide in the Url doesn't much the one in the request body!"));
            }

            var todoInDb = _context.Todos.FirstOrDefault(m => m.TodoId == id);

            if (todoInDb == null)
            {
                return new JsonResult(NotFound());
            }

            if(todoInDb.CreatedBy != user.UserId)
            {
                return new JsonResult(BadRequest("Field 'createdBy' of this todo in the database does not contain your Id!"));
            }

            _context.Remove(todoInDb);
            _context.Add(todo);
            //todoInDb = todo;
            _context.SaveChanges();

            todo.CreatedByNavigation = null;
            return new JsonResult(Ok(todo));
        }
        [HttpDelete("/todos/{id}")]
        public JsonResult Delete(int id)
        {
            if (!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if (UserName() == null)
            {
                return new JsonResult(NotFound());
            }
            var user = _context.Users.FirstOrDefault(m => m.UserName == UserName());

            var todoInDb = _context.Todos.FirstOrDefault(m => m.TodoId == id);

            if (todoInDb == null)
            {
                return new JsonResult(NotFound());
            }

            if(todoInDb.CreatedBy != user.UserId)
            {
                return new JsonResult(BadRequest("Field 'createdBy' of this todo in the database does not contain your Id!"));
            }

            _context.Remove(todoInDb);
            _context.SaveChanges();

            todoInDb.CreatedByNavigation = null;
            return new JsonResult(Ok(todoInDb));
        }




        [HttpGet("/todos/{id}/items/{iid}")]
        public JsonResult Read(int id, int iid)
        {
            if (!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if (UserName() == null)
            {
                return new JsonResult(NotFound());
            }
            var user = _context.Users.FirstOrDefault(m => m.UserName == UserName());

            var todoInDb = _context.Todos.Include(r=>r.Items).FirstOrDefault(m => m.TodoId == id);

            if (todoInDb == null)
            {
                return new JsonResult(NotFound());
            }

            if(todoInDb.CreatedBy != user.UserId)
            {
                return new JsonResult(BadRequest("Field 'createdBy' of this todo in the database does not contain your Id!"));
            }

            /*var itemInDb = _context.Items.*/
            var itemInDb = todoInDb.Items.FirstOrDefault(m => m.ItemId == iid);
            if (itemInDb == null)
            {
                return new JsonResult(NotFound());
            }
            Item it = itemInDb;
            it.BelongsToNavigation = null;
            return new JsonResult(Ok(it));
        }
        [HttpPost("/todos/{id}/items")]
        public JsonResult Create(int id, Item item)
        {
            if (!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if (UserName() == null)
            {
                return new JsonResult(NotFound());
            }
            var user = _context.Users.Include(r=>r.Todos).FirstOrDefault(m => m.UserName == UserName());

            if(!user.Todos.Any(r=>r.TodoId == id))
            {
                return new JsonResult(BadRequest("It seems that you don't have created any todo with that specific Id you provided in the Url!"));
            }

            if (item.BelongsTo != id)
            {
                return new JsonResult(BadRequest("The 'belongsTo' field doesn't match the id in the Url!"));
            }

            _context.Add(item);
            _context.SaveChanges();

            item.BelongsToNavigation = null;
            return new JsonResult(Ok(item));
        }
        [HttpPut("/todos/{id}/items/{iid}")]
        public JsonResult Update(int id, int iid, Item item)
        {
            if (!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if (UserName() == null)
            {
                return new JsonResult(NotFound());
            }
            var user = _context.Users.Include(r => r.Todos).FirstOrDefault(m => m.UserName == UserName());


            if (iid != item.ItemId)
            {
                return new JsonResult(BadRequest("The iid field in the Url doesn't much the one you provide in the itemId field in the request body!"));
            }

            var todoInDb = _context.Todos.Include(r => r.Items).FirstOrDefault(m => m.TodoId == id);

            if (todoInDb == null)
            {
                return new JsonResult(NotFound());
            }

            if(todoInDb.CreatedBy != user.UserId || item.BelongsTo != id)
            {
                return new JsonResult(BadRequest("Field 'createdBy' of this todo in the database does not contain your Id or the BelongsTo field doesn't much the todo's id you provide in the Url!"));
            }

            var itemInDb = todoInDb.Items.FirstOrDefault(m => m.ItemId == iid);
            if (itemInDb == null)
            {
                return new JsonResult(NotFound());
            }

            _context.Remove(itemInDb);
            _context.Add(item);
            _context.SaveChanges();
            Item item1 = item;
            item1.BelongsToNavigation = null;

            return new JsonResult(Ok(item1));
        }
        [HttpDelete("/todos/{id}/items/{iid}")]
        public JsonResult Delete(int id, int iid)
        {
            if (!isActiveAsync(GetCurrentAsync()).Result)
                return new JsonResult(BadRequest("You need to login!"));

            if (UserName() == null)
            {
                return new JsonResult(NotFound());
            }
            var user = _context.Users.Include(r => r.Todos).FirstOrDefault(m => m.UserName == UserName());


            var todoInDb = _context.Todos.Include(r => r.Items).FirstOrDefault(m => m.TodoId == id);

            if (todoInDb == null)
            {
                return new JsonResult(NotFound());
            }

            if(todoInDb.CreatedBy != user.UserId)
            {
                return new JsonResult(BadRequest("Field 'createdBy' of this todo in the database does not contain your Id!"));
            }

            var itemInDb = todoInDb.Items.FirstOrDefault(m => m.ItemId == iid);
            if (itemInDb == null)
            {
                return new JsonResult(NotFound());
            }

            _context.Remove(itemInDb);
            _context.SaveChanges();
            Item item1 = itemInDb;
            item1.BelongsToNavigation = null;

            return new JsonResult(Ok(item1));
        }

        private string CreateJWT(User user)
        {
            List<Claim> claims = new List<Claim> {
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("Jwt:Token").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddDays(1), signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
        private string UserName()
        {
            var username = User?.Identity?.Name;
            return username ?? string.Empty;
        }
    }
}
