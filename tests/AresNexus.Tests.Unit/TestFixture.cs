using AutoFixture;
using AutoFixture.AutoMoq;

namespace AresNexus.Tests.Unit;

public static class TestFixture
{
    public static IFixture Create()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        
        // Handle circular references if any
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Customizations for specific domain types
        fixture.Customize<decimal>(c => c.FromFactory((int i) => Math.Abs(i) / 100m));
        
        return fixture;
    }
}
