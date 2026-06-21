using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;

namespace SensorPanelToo
{
    public partial class MonitorWindow : Window
    {
        private Computer _computer = null!;
        private DispatcherTimer _timer = null!;
        private UpdateVisitor _updateVisitor = null!;
        private readonly Dictionary<string, SensorItem> _sensorMap = new();

        public static readonly DependencyProperty ForegroundColorProperty =
            DependencyProperty.Register(
                nameof(ForegroundColor),
                typeof(SolidColorBrush),
                typeof(MonitorWindow),
                new PropertyMetadata(new SolidColorBrush(Colors.LimeGreen),
                    (d, e) => ((MonitorWindow)d).OnForegroundColorChanged()));

        public SolidColorBrush ForegroundColor
        {
            get => (SolidColorBrush)GetValue(ForegroundColorProperty);
            set => SetValue(ForegroundColorProperty, value);
        }

        public ObservableCollection<SensorItem> Sensors { get; } = new();

        public MonitorWindow()
        {
            InitializeComponent();
            SensorListView.ItemsSource = Sensors;
            InitializeComputer();
            StartMonitoring();
        }

        private void InitializeComputer()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = true,
                IsControllerEnabled = true
            };
            _computer.Open();
            _updateVisitor = new UpdateVisitor();
        }

        private void StartMonitoring()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += (s, e) => RefreshSensors();
            _timer.Start();
            RefreshSensors();
        }

        private void RefreshSensors()
        {
            _computer.Accept(_updateVisitor);

            var currentKeys = new HashSet<string>();

            foreach (var hardware in _computer.Hardware)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (!sensor.Value.HasValue)
                        continue;

                    var key = sensor.Identifier.ToString();
                    currentKeys.Add(key);

                    if (_sensorMap.TryGetValue(key, out var item))
                    {
                        item.Value = sensor.Value.Value;
                        item.Min = sensor.Min;
                        item.Max = sensor.Max;
                    }
                    else
                    {
                        item = new SensorItem
                        {
                            Key = key,
                            HardwareName = hardware.Name,
                            SensorName = sensor.Name,
                            SensorType = sensor.SensorType.ToString(),
                            Value = sensor.Value.Value,
                            Min = sensor.Min,
                            Max = sensor.Max,
                            Foreground = ForegroundColor
                        };
                        _sensorMap[key] = item;
                        Sensors.Add(item);
                    }
                }
            }

            for (int i = Sensors.Count - 1; i >= 0; i--)
            {
                var item = Sensors[i];
                if (!currentKeys.Contains(item.Key))
                {
                    _sensorMap.Remove(item.Key);
                    Sensors.RemoveAt(i);
                }
            }
        }

        private void OnForegroundColorChanged()
        {
            foreach (var item in Sensors)
                item.Foreground = ForegroundColor;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _timer?.Stop();
            _computer?.Close();
            base.OnClosing(e);
        }
    }

    public class SensorItem : INotifyPropertyChanged
    {
        private string _key = "";
        private string _hardwareName = "";
        private string _sensorName = "";
        private string _sensorType = "";
        private float _value;
        private float? _min;
        private float? _max;
        private SolidColorBrush _foreground = new(Colors.LimeGreen);

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public string HardwareName
        {
            get => _hardwareName;
            set { _hardwareName = value; OnPropertyChanged(); }
        }

        public string SensorName
        {
            get => _sensorName;
            set { _sensorName = value; OnPropertyChanged(); }
        }

        public string SensorType
        {
            get => _sensorType;
            set { _sensorType = value; OnPropertyChanged(); }
        }

        public float Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayValue)); }
        }

        public float? Min
        {
            get => _min;
            set { _min = value; OnPropertyChanged(); OnPropertyChanged(nameof(MinDisplay)); }
        }

        public float? Max
        {
            get => _max;
            set { _max = value; OnPropertyChanged(); OnPropertyChanged(nameof(MaxDisplay)); }
        }

        public SolidColorBrush Foreground
        {
            get => _foreground;
            set { _foreground = value; OnPropertyChanged(); }
        }

        public string DisplayValue => $"{Value:F1}";
        public string MinDisplay => Min.HasValue ? $"{Min.Value:F1}" : "-";
        public string MaxDisplay => Max.HasValue ? $"{Max.Value:F1}" : "-";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware)
                subHardware.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
