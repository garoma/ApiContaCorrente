using MediatR;
using Application.Commands.Responses;

namespace Application.Commands.Requests
{
    public class MovimentarContaRequest : IRequest<MovimentarContaResponse>
    {
        public Guid IdRequisicao { get; set; }
        public string ContaCorrenteId { get; set; }
        public decimal Valor { get; set; }
        public string Tipo { get; set; } // 'C' ou 'D'
    }
}