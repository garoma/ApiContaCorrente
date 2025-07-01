using Application.Commands.Requests;
using Application.Commands.Responses;
using Domain.Language;
using Infrastructure.Database.CommandStore;
using Infrastructure.Database.QueryStore;
using MediatR;

namespace Application.Handlers
{
    public class MovimentarContaHandler : IRequestHandler<MovimentarContaRequest, MovimentarContaResponse>
    {
        private readonly IMovimentoCommandStore _commandStore;
        private readonly IContaCorrenteQueryStore _queryStore;

        public MovimentarContaHandler(IMovimentoCommandStore commandStore, IContaCorrenteQueryStore queryStore)
        {
            _commandStore = commandStore;
            _queryStore = queryStore;
        }

        public async Task<MovimentarContaResponse> Handle(MovimentarContaRequest request, CancellationToken cancellationToken)
        {
            var conta = await _queryStore.ObterContaCorrenteAsync(request.ContaCorrenteId);
            if (conta == null)
                throw new ApplicationException($"{Mensagens.ContaInvalida} | Tipo: INVALID_ACCOUNT");

            if (!conta.Ativo)
                throw new ApplicationException($"{Mensagens.ContaInativa} | Tipo: INACTIVE_ACCOUNT");

            if (request.Valor <= 0)
                throw new ApplicationException($"{Mensagens.ValorInvalido} | Tipo: INVALID_VALUE");

            if (request.Tipo != "C" && request.Tipo != "D")
                throw new ApplicationException($"{Mensagens.TipoMovimentoInvalido} | Tipo: INVALID_TYPE");

            // Verifica idempotência
            var idempotente = await _commandStore.VerificarIdempotenciaAsync(request.IdRequisicao);
            if (!string.IsNullOrEmpty(idempotente))
                return new MovimentarContaResponse(idempotente);

            // Gera um movimentoId do tipo string (ex: um GUID)
            var movimentoId = Guid.NewGuid().ToString(); // ou outro gerador, se preferir

            await _commandStore.InserirMovimentoAsync(request.ContaCorrenteId, request.Valor, request.Tipo);
            await _commandStore.RegistrarIdempotenciaAsync(request.IdRequisicao, movimentoId);

            return new MovimentarContaResponse(movimentoId);
        }

    }
}