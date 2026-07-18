using RepairShop.Application.Abstractions;

namespace RepairShop.Infrastructure;

/// <inheritdoc cref="IClock"/>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
