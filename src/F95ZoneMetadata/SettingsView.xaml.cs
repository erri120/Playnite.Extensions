using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace F95ZoneMetadata;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private Settings GetSettings()
    {
        if (DataContext is not Settings settings)
            throw new InvalidDataException();
        return settings;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var settings = GetSettings();
        settings.DoLogin();
    }
}

