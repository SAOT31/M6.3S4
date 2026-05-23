namespace Firmeza.Core.Interfaces;

// Interfaz de persistencia para desacoplar el contexto
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
