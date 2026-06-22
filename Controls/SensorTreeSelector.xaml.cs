using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SensorPanelToo.Models;
using SensorPanelToo.Services;

namespace SensorPanelToo.Controls;

public partial class SensorTreeSelector : UserControl
{
    public static readonly DependencyProperty SelectedBindingIdProperty =
        DependencyProperty.Register(nameof(SelectedBindingId), typeof(string), typeof(SensorTreeSelector),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedBindingIdChanged));

    public string? SelectedBindingId
    {
        get => (string?)GetValue(SelectedBindingIdProperty);
        set => SetValue(SelectedBindingIdProperty, value);
    }

    public event Action<string?>? SelectionChanged;

    private readonly ObservableCollection<SensorTreeItem> _roots = new();
    private List<SensorTreeItem> _allLeaves = new();

    public SensorTreeSelector()
    {
        InitializeComponent();
        Loaded += (_, _) => PopulateTree();
    }

    public void PopulateTree()
    {
        _roots.Clear();
        _allLeaves.Clear();

        var tree = HardwareService.Instance.GetSensorTree();
        foreach (var node in tree)
        {
            var item = BuildItem(node);
            _roots.Add(item);
        }

        SensorTree.ItemsSource = _roots;
    }

    private SensorTreeItem BuildItem(SensorTreeNode node)
    {
        var item = new SensorTreeItem
        {
            DisplayName = node.Name,
            BindingId = node.BindingId,
            UnitSuffix = node.Unit != null ? $" ({node.Unit})" : ""
        };

        if (node.BindingId != null)
            _allLeaves.Add(item);

        foreach (var child in node.Children)
            item.Children.Add(BuildItem(child));

        return item;
    }

    private void SensorTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is SensorTreeItem item && item.BindingId != null)
        {
            SelectedBindingId = item.BindingId;
            SelectionChanged?.Invoke(item.BindingId);
        }
    }

    private static void OnSelectedBindingIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Select corresponding node in tree
        var selector = (SensorTreeSelector)d;
        var id = e.NewValue as string;
        if (id != null)
            selector.SelectById(id);
    }

    private void SelectById(string bindingId)
    {
        foreach (var item in _allLeaves)
        {
            if (item.BindingId == bindingId)
            {
                item.IsSelected = true;
                ExpandToItem(item);
                return;
            }
        }
    }

    private void ExpandToItem(SensorTreeItem target)
    {
        var parent = FindParent(_roots, target);
        while (parent != null)
        {
            parent.IsExpanded = true;
            parent = FindParent(_roots, parent);
        }
    }

    private static SensorTreeItem? FindParent(IEnumerable<SensorTreeItem> items, SensorTreeItem target)
    {
        foreach (var item in items)
        {
            if (item.Children.Contains(target))
                return item;

            var found = FindParent(item.Children, target);
            if (found != null)
                return found;
        }
        return null;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var filter = SearchBox.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(filter))
        {
            SensorTree.ItemsSource = _roots;
            return;
        }

        var filtered = _allLeaves
            .Where(leaf => leaf.DisplayName.ToLowerInvariant().Contains(filter))
            .Select(leaf => new SensorTreeItem
            {
                DisplayName = $"{leaf.DisplayName}{leaf.UnitSuffix}",
                BindingId = leaf.BindingId,
                UnitSuffix = ""
            })
            .ToList();

        SensorTree.ItemsSource = filtered;
    }
}

public class SensorTreeItem : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isSelected;

    public string DisplayName { get; set; } = "";
    public string? BindingId { get; set; }
    public string UnitSuffix { get; set; } = "";
    public List<SensorTreeItem> Children { get; set; } = new();

    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
