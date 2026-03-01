using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace AresNexus.Settlement.Infrastructure.Logging;

/// <summary>
/// Serilog destructuring policy to mask sensitive data (Reference field) in logs.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly string[] SensitiveFields = ["Reference"];
    private const string Mask = "***MASKED***";

    /// <inheritdoc />
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        var type = value.GetType();
        
        // Only destructure our own objects (domain events, etc.)
        if (!type.FullName!.StartsWith("AresNexus"))
        {
            result = null;
            return false;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var logEventProperties = new List<LogEventProperty>();

        foreach (var property in properties)
        {
            var propValue = property.GetValue(value);
            
            if (SensitiveFields.Contains(property.Name) && propValue != null)
            {
                logEventProperties.Add(new LogEventProperty(property.Name, new ScalarValue(Mask)));
            }
            else
            {
                logEventProperties.Add(new LogEventProperty(property.Name, propertyValueFactory.CreatePropertyValue(propValue, true)));
            }
        }

        result = new StructureValue(logEventProperties, type.Name);
        return true;
    }
}
