using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BotDashboard.App.Data;

public class BotHarborDbContext : IdentityDbContext<IdentityUser>
{
    public BotHarborDbContext(DbContextOptions<BotHarborDbContext> options) : base(options)
    {
    }
}