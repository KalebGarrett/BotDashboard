using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BotHarbor.App2.Data;

public class BotHarborDbContext(DbContextOptions<BotHarborDbContext> options)
    : IdentityDbContext<BotHarborUser>(options)
{
}