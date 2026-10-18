namespace EndfieldChargePlus.Customization;

internal static class NativeHardware
{
    private static readonly string[] Prefixes={"cpu.","memory.","battery.","gpu.","disk.","system."};
    internal static string[] RequestedKeys=>VariableCatalog.AllBuiltIns.Where(d=>Prefixes.Any(p=>d.Key.StartsWith(p,StringComparison.OrdinalIgnoreCase))).Select(d=>d.Key).ToArray();
    internal static bool Requested(HashSet<string>? requested)=>requested is null || requested.Any(k=>k.StartsWith("sensor.") || k.StartsWith("hardware."));
    internal static HashSet<string>? Expand(HashSet<string>? requested)
    {
        if(requested is null)return null;
        if(Requested(requested))requested.UnionWith(RequestedKeys);
        return requested;
    }
    internal static void Publish(Dictionary<string,object?> values)
    {
        string source=OperatingSystem.IsMacOS() ? "macos" : "linux";
        var definitions=VariableCatalog.AllBuiltIns.ToDictionary(d=>d.Key,StringComparer.OrdinalIgnoreCase);
        var readings=values.Where(p=>p.Value is not null && Prefixes.Any(prefix=>p.Key.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)))
            .Select(p=>new HardwareReading(source,p.Key,source,definitions.TryGetValue(p.Key,out var definition) ? VariableLocalization.Localize(definition).Name : p.Key,"Native",definitions.GetValueOrDefault(p.Key)?.Unit ?? "",p.Value)).ToArray();
        HardwareTelemetryCatalog.Publish(readings);HardwareTelemetryCatalog.Write(values,readings);
        values["hardware.sensor_count"]=readings.Length;
        values["hardware.elevated"]=Interop.ProcessPrivileges.IsElevated;
        values["hardware.cpu_temperature_status"]=values.GetValueOrDefault("cpu.temperature_max") is not null ? LocalizationManager.Text("可用","Available") : LocalizationManager.Text("不可用","Unavailable");
    }
}
