using EndfieldChargePlus.Customization;
namespace EndfieldChargePlus.Settings;

public sealed record HudInstanceSettings
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = "HUD";
    public bool Enabled { get; init; } = true;
    public AppSettings Settings { get; init; } = new();
}
