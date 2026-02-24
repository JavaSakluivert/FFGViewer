using System.Text;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using FFGViewer.Services;
using FFGViewer.ViewModels;

namespace FFGViewer;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Shift-JIS 有効化
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // DI コンテナ構築
        var services = new ServiceCollection();
        services.AddSingleton<IFfgFileService, FfgFileService>();
        services.AddSingleton<ICsvExportService, CsvExportService>();
        services.AddSingleton<IExcelExportService, ExcelExportService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // 起動引数（アイコンへの D&D）があれば読み込み
        if (e.Args.Length > 0)
        {
            var vm = _serviceProvider.GetRequiredService<MainViewModel>();
            var ffgFiles = e.Args
                .Where(a => a.EndsWith(".ffg", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (ffgFiles.Length > 0)
                vm.LoadFiles(ffgFiles);
        }
    }
}
