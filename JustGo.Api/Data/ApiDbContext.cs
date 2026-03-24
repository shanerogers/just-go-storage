using Microsoft.EntityFrameworkCore;

namespace JustGo.Api.Data;

public sealed class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
}
