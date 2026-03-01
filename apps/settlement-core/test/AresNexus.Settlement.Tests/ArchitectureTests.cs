using AresNexus.Settlement.Domain.Aggregates;
using FluentAssertions;
using Xunit;

namespace AresNexus.Settlement.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Infrastructure_Or_ThirdParty()
    {
        // Arrange
        var domainAssembly = typeof(Account).Assembly;
        
        // Define forbidden dependencies
        string[] forbiddenDependencies = 
        [
            "AresNexus.Settlement.Infrastructure",
            "AresNexus.Settlement.Application",
            "AresNexus.Settlement.Api",
            "Marten",
            "StackExchange.Redis",
            "Azure.Messaging.ServiceBus",
            "Serilog",
            "Microsoft.EntityFrameworkCore",
            "Newtonsoft.Json"
        ];

        // Act: Manual check via reflection since NetArchTest is unavailable
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();
        var failingDependencies = referencedAssemblies
            .Where(a => forbiddenDependencies.Any(fd => a.Name?.Contains(fd, StringComparison.OrdinalIgnoreCase) == true))
            .Select(a => a.Name)
            .ToList();

        // Assert
        failingDependencies.Should().BeEmpty($"Domain project should not reference infrastructure or third-party libraries. Failed dependencies: {string.Join(", ", failingDependencies)}");
    }
}
