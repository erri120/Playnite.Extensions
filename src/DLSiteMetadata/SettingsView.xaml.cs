using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DLSiteMetadata;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    // private Settings GetSettings()
    // {
    //     if (DataContext is not Settings settings)
    //         throw new InvalidDataException();
    //     return settings;
    // }
}
