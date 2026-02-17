using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BotDashboard.App.Data;

public class BotHarborDbContext(DbContextOptions<BotHarborDbContext> options)
    : IdentityDbContext<BotHarborUser>(options)
{
}