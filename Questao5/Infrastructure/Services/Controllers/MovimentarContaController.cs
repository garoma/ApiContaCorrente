using Application.Commands.Requests;
using Application.Commands.Responses;
using Application.Queries.Requests;
using Application.Queries.Responses;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace Infrastructure.Database.Services.Controllers
{
    public static class MovimentarContaController
    {
        public static void MapMovimentarContaEndpoint(this WebApplication app)
        {
            app.MapPost("/conta/{id}/movimentar", async (string id, MovimentarContaRequest body, IMediator mediator) =>
            {
                body.ContaCorrenteId = id;
                var result = await mediator.Send(body);
                return Results.Ok(result);
            })
            .WithName("MovimentarConta")
            .WithTags("Conta Corrente")
            .WithMetadata(new SwaggerOperationAttribute(
                summary: "Realiza movimentação de crédito ou débito na conta",
                description: "Movimenta o saldo da conta corrente com suporte a idempotência"
            ))
            .Produces<MovimentarContaResponse>(200)
            .Produces(400);
        }

        public static void MapConsultarSaldoEndpoint(this WebApplication app)
        {
            app.MapGet("/conta/{id}/saldo", async (string id, IMediator mediator) =>
            {
                var response = await mediator.Send(new ConsultarSaldoRequest(id));
                return Results.Ok(response);
            })
            .WithName("ConsultarSaldo")
            .WithTags("Conta Corrente")
            .WithMetadata(new SwaggerOperationAttribute(
                summary: "Consulta o saldo atual da conta",
                description: "Retorna o valor atual do saldo com nome do titular e horário da consulta"
            ))
            .Produces<ConsultarSaldoResponse>(200)
            .Produces(400);
        }
    }
}