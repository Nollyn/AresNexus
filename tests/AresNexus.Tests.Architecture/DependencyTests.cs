using AresNexus.Services.Settlement.Domain.Aggregates;
using NetArchTest.Rules;
using Xunit;
using FluentAssertions;

namespace AresNexus.Tests.Architecture;

public class DependencyTests
{
    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Infrastructure()
    {
        // Arrange
        var domainAssembly = typeof(Account).Assembly;
        
        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("AresNexus.Services.Settlement.Infrastructure")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Domain should not depend on Infrastructure");
    }

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Application()
    {
        // Arrange
        var domainAssembly = typeof(Account).Assembly;
        
        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("AresNexus.Services.Settlement.Application")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Domain should not depend on Application");
    }

    [Fact]
    public void Domain_Should_Not_Have_Any_External_Dependencies()
    {
        // Arrange
        var domainAssembly = typeof(Account).Assembly;
        
        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("AresNexus.Services.Settlement.Infrastructure", 
                                "AresNexus.Services.Settlement.Application", 
                                "AresNexus.Services.Settlement.Api",
                                "Microsoft.EntityFrameworkCore",
                                "Newtonsoft.Json",
                                "StackExchange.Redis")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Domain project must have zero external dependencies to mitigate substitution risk.");
    }
}
