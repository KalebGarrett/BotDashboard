using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BotHarbor.API.Services;

public class BotHarborDbContext : IdentityDbContext
{
    public BotHarborDbContext(DbContextOptions options) : base(options)
    {
    }
}