namespace Crashguard.Engine.Connectors;

/// <summary>
/// Looks up the <see cref="IConnector"/> registered for a given channel type. Adding a new connector
/// is just a new class implementing <see cref="IConnector"/> registered in Program.cs — nothing here
/// needs to change.
/// </summary>
public class ConnectorRegistry(IEnumerable<IConnector> connectors)
{
    private readonly Dictionary<string, IConnector> _byType =
        connectors.ToDictionary(c => c.Type, StringComparer.OrdinalIgnoreCase);

    public IConnector? Resolve(string type) => _byType.GetValueOrDefault(type);
}
