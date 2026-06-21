using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SensorPanelToo
{
    public partial class MainWindow : Window
    {
        private MonitorWindow? _monitorWindow;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_monitorWindow == null)
            {
                _monitorWindow = new MonitorWindow();
                _monitorWindow.Closed += (s, args) =>
                {
                    _monitorWindow = null;
                    StatusText.Text = "";
                };
                _monitorWindow.Show();
                ApplySelectedColor();
                StatusText.Text = "Monitor window opened.";
            }
            else
            {
                _monitorWindow.Focus();
                StatusText.Text = "Monitor window already open - focused.";
            }
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySelectedColor();
        }

        private void ApplySelectedColor()
        {
            if (_monitorWindow == null) return;

            var selectedItem = ColorComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            var colorStr = selectedItem.Tag as string;
            if (colorStr == null) return;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorStr);
                _monitorWindow.ForegroundColor = new SolidColorBrush(color);
                StatusText.Text = $"Font color set to {selectedItem.Content}.";
            }
            catch
            {
                StatusText.Text = "Failed to apply color.";
            }
        }
    }
}
