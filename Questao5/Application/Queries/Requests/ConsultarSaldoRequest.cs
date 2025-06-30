using MediatR;
using Application.Queries.Responses;

namespace Application.Queries.Requests
{
    public class ConsultarSaldoRequest : IRequest<ConsultarSaldoResponse>
    {
        public string ContaCorrenteId { get; set; }
        public ConsultarSaldoRequest(string contaCorrenteId)
        {
            ContaCorrenteId = contaCorrenteId;
        }
    }
}