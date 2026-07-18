namespace RepairShop.Application.Abstractions;

/// <summary>
/// Commits the changes tracked by the repositories in one transaction.
/// Keeping this separate from the repositories means a use case that touches
/// several aggregates still results in exactly one save.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
