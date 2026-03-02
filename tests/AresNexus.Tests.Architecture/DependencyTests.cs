using AresNexus.Settlement.Domain.Aggregates;
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
            .HaveDependencyOn("AresNexus.Settlement.Infrastructure")
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
            .HaveDependencyOn("AresNexus.Settlement.Application")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Domain should not depend on Application");
    }

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Api()
    {
        // Arrange
        var domainAssembly = typeof(Account).Assembly;
        
        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("AresNexus.Settlement.Api")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Domain should not depend on Api");
    }
}
