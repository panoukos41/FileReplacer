using DynamicData;
using DynamicData.Binding;
using Ookii.Dialogs.Wpf;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FileReplacer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly ObservableCollectionExtended<Operation> _operations = new();

    private int _countProgress = 0;

    public MainWindow()
    {
        InitializeComponent();

        Operations.ItemsSource = _operations;
        ViewModel = new();

        var filterList = OperationResult.List.OrderBy(r => r.Value).Select(r => r.Name).ToList();
        filterList.Insert(0, "-");
        ResultFilter.ItemsSource = filterList;
        ResultFilter.SelectedIndex = 0;

        //PathFilter
        //FilterCount

        this.WhenActivated(d =>
        {
            this.Bind(ViewModel, vm => vm.FilePath, v => v.FilePath.Text)
                .DisposeWith(d);

            this.Bind(ViewModel, vm => vm.Directory, v => v.Directory.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.Searching, v => v.Loading.Visibility, v => ToVisibility(v, true))
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Execute, v => v.Execute)
                .DisposeWith(d);

            ViewModel
                .Execute.IsExecuting
                .Subscribe(v => ExecuteProgress.Visibility = ToVisibility(v))
                .DisposeWith(d);

            this.FilePathPicker.Events()
                .Click
                .Select(_ => new VistaOpenFileDialog
                {
                    Title = "Choose the new file.",
                    Multiselect = false
                })
                .Where(dialog => dialog.ShowDialog() is true)
                .Subscribe(dialog => FilePath.Text = dialog.FileName)
                .DisposeWith(d);

            this.DirectoryPicker.Events()
                .Click
                .Select(_ => new VistaFolderBrowserDialog
                {
                    Description = "Choose the directory.",
                    UseDescriptionForTitle = true,
                    Multiselect = false
                })
                .Where(dialog => dialog.ShowDialog() is true)
                .Subscribe(dialog => Directory.Text = dialog.SelectedPath)
                .DisposeWith(d);

            //var filterByOperation = Observable.Return((Operation o) => o.Result == OperationResult.NotStarted);
            //var filterByPath = Observable.Return((Operation o) => o.Path.Contains("Copy"));


            var filterByOperation = ResultFilter.Events()
                .SelectionChanged
                .Select<SelectionChangedEventArgs, Func<Operation, bool>>(args =>
                {
                    var index = ((ComboBox)args.Source).SelectedIndex;

                    if (index is 0) return static (_) => true;

                    var filter = OperationResult.FromValue(index - 1);

                    return (o) => o.Result == filter;
                })
                .StartWith(static (_) => true);

            var filterByPath = PathFilter.Events()
                .TextChanged
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select<TextChangedEventArgs, Func<Operation, bool>>(args =>
                {
                    var txt = ((TextBox)args.Source).Text;

                    if (txt is not { Length: > 0 }) return static (_) => true;

                    return (o) => o.Path.Contains(txt, StringComparison.OrdinalIgnoreCase);
                })
                .StartWith(static (_) => true);

            ViewModel.Operations
                .Connect()
                .Filter(filterByOperation)
                .Filter(filterByPath)
                .Bind(_operations)
                .Subscribe(_ =>
                {
                    FilterCount.Text = $"Showing {_operations.Count} results!";
                })
                .DisposeWith(d);

            ViewModel.WhenOperationExecuted
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(s =>
                {
                    _countProgress += 1;
                    CountProgress.Text = _countProgress.ToString();
                })
                .DisposeWith(d);

            ViewModel.Operations.CountChanged
                .Subscribe(count =>
                {
                    _countProgress = 0;
                    CountProgress.Text = "0";
                    Count.Text = count.ToString();
                })
                .DisposeWith(d);
        });
    }

    private static Visibility ToVisibility(bool value, bool hidden = false) =>
        hidden
            ? value ? Visibility.Visible : Visibility.Hidden
            : value ? Visibility.Visible : Visibility.Collapsed;
}
