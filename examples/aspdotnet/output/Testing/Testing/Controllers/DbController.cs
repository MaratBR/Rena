using Testing.Models;
using Microsoft.AspNetCore.Mvc;

namespace Testing.Controllers
{
    public class DbController : ControllerBase
    {
        protected readonly AppDbContext DbContext;

        public DbController(AppDbContext context)
        {
            DbContext = context;
        }
    }
}