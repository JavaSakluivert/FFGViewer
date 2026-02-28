using System.IO;
using System.Reflection;
using System.Windows;

namespace FFGViewer.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        PopulateInfo();
    }

    private void PopulateInfo()
    {
        var asm = Assembly.GetExecutingAssembly();

        var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "FFGViewer";
        var version = asm.GetName().Version?.ToString(3) ?? "1.0.0";
        var description = asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty;
        var company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;
        var copyright = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;

        // Authors は MSBuild <Authors> → AssemblyMetadataAttribute("Authors", ...) として埋め込まれる
        var authors = asm.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "Authors")?.Value ?? company;

        var buildDate = GetBuildDate(asm);

        AppNameText.Text = product;
        VersionText.Text = $"Version {version}";
        BuildDateText.Text = $"Build: {buildDate:yyyy-MM-dd}";
        DescriptionText.Text = description;
        AuthorText.Text = authors;
        OrgText.Text = company;
        CopyrightText.Text = copyright;
    }

    private static DateTime GetBuildDate(Assembly asm)
    {
        var location = asm.Location;
        return File.Exists(location)
            ? File.GetLastWriteTime(location)
            : DateTime.Today;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
