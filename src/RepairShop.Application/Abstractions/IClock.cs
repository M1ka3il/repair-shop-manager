namespace RepairShop.Application.Abstractions;

/// <summary>
/// Injectable time source. Exists so that use cases touching timestamps stay
/// deterministic in tests instead of depending on <c>DateTime.UtcNow</c>.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
