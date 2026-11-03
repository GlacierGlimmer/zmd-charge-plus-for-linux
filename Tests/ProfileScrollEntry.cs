using Avalonia;
using EndfieldChargePlus;
internal static class ProfileScrollEntry
{
    [STAThread]
    public static int Main()
    {
        try
        {
            AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().With(LocalizationManager.FontOptions).SetupWithoutStarting();
            int checks=0;
            ProfileScrollChecks.Run((valid,message)=>{if(!valid)throw new Exception(message);checks++;});
            Console.WriteLine($"PASS {checks} native profile scroll checks");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
