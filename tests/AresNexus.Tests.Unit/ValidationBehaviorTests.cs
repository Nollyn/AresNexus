using AresNexus.Services.Settlement.Application.Validation;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace AresNexus.Tests.Unit;

public class ValidationBehaviorTests
{
    private readonly Mock<IValidator<TestRequest>> _validatorMock;
    private readonly Mock<ILogger<ValidationBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly ValidationBehavior<TestRequest, TestResponse> _behavior;

    public ValidationBehaviorTests()
    {
        _validatorMock = new Mock<IValidator<TestRequest>>();
        _loggerMock = new Mock<ILogger<ValidationBehavior<TestRequest, TestResponse>>>();
        
        var validators = new List<IValidator<TestRequest>> { _validatorMock.Object };
        _behavior = new ValidationBehavior<TestRequest, TestResponse>(validators, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoValidators_ShouldCallNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(
            new List<IValidator<TestRequest>>(), 
            _loggerMock.Object);
        var request = new TestRequest();
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = (CancellationToken ct) => 
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse());
        };

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidationSucceeds_ShouldCallNext()
    {
        // Arrange
        var request = new TestRequest();
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = (CancellationToken ct) => 
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse());
        };

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var request = new TestRequest();
        RequestHandlerDelegate<TestResponse> next = (CancellationToken ct) => Task.FromResult(new TestResponse());

        var failures = new List<ValidationFailure> { new("Property", "Error") };
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act
        var act = () => _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public class TestRequest : IRequest<TestResponse> { }
    public class TestResponse { }
}
