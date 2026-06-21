using System.Collections.Concurrent;
using System.Diagnostics;
using LibreHardwareMonitor.Hardware;
using SensorModel = SensorPanelToo.Models.SensorValue;
using SensorTreeNodeModel = SensorPanelToo.Models.SensorTreeNode;
using SensorValueTypeModel = SensorPanelToo.Models.SensorValueType;

namespace SensorPanelToo.Services;

public sealed class HardwareService : IDisposable
{
    private static readonly Lazy<HardwareService> _instance = new(() => new HardwareService());
    public static HardwareService Instance => _instance.Value;

    private Computer? _computer;
    private readonly object _computerLock = new();
    private readonly UpdateVisitor _updateVisitor = new();

    private volatile int _referenceCount;
    private Thread? _updateThread;
    private CancellationTokenSource? _cts;
    private volatile bool _isRunning;

    private readonly ConcurrentDictionary<string, SensorModel> _sensorCache = new();

    public event Action? SensorsUpdated;
    public bool IsRunning => _isRunning;
    public int ReferenceCount => _referenceCount;

    private HardwareService() { }

    public void Start()
    {
        if (Interlocked.Increment(ref _referenceCount) == 1)
        {
            lock (_computerLock)
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

                _computer.Accept(_updateVisitor);
                RefreshCache();
            }

            _cts = new CancellationTokenSource();
            _isRunning = true;
            _updateThread = new Thread(UpdateLoop)
            {
                IsBackground = true,
                Name = "HardwareService-Update"
            };
            _updateThread.Start(_cts.Token);
        }
    }

    public void Stop()
    {
        int original = Interlocked.Decrement(ref _referenceCount);
        if (original < 0)
        {
            Interlocked.Increment(ref _referenceCount);
            return;
        }
        if (original > 0)
            return;

        _isRunning = false;
        _cts?.Cancel();
        _updateThread?.Join(TimeSpan.FromSeconds(3));
        _cts?.Dispose();
        _cts = null;
        _updateThread = null;

        lock (_computerLock)
        {
            _computer?.Close();
            _computer = null;
        }

        _sensorCache.Clear();
    }

    public Dictionary<string, SensorModel> GetAllSensors()
    {
        return new Dictionary<string, SensorModel>(_sensorCache);
    }

    public SensorModel? GetSensor(string bindingId)
    {
        _sensorCache.TryGetValue(bindingId, out var value);
        return value;
    }

    public List<SensorTreeNodeModel> GetSensorTree()
    {
        lock (_computerLock)
        {
            if (_computer == null)
                return new List<SensorTreeNodeModel>();

            return BuildSensorTree(_computer);
        }
    }

    private List<SensorTreeNodeModel> BuildSensorTree(Computer computer)
    {
        var roots = new List<SensorTreeNodeModel>();

        foreach (var hardware in computer.Hardware)
        {
            var hwNode = new SensorTreeNodeModel
            {
                Name = hardware.Name
            };

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.Value.HasValue)
                    hwNode.Children.Add(CreateSensorLeaf(sensor));
            }

            foreach (var subHw in hardware.SubHardware)
            {
                hwNode.Children.Add(BuildSubHardwareNode(subHw));
            }

            if (hwNode.Children.Count > 0)
                roots.Add(hwNode);
        }

        return roots;
    }

    private SensorTreeNodeModel BuildSubHardwareNode(IHardware subHw)
    {
        var node = new SensorTreeNodeModel
        {
            Name = subHw.Name
        };

        foreach (var sensor in subHw.Sensors)
        {
            if (sensor.Value.HasValue)
                node.Children.Add(CreateSensorLeaf(sensor));
        }

        return node;
    }

    private static SensorTreeNodeModel CreateSensorLeaf(ISensor sensor)
    {
        var bindingId = sensor.Identifier.ToString();
        var unit = GetUnit(sensor.SensorType);

        return new SensorTreeNodeModel
        {
            Name = $"{sensor.Name} ({sensor.SensorType})",
            BindingId = bindingId,
            SensorType = sensor.SensorType.ToString(),
            Unit = unit,
            ValueRange = (sensor.Min ?? 0, sensor.Max ?? 100)
        };
    }

    internal static string GetUnit(SensorType sensorType)
    {
        return sensorType switch
        {
            SensorType.Load => "%",
            SensorType.Temperature => "°C",
            SensorType.Fan => "RPM",
            SensorType.Control => "%",
            SensorType.Power => "W",
            SensorType.Clock => "MHz",
            SensorType.Voltage => "V",
            SensorType.Data => "GB",
            SensorType.SmallData => "MB",
            SensorType.Throughput => "B/s",
            SensorType.Level => "%",
            SensorType.Factor => "",
            SensorType.Frequency => "Hz",
            SensorType.Noise => "dBA",
            SensorType.Flow => "L/h",
            SensorType.TimeSpan => "s",
            SensorType.Current => "A",
            SensorType.Energy => "mWh",
            SensorType.Humidity => "%",
            _ => ""
        };
    }

    private void UpdateLoop(object? state)
    {
        var token = (CancellationToken)state!;

        while (!token.IsCancellationRequested)
        {
            try
            {
                token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                if (token.IsCancellationRequested)
                    break;

                lock (_computerLock)
                {
                    if (_computer == null)
                        break;

                    _computer.Accept(_updateVisitor);
                }

                RefreshCache();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HardwareService error: {ex.Message}");
            }
        }
    }

    private void RefreshCache()
    {
        lock (_computerLock)
        {
            if (_computer == null)
                return;

            foreach (var hardware in _computer.Hardware)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (!sensor.Value.HasValue)
                        continue;

                    UpdateSensorCache(sensor);
                }

                foreach (var subHw in hardware.SubHardware)
                {
                    foreach (var sensor in subHw.Sensors)
                    {
                        if (!sensor.Value.HasValue)
                            continue;

                        UpdateSensorCache(sensor);
                    }
                }
            }
        }

        OnSensorsUpdated();
    }

    private void UpdateSensorCache(ISensor sensor)
    {
        var bindingId = sensor.Identifier.ToString();
        var unit = GetUnit(sensor.SensorType);

        var sensorValue = new SensorModel
        {
            BindingId = bindingId,
            CurrentValue = sensor.Value ?? 0f,
            DisplayText = FormatDisplayText(sensor.Value ?? 0f, unit),
            Unit = unit,
            ValueType = SensorValueTypeModel.Continuous,
            UpperBound = sensor.Max ?? 100f,
            LowerBound = sensor.Min ?? 0f
        };

        _sensorCache[bindingId] = sensorValue;
    }

    private static string FormatDisplayText(float value, string unit)
    {
        return string.IsNullOrEmpty(unit) ? $"{value:F1}" : $"{value:F1}{unit}";
    }

    private void OnSensorsUpdated()
    {
        SensorsUpdated?.Invoke();
    }

    public void Dispose()
    {
        while (_referenceCount > 0)
            Stop();

        _cts?.Dispose();
        _computer?.Close();
    }

    private sealed class UpdateVisitor : IVisitor
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
