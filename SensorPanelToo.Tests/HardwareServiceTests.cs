namespace SensorPanelToo.Tests;

public class HardwareServiceUnitTests
{
    [Fact]
    public void Instance_ReturnsSameSingleton()
    {
        var a = Services.HardwareService.Instance;
        var b = Services.HardwareService.Instance;
        Assert.Same(a, b);
    }
}

[CollectionDefinition("HardwareService")]
public class HardwareServiceCollection : ICollectionFixture<HardwareServiceFixture> { }

public class HardwareServiceFixture : IDisposable
{
    public HardwareServiceFixture()
    {
        Services.HardwareService.Instance.Start();
        Thread.Sleep(2000);
    }

    public void Dispose()
    {
        while (Services.HardwareService.Instance.ReferenceCount > 0)
            Services.HardwareService.Instance.Stop();
    }
}

[Collection("HardwareService")]
public class HardwareServiceIntegrationTests
{
    private readonly HardwareServiceFixture _fixture;

    public HardwareServiceIntegrationTests(HardwareServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void GetAllSensors_ReturnsPopulatedCache()
    {
        var sensors = Services.HardwareService.Instance.GetAllSensors();

        Assert.NotNull(sensors);
        Assert.True(sensors.Count > 0, "Expected at least one sensor");

        foreach (var kvp in sensors)
        {
            Assert.NotNull(kvp.Value);
            Assert.NotEmpty(kvp.Value.BindingId);
            Assert.StartsWith("/", kvp.Key);
            Assert.Equal(kvp.Key, kvp.Value.BindingId);
        }
    }

    [Fact]
    public void GetSensor_ByValidBindingId_ReturnsSensor()
    {
        var allSensors = Services.HardwareService.Instance.GetAllSensors();
        var firstKey = allSensors.Keys.FirstOrDefault();

        Assert.NotNull(firstKey);

        var sensor = Services.HardwareService.Instance.GetSensor(firstKey!);

        Assert.NotNull(sensor);
        Assert.Equal(firstKey, sensor!.BindingId);
    }

    [Fact]
    public void GetSensor_ByInvalidBindingId_ReturnsNull()
    {
        var sensor = Services.HardwareService.Instance.GetSensor("Nonexistent-Type-Name");

        Assert.Null(sensor);
    }

    [Fact]
    public void GetSensorTree_ReturnsValidStructure()
    {
        var tree = Services.HardwareService.Instance.GetSensorTree();

        Assert.NotNull(tree);
        Assert.NotEmpty(tree);

        foreach (var root in tree)
        {
            Assert.NotEmpty(root.Name);
            Assert.NotNull(root.Children);
        }
    }

    [Fact]
    public void ReferenceCounting_MultipleStartStop()
    {
        var svc = Services.HardwareService.Instance;
        int initialCount = svc.ReferenceCount;
        Assert.True(initialCount > 0);

        svc.Start();
        Assert.Equal(initialCount + 1, svc.ReferenceCount);

        svc.Start();
        Assert.Equal(initialCount + 2, svc.ReferenceCount);

        svc.Stop();
        Assert.Equal(initialCount + 1, svc.ReferenceCount);
        Assert.True(svc.IsRunning);

        svc.Stop();
        Assert.Equal(initialCount, svc.ReferenceCount);
    }

    [Fact]
    public void GetAllSensors_ReturnsIndependentSnapshots()
    {
        var snapshot1 = Services.HardwareService.Instance.GetAllSensors();
        var snapshot2 = Services.HardwareService.Instance.GetAllSensors();

        Assert.Equal(snapshot1.Count, snapshot2.Count);

        if (snapshot1.Count > 0)
        {
            var firstKey = snapshot1.Keys.First();
            Assert.Equal(snapshot1[firstKey].BindingId, snapshot2[firstKey].BindingId);
        }
    }
}
