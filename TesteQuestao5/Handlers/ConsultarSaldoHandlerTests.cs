using Application.Queries.Requests;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Database.QueryStore;
using Moq;

namespace Tests.Application.Handlers
{
    public class ConsultarSaldoHandlerTests
    {
        private readonly Mock<IContaCorrenteQueryStore> _mockQueryStore;
        private readonly ConsultarSaldoHandler _handler;

        public ConsultarSaldoHandlerTests()
        {
            _mockQueryStore = new Mock<IContaCorrenteQueryStore>();
            _handler = new ConsultarSaldoHandler(_mockQueryStore.Object);
        }

        [Fact]
        public async Task DeveRetornarSaldoComSucesso_QuandoContaExiste()
        {
            // Arrange
            var request = new ConsultarSaldoRequest("B6BAFC09-6967-ED11-A567-055DFA4A16C9");
            var contaMock = new ContaCorrente
            {
                IdContaCorrente = "B6BAFC09-6967-ED11-A567-055DFA4A16C9",
                Numero = 0,
                Nome = "",
                Ativo = true
            };

            _mockQueryStore.Setup(q => q.ObterContaCorrenteAsync(request.ContaCorrenteId))
                           .ReturnsAsync(contaMock);

            _mockQueryStore.Setup(q => q.ObterSaldoAsync(request.ContaCorrenteId))
                           .ReturnsAsync(1500.75m);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Sucesso.Should().BeTrue();
            result.Saldo.Should().Be(1500.75m);
            result.Mensagem.Should().Be("Saldo consultado com sucesso.");
        }

        [Fact]
        public async Task DeveRetornarErro_QuandoContaNaoExiste()
        {
            // Arrange
            var request = new ConsultarSaldoRequest ("B6BAFC09-6967-ED11-A567-055DFA4A16C9");

            _mockQueryStore.Setup(q => q.ObterContaCorrenteAsync(request.ContaCorrenteId))
                           .ReturnsAsync((ContaCorrente)null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Sucesso.Should().BeFalse();
            result.Saldo.Should().Be(0);
            result.Mensagem.Should().Be("Conta não encontrada.");
        }
    }
}
