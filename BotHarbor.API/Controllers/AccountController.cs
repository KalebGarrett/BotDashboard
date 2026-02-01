using BotDashboard.Models.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BotHarbor.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }
    
    [HttpGet]
    public IActionResult Welcome()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Unauthorized("You are not authenticated");
        }

        return Ok("You are authenticated");
    }
    
    [Authorize] 
    [HttpGet("Profile")]
    public async Task<IActionResult> Profile()
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return BadRequest();
        }

        var userProfile = new UserProfile
        {
            Id = currentUser.Id,
            Name = currentUser.UserName,
            Email = currentUser.Email,
            PhoneNumber = currentUser.PhoneNumber
        };

        return Ok(userProfile);
    }
}