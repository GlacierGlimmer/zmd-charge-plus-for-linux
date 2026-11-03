using System.Globalization;
using EndfieldChargePlus.Customization;
namespace EndfieldChargePlus.Settings;
internal sealed partial class SettingsStatusSampler
{
    private readonly LinuxVariableProvider _statusPlatform=new();
    private partial IDictionary<string,object?> ReadOtherSystem()
    {
        var values=new Dictionary<string,object?>();
        _statusPlatform.Collect(values,new HashSet<string>{"cpu.usage","memory.usage","memory.total_bytes"},null);
        // All physical GPUs, dedicated memory only. Never divide unified/shared RAM by a VRAM budget.
        var readings=new List<GpuStatusReading>();
        _otherAdapters=Array.Empty<GpuStatusReading>();
        try
        {
            double total=0,used=0;int known=0;
            var nvidia=LinuxNvidia.Read();
            foreach(var gpu in nvidia)
            {
                var capacity=Number(gpu,"gpu.memory_total_bytes");var allocated=Number(gpu,"gpu.memory_used_bytes");
                readings.Add(new(gpu.GetValueOrDefault("gpu.id")?.ToString()??"nvidia-"+readings.Count,gpu.GetValueOrDefault("gpu.name")?.ToString()??"NVIDIA GPU",capacity,allocated,null,false));
                if(capacity>0 && allocated>=0){total+=capacity.Value;used+=allocated.Value;known++;}
            }
            foreach(var card in Directory.EnumerateDirectories("/sys/class/drm").Where(p=>System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(p),"^card[0-9]+$")))
            {
                var device=Path.Combine(card,"device");
                if(nvidia.Count>0 && File.Exists(Path.Combine(device,"vendor")) && File.ReadAllText(Path.Combine(device,"vendor")).Trim()=="0x10de")continue;
                if(!File.Exists(Path.Combine(device,"mem_info_vram_total")) || !File.Exists(Path.Combine(device,"mem_info_vram_used")))continue;
                if(double.TryParse(File.ReadAllText(Path.Combine(device,"mem_info_vram_total")),CultureInfo.InvariantCulture,out var capacity) && double.TryParse(File.ReadAllText(Path.Combine(device,"mem_info_vram_used")),CultureInfo.InvariantCulture,out var allocated) && capacity>0 && allocated>=0){total+=capacity;used+=allocated;known++;readings.Add(new(card,Path.GetFileName(card),capacity,allocated,null,false));}
            }
            if(known>0)values["gpu.dedicated_usage"]=Ratio(used,total);
            var clients=new Dictionary<string,double>();
            foreach(var fd in Directory.EnumerateFiles("/proc/self/fdinfo"))
            {
                var fields=File.ReadAllLines(fd).Select(line=>line.Split(':',2)).Where(p=>p.Length==2).ToDictionary(p=>p[0].Trim(),p=>p[1].Trim());
                if(!fields.TryGetValue("drm-client-id",out var id) || !fields.TryGetValue("drm-memory-vram",out var text))continue;
                var tokens=text.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
                if(tokens.Length<2 || !double.TryParse(tokens[0],CultureInfo.InvariantCulture,out var amount))continue;
                double factor=tokens[1] switch{"KiB"=>1024,"MiB"=>1048576,"B"=>1,_=>0};if(factor==0)continue;
                string key=fields.GetValueOrDefault("drm-pdev","")+":"+id;
                clients[key]=Math.Max(clients.GetValueOrDefault(key),amount*factor);
            }
            if(clients.Count>0){double own=clients.Values.Sum();values["app.gpu_bytes"]=own;values["app.gpu_usage"]=Ratio(own,total);}
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException) { }
        _otherAdapters=readings;
        return values;
    }
}
