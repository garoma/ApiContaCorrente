using Application.Queries.Requests;
using Application.Queries.Responses;
using Domain.Language;
using Infrastructure.Database.QueryStore;
using MediatR;

public class ConsultarSaldoHandler : IRequestHandler<ConsultarSaldoRequest, ConsultarSaldoResponse>
{
    private readonly IContaCorrenteQueryStore _queryStore;

    public ConsultarSaldoHandler(IContaCorrenteQueryStore queryStore)
    {
        _queryStore = queryStore;
    }

    public async Task<ConsultarSaldoResponse> Handle(ConsultarSaldoRequest request, CancellationToken cancellationToken)
    {
        var conta = await _queryStore.ObterContaCorrenteAsync(request.ContaCorrenteId);
        if (conta == null)
            throw new ApplicationException($"{Mensagens.ContaInvalida} | Tipo: INVALID_ACCOUNT");
        if (!conta.Ativo)
            throw new ApplicationException($"{Mensagens.ContaInativa} | Tipo: INACTIVE_ACCOUNT");

        var saldo = await _queryStore.ObterSaldoAsync(request.ContaCorrenteId);

        return new ConsultarSaldoResponse
        {
            Sucesso = true,
            Mensagem = "Saldo consultado com sucesso.",
            ContaCorrenteId = conta.IdContaCorrente,
            NomeTitular = conta.Nome,
            DataHoraConsulta = DateTime.UtcNow,
            Saldo = saldo
        };
    }
}
