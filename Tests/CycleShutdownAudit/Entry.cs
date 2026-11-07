using System.Reflection;
using Avalonia;
using Avalonia.Threading;
using EndfieldChargePlus;
internal static partial class Audit {
 static int _checks;static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string message){if(!value)throw new Exception(message);_checks++;}
 static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Private)!.GetValue(o)!;
 static int Main(string[] args){
  if(Environment.GetEnvironmentVariable("ECP_INPUT_TEST_ISOLATED")!="1" || Environment.GetEnvironmentVariable("XAUTHORITY")?.Contains("xvfb-run")!=true || Environment.GetEnvironmentVariable("DISPLAY")?.StartsWith(":")!=true)throw new Exception("Use isolated Xvfb only.");
  string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);
  AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().SetupWithoutStarting();
  Exception? error=null;bool completed=false;using var done=new CancellationTokenSource(TimeSpan.FromMinutes(3));
  Dispatcher.UIThread.Post(async()=>{try{await RunChecks(output);completed=true;}catch(Exception ex){error=ex;}finally{done.Cancel();}});
  Dispatcher.UIThread.MainLoop(done.Token);if(error!=null)throw error;if(!completed)throw new TimeoutException();
  File.WriteAllText(Path.Combine(output,"result.txt"),$"PASS {_checks} native Xvfb cycle/shutdown assertions\n");Console.WriteLine($"PASS {_checks} native Xvfb cycle/shutdown assertions");return 0;
 }
}
