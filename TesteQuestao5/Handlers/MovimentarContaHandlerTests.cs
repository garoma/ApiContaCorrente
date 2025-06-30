using Application.Commands.Requests;
using Application.Handlers;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Database.CommandStore;
using Infrastructure.Database.QueryStore;
using Moq;
using NSubstitute;

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
                Tipo = 'C',
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

            _mockCommandStore.Setup(c => c.InserirMovimentoAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<char>()))
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
                IdRequisicao = Guid.NewGuid(),
                ContaCorrenteId = "conta-invalida",
                Valor = 100m,
                Tipo = 'C'
            };

            _mockQueryStore.Setup(q => q.ObterContaCorrenteAsync(It.IsAny<string>()))
                           .ReturnsAsync((ContaCorrente?)null); // ESSENCIAL!
            // Act
            Func<Task> act = async () => await _handler.Handle(request, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ApplicationException>()
                .WithMessage("*Conta inválida*");
        }

        [Fact]
        public async Task Handle_DeveLancarException_QuandoContaInativa()
        {
            // Arrange
            var request = new MovimentarContaRequest
            {
                ContaCorrenteId = "conta-123",
                Valor = 100m,
                Tipo = 'C',
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
            await act.Should().ThrowAsync<ApplicationException>()
                .WithMessage("*Conta inativa*");
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
                Tipo = 'C',
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
            await act.Should().ThrowAsync<ApplicationException>()
                .WithMessage("*Valor inválido*");
        }

        [Theory]
        [InlineData('X')]
        [InlineData('Z')]
        [InlineData(' ')]
        public async Task Handle_DeveLancarException_QuandoTipoMovimentoInvalido(char tipoInvalido)
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
            await act.Should().ThrowAsync<ApplicationException>()
                .WithMessage("*Tipo inválido*");
        }

        [Fact]
        public async Task Handle_DeveRetornarRespostaIdempotente_QuandoIdempotenciaEncontrada()
        {
            // Arrange
            var request = new MovimentarContaRequest
            {
                ContaCorrenteId = "conta-123",
                Valor = 100m,
                Tipo = 'C',
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
