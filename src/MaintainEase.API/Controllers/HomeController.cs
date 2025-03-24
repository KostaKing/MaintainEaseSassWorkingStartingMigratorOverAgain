using Microsoft.AspNetCore.Mvc;

namespace MaintainEase.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "MaintainEase API is running" });
        }
    }
}
