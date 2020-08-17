using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ScreenshotPlugin
{
    public partial class HotkeySelectionControl
    {
        public static readonly DependencyProperty HotkeyProperty =
            DependencyProperty.Register(nameof(Hotkey), typeof(Hotkey.Hotkey),
                typeof(HotkeySelectionControl),
                new FrameworkPropertyMetadata(default(Hotkey.Hotkey),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Hotkey.Hotkey Hotkey
        {
            get => (Hotkey.Hotkey) GetValue(HotkeyProperty);
            set => SetValue(HotkeyProperty, value);
        }

        public HotkeySelectionControl()
        {
            InitializeComponent();
            //DataContext = this;
        }
        
        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Don't let the event pass further
            // because we don't want standard textbox shortcuts working
            e.Handled = true;

            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // Pressing delete, backspace or escape without modifiers clears the current value
            if (modifiers == ModifierKeys.None &&
                (key == Key.Delete || key == Key.Back || key == Key.Escape))
            {
                Hotkey = null;
                return;
            }

            // If no actual key was pressed - return
            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps)
            {
                return;
            }

            // Update the value
            Hotkey = new Hotkey.Hotkey
            {
                KeyCode = key,
                KeyModifiers = modifiers
            };
        }
    }
}
