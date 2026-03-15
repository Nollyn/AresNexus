using AresNexus.BuildingBlocks.Domain;
using FluentAssertions;

namespace AresNexus.Tests.Unit;

public class SharedKernelTests
{
    [Fact]
    public void AsDynamic_ShouldReturnObjectAsDynamic()
    {
        // Arrange
        var obj = new { Name = "Test", Value = 123 };

        // Act
        dynamic dynamicObj = obj.AsDynamic();

        // Assert
        ((string)dynamicObj.Name).Should().Be("Test");
        ((int)dynamicObj.Value).Should().Be(123);
    }
}
