using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenJobEngine.Infrastructure.Persistence;

public sealed class OpenJobEngineDbContextFactory : IDesignTimeDbContextFactory<OpenJobEngineDbContext>
{
    public OpenJobEngineDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OpenJobEngineDbContext>();
        optionsBuilder.UseSqlite("Data Source=openjobengine.design.db");

        return new OpenJobEngineDbContext(optionsBuilder.Options);
    }
}
