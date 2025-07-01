﻿using Application.Commands.Requests;
using Application.Handlers;
using Domain.Entities;
using Domain.Enumerators;
using FluentAssertions;
using Infrastructure.Database.CommandStore;
using Infrastructure.Database.QueryStore;
using Moq;
using NSubstitute;
using System.Security;

namespace Tests.Application.Handlers
{
    public class MovimentarContaHandlerTests
    {
        private readonly Mock<IMovimentoCommandStore> _mockCommandStore;
        private readonly Mock<IContaCorrenteQueryStore> _mockQueryStore;
        private readonly MovimentarContaHandler _handler;

        public MovimentarContaHandlerTests()
        {
            _mockCommandStore = new Mock<IMovimentoCommandStore>();
            _mockQueryStore = new Mock<IContaCorrenteQueryStore>();
            _handler = new MovimentarContaHandler(_mockCommandStore.Object, _mockQueryStore.Object);
        }

        [Fact]
        public async Task Handle_DeveRetornarRespostaComMovimentoId_QuandoSucesso()
        {
            // Arrange
            var request = new MovimentarContaRequest
            {
                ContaCorrenteId = "conta-123",
                Valor = 100m,
                Tipo = "C",
                IdRequisicao = Guid.NewGuid()
            };

            var contaMock = new ContaCorrente
            {
                IdContaCorrente = "",
                Numero = 0,
                Nome = "",
                Ativo = true
            };

            _mockQueryStore.Setup(q => q.ObterContaCorrenteAsync(request.ContaCorrenteId))
                .ReturnsAsync(contaMock);

            _mockCommandStore.Setup(c => c.VerificarIdempotenciaAsync(request.IdRequisicao))
                .ReturnsAsync(string.Empty);

            _mockCommandStore.Setup(c => c.InserirMovimentoAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                             .ReturnsAsync("movimento-id-fake");

            _mockCommandStore.Setup(c => c.RegistrarIdempotenciaAsync(request.IdRequisicao, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var response = await _handler.Handle(request, CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.MovimentoId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Handle_DeveLancarException_QuandoContaNaoExiste()
        {
            // Arrange
            var request = new MovimentarContaRequest
            {
                ContaCorrenteId = "123",
                Tipo = TipoMovimento.Credito.ToString(),
                Valor = 100
            };

            var mockQueryStore = new Mock<IContaCorrenteQueryStore>();
            var mockCommandStore = new Mock<IMovimentoCommandStore>();

            mockQueryStore.Setup(q => q.ObterContaCorrenteAsync(request.ContaCorrenteId))
                          .ReturnsAsync((ContaCorrente)null);

            var handler = new MovimentarContaHandler(mockCommandStore.Object, mockQueryStore.Object);

            // Act
            Func<Task> act = async () => await handler.Handle(request, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<ApplicationException>()
                .WithMessage("Conta corrente não encontrada. | Tipo: INVALID_ACCOUNT");
        }

        [Fact]
        public async Task Handle_DeveLancarException_QuandoContaInativa()
        {
            // Arrange
            var request = new MovimentarContaRequest
            {
                ContaCorrenteId = "conta-123",
                Valor = 100m,
                Tipo = "C",
                IdRequisicao = Guid.NewGuid()
            };

            var contaMock = new ContaCorrente
            {
                IdContaCorrente = "",
                Numero = 0,
                Nome = "",
                Ativo = false
            };

            _mockQueryStore.Setup(q => q.ObterContaCorrenteAsync(request.ContaCorrenteId))
                .ReturnsAsync(contaMock);

            // Act
            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<ApplicationException>()
                .WithMessage("Conta corrente está inativa. | Tipo: INACTIVE_ACCOUNT");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task Handle_DeveLancarException_QuandoValorInvalido(decimal valorInvalido)
        {
            // Arrange
            var request = new MovimentarContaRequest
            {
                ContaCorrenteId = "conta-123",
                Valor = valorInvalido,
                Tipo = "C",
                IdRequisicao = Guid.NewGuid()
            };

            var contaMock = new ContaCorrente
            {
                IdContaCorrente = "123",
                Ativo = true,
                // outros campos
            };

            _mockQueryStore
                .Setup(q => q.ObterContaCorrenteAsync(It.IsAny<string>()))
                .ReturnsAsync(contaMock);

            // Act
            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<ApplicationException>()
                .Where(e => e.Message.Contains("maior que zero"));
        }

        [Theory]
        [InlineData("X")]
        [InlineData("Z")]
        [InlineData(" ")]
        public async Task Handle_DeveLancarException_QuandoTipoMovimentoInvalido(String tipoInvalido)
        {
            // Arrange
            var request = new MovimentarContaRequest
            {
                ContaCorrenteId = "conta-123",
                Valor = 100m,
                Tipo = tipoInvalido,
                IdRequisicao = Guid.NewGuid()
            };

            var contaMock = new ContaCorrente
            {
                IdContaCorrente = "123",
                Ativo = true,
                // outros campos
            };

            _mockQueryStore
                .Setup(q => q.ObterContaCorrenteAsync(It.IsAny<string>()))
                .ReturnsAsync(contaMock);

            // Act
            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<ApplicationException>()
                .WithMessage("Tipo de movimento inválido. Use 'C' ou 'D'. | Tipo: INVALID_TYPE");
        }

        [Fact]
        public async Task Handle_DeveRetornarRespostaIdempotente_QuandoIdempotenciaEncontrada()
        {
            // Arrange
            var request = new MovimentarContaRequest
            {
                ContaCorrenteId = "conta-123",
                Valor = 100m,
                Tipo = "C",
                IdRequisicao = Guid.NewGuid()
            };

            var contaMock = new ContaCorrente
            {
                IdContaCorrente = "123",
                Ativo = true,
                // outros campos
            };

            _mockQueryStore
                .Setup(q => q.ObterContaCorrenteAsync(It.IsAny<string>()))
                .ReturnsAsync(contaMock);

            var movimentoIdExistente = "movimento-existente";

            _mockCommandStore.Setup(c => c.VerificarIdempotenciaAsync(request.IdRequisicao))
                .ReturnsAsync(movimentoIdExistente);

            // Act
            var response = await _handler.Handle(request, CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.MovimentoId.Should().Be(movimentoIdExistente);
        }
    }
}
