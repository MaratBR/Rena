using TestingThisFeature.Models;
using Microsoft.AspNetCore.Mvc;

namespace TestingThisFeature.Controllers
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