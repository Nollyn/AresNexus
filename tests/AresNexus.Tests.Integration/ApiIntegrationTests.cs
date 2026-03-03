using System.Net;
using System.Net.Http.Json;
using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using AresNexus.Settlement.Application.Interfaces;
using Microsoft.AspNetCore.TestHost;
using MediatR;

namespace AresNexus.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Mock out Marten and ServiceBus to avoid needing real infrastructure
                var eventStoreMock = new Mock<IEventStore>();
                services.AddScoped(_ => eventStoreMock.Object);
                
                var outboxPublisherMock = new Mock<IOutboxPublisher>();
                services.AddSingleton(_ => outboxPublisherMock.Object);

                var accountRepoMock = new Mock<IAccountRepository>();
                services.AddScoped(_ => accountRepoMock.Object);
            });
        });
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content!.status.Should().Be("UP");
    }

    private record HealthResponse(string status);
    private record ErrorResponse(string error);

    [Fact]
    public async Task ProcessTransaction_WithMissingIdempotencyKey_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new {
            AccountId = Guid.NewGuid(),
            Amount = new { Value = 100 },
            Type = "DEPOSIT"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProcessTransaction_ShouldHandleException_AndReturn500()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var mediatorMock = new Mock<ISender>();
                mediatorMock.Setup(m => m.Send(It.IsAny<ProcessTransactionCommand>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Internal error"));
                services.AddScoped<ISender>(_ => mediatorMock.Object);
            });
        }).CreateClient();

        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(100), "DEPOSIT", Guid.NewGuid());

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content!.error.Should().Be("Unhandled error");
    }

    [Fact]
    public async Task GetHealthLive_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
