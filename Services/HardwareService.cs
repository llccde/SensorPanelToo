using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
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
    private List<SensorTreeNodeModel>? _cachedTree;

    private Thread? _updateThread;
    private CancellationTokenSource? _cts;
    private volatile bool _isRunning;

    private bool _cpu;
    private bool _gpu;
    private bool _memory;
    private bool _motherboard;
    private bool _network;
    private bool _storage;
    private bool _controller;

    private readonly ConcurrentDictionary<string, SensorModel> _sensorCache = new();

    public event Action? SensorsUpdated;
    public bool IsRunning => _isRunning;

    private HardwareService() { }

    public void Start(bool cpu = true, bool gpu = false, bool memory = true, bool motherboard = false,
        bool network = false, bool storage = true, bool controller = false)
    {
        bool needRestart = _isRunning && (_cpu != cpu || _gpu != gpu || _memory != memory ||
            _motherboard != motherboard || _network != network || _storage != storage || _controller != controller);

        _cpu |= cpu;
        _gpu |= gpu;
        _memory |= memory;
        _motherboard |= motherboard;
        _network |= network;
        _storage |= storage;
        _controller |= controller;

        if (!_isRunning || needRestart)
        {
            if (_isRunning)
            {
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
                    _cachedTree = null;
                }
            }

            lock (_computerLock)
            {
                _computer = new Computer
                {
                    IsCpuEnabled = _cpu,
                    IsGpuEnabled = _gpu,
                    IsMemoryEnabled = _memory,
                    IsMotherboardEnabled = _motherboard,
                    IsStorageEnabled = _storage,
                    IsNetworkEnabled = _network,
                    IsControllerEnabled = _controller
                };
                _computer.Open();
                _computer.Accept(_updateVisitor);
                RefreshCacheCore();
                _cachedTree = BuildSensorTree(_computer);
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

    public Dictionary<string, SensorModel> GetAllSensors()
        => new(_sensorCache);

    public SensorModel? GetSensor(string bindingId)
    {
        _sensorCache.TryGetValue(bindingId, out var value);
        return value;
    }

    public List<SensorTreeNodeModel> GetSensorTree()
    {
        var tree = _cachedTree;
        return tree == null ? new() : new List<SensorTreeNodeModel>(tree);
    }

    private List<SensorTreeNodeModel> BuildSensorTree(Computer computer)
    {
        var roots = new List<SensorTreeNodeModel>();
        foreach (var hardware in computer.Hardware)
        {
            var hwNode = new SensorTreeNodeModel { Name = hardware.Name };
            foreach (var sensor in hardware.Sensors)
                if (sensor.Value.HasValue) hwNode.Children.Add(CreateSensorLeaf(sensor));
            foreach (var subHw in hardware.SubHardware)
                hwNode.Children.Add(BuildSubHardwareNode(subHw));
            if (hwNode.Children.Count > 0) roots.Add(hwNode);
        }
        return roots;
    }

    private SensorTreeNodeModel BuildSubHardwareNode(IHardware subHw)
    {
        var node = new SensorTreeNodeModel { Name = subHw.Name };
        foreach (var sensor in subHw.Sensors)
            if (sensor.Value.HasValue) node.Children.Add(CreateSensorLeaf(sensor));
        return node;
    }

    private static SensorTreeNodeModel CreateSensorLeaf(ISensor sensor)
        => new()
        {
            Name = $"{sensor.Name} ({sensor.SensorType})",
            BindingId = sensor.Identifier.ToString(),
            SensorType = sensor.SensorType.ToString(),
            Unit = GetUnit(sensor.SensorType),
            ValueRange = (sensor.Min ?? 0, sensor.Max ?? 100)
        };

    internal static string GetUnit(SensorType sensorType) => sensorType switch
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

    internal static (float Min, float Max) GetSensorTypeBounds(SensorType type, float? observedMin, float? observedMax)
    {
        float min = observedMin ?? 0f, max = observedMax ?? 100f;
        return type switch
        {
            SensorType.Load or SensorType.Control or SensorType.Level or SensorType.Humidity => (0f, 100f),
            SensorType.Temperature => (min, Math.Max(max, 100f)),
            SensorType.Fan => (0f, Math.Max(max, 5000f)),
            SensorType.Voltage => (0f, Math.Max(max, 5f)),
            SensorType.Power => (min, Math.Max(max, 200f)),
            SensorType.Clock => (0f, Math.Max(max, 5000f)),
            SensorType.Data or SensorType.SmallData => (0f, Math.Max(max, 100f)),
            SensorType.Throughput or SensorType.Noise or SensorType.Flow or SensorType.Frequency => (0f, Math.Max(max, 100f)),
            _ => (min, Math.Max(max, 100f))
        };
    }

    private void UpdateLoop(object? state)
    {
        var token = (CancellationToken)state!;
        while (!token.IsCancellationRequested)
        {
            try
            {
                token.WaitHandle.WaitOne(250);
                if (token.IsCancellationRequested) break;
                lock (_computerLock)
                {
                    if (_computer == null) break;
                    _computer.Accept(_updateVisitor);
                    RefreshCacheCore();
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Debug.WriteLine($"HardwareService: {ex.Message}"); }
        }
    }

    private void RefreshCacheCore()
    {
        if (_computer == null) return;
        foreach (var hardware in _computer.Hardware)
        {
            foreach (var sensor in hardware.Sensors)
                if (sensor.Value.HasValue) UpdateSensorCache(sensor);
            foreach (var subHw in hardware.SubHardware)
                foreach (var sensor in subHw.Sensors)
                    if (sensor.Value.HasValue) UpdateSensorCache(sensor);
        }
        SensorsUpdated?.Invoke();
    }

    private void UpdateSensorCache(ISensor sensor)
    {
        var id = sensor.Identifier.ToString();
        var unit = GetUnit(sensor.SensorType);
        var bounds = GetSensorTypeBounds(sensor.SensorType, sensor.Min, sensor.Max);
        _sensorCache[id] = new SensorModel
        {
            BindingId = id,
            CurrentValue = sensor.Value ?? 0f,
            DisplayText = string.IsNullOrEmpty(unit) ? $"{sensor.Value ?? 0f:F1}" : $"{sensor.Value ?? 0f:F1}{unit}",
            Unit = unit,
            ValueType = SensorValueTypeModel.Continuous,
            UpperBound = bounds.Max,
            LowerBound = bounds.Min
        };
    }

    public void Dispose()
    {
        _isRunning = false;
        _cts?.Cancel();
        _updateThread?.Join(TimeSpan.FromSeconds(3));
        _cts?.Dispose();
        lock (_computerLock) { _computer?.Close(); _computer = null; _cachedTree = null; }
        _sensorCache.Clear();
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer c) => c.Traverse(this);
        public void VisitHardware(IHardware h)
        {
            try
            {
                var task = Task.Run(h.Update);
                if (!task.Wait(TimeSpan.FromSeconds(2)))
                {
                    Debug.WriteLine($"HardwareService: Update timed out for {h.Name} ({h.HardwareType})");
                    return;
                }
            }
            catch (AggregateException ex)
            {
                Debug.WriteLine($"HardwareService: Update failed for {h.Name}: {ex.InnerException?.Message}");
                return;
            }
            foreach (var s in h.SubHardware) s.Accept(this);
        }
        public void VisitSensor(ISensor _) { }
        public void VisitParameter(IParameter _) { }
    }
}
