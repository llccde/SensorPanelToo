using System;
using System.Windows;
using SensorPanelToo.Services;

namespace SensorPanelToo.Views;

public partial class HardwareSelectDialog : Window
{
    public HardwareSelectDialog()
    {
        InitializeComponent();
    }

    private void StartBtn_Click(object sender, RoutedEventArgs e)
    {
        StartBtn.IsEnabled = false;
        StartBtn.Content = "Starting...";

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
            MessageBox.Show($"Failed: {ex.Message}", "Error",
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
            MessageBox.Show($"Failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        DialogResult = true;
        Close();
    }
}
