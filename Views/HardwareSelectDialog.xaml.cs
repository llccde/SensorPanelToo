using System;
using System.Windows;
using SensorPanelToo.Services;

namespace SensorPanelToo.Views;

public partial class HardwareSelectDialog : Window
{
    public HardwareSelectDialog()
    {
        InitializeComponent();
        LoadCurrentState();
    }

    void LoadCurrentState()
    {
        if (HardwareService.Instance.IsRunning)
        {
            ChkCpu.IsChecked = HardwareService.Instance.IsCpuEnabled;
            ChkGpu.IsChecked = HardwareService.Instance.IsGpuEnabled;
            ChkMemory.IsChecked = HardwareService.Instance.IsMemoryEnabled;
            ChkMotherboard.IsChecked = HardwareService.Instance.IsMotherboardEnabled;
            ChkNetwork.IsChecked = HardwareService.Instance.IsNetworkEnabled;
            ChkStorage.IsChecked = HardwareService.Instance.IsStorageEnabled;
            ChkController.IsChecked = HardwareService.Instance.IsControllerEnabled;
        }
        else
        {
            ChkCpu.IsChecked = true;
            ChkMemory.IsChecked = true;
            ChkGpu.IsChecked = false;
            ChkStorage.IsChecked = false;
            ChkMotherboard.IsChecked = false;
            ChkNetwork.IsChecked = false;
            ChkController.IsChecked = false;
        }
    }

    private void StartBtn_Click(object sender, RoutedEventArgs e)
    {
        StartBtn.IsEnabled = false;
        StartBtn.Content = "启动中…";

        try
        {
            HardwareService.Instance.Start(
                cpu: ChkCpu.IsChecked == true,
                gpu: ChkGpu.IsChecked == true,
                memory: ChkMemory.IsChecked == true,
                motherboard: ChkMotherboard.IsChecked == true,
                network: ChkNetwork.IsChecked == true,
                storage: ChkStorage.IsChecked == true,
                controller: ChkController.IsChecked == true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        DialogResult = true;
        Close();
    }

    private void SkipBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            HardwareService.Instance.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        DialogResult = true;
        Close();
    }
}
