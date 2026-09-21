using Microsoft.EntityFrameworkCore;
using Identity.Sso.Application.Interfaces.Persistence;

namespace Identity.Sso.Persistence.UnitOfWorks;

/// <summary>
/// Generic UnitOfWork implementation for any EF Core DbContext.
/// Each persistence layer registers its own instance with the appropriate context type.
/// </summary>
public class UnitOfWorkEFCore<TContext>(TContext context) : IUnitOfWork
    where TContext : DbContext
{
    public async Task CommitAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

