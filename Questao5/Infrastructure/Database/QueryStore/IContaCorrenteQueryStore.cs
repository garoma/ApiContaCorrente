using System.Threading.Tasks;
using Domain.Entities;

namespace Infrastructure.Database.QueryStore
{
    public interface IContaCorrenteQueryStore
    {
        Task<ContaCorrente?> ObterContaCorrenteAsync(string id);
        Task<decimal> ObterSaldoAsync(string id);
    }
}
