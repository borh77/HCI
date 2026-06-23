using System.Windows;
using StickItApp.Services;

namespace StickItApp;

public partial class App : Application
{
    public static CsvDataService DataService { get; private set; } = null!;

    public static CsvDataStore DataStore { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        DataService = new CsvDataService();
        DataService.Initialize();
        DataStore = DataService.LoadAll();

        base.OnStartup(e);
    }
}
