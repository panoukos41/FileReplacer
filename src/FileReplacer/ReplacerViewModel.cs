using DynamicData;
using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FileReplacer;

public class ReplacerViewModel : ReactiveObject
{
    private string _file = "";
    private string _path = "";
    private bool _searching;

    public SourceCache<Operation, string> Operations { get; } = new(x => x.Path);

    public string FilePath { get => _file; set => this.RaiseAndSetIfChanged(ref _file, value); }

    public string Directory { get => _path; set => this.RaiseAndSetIfChanged(ref _path, value); }

    public bool Searching { get => _searching; set => this.RaiseAndSetIfChanged(ref _searching, value); }

    /// <summary>
    /// Observable that emits messages when an operation is executed.
    /// </summary>
    public IObservable<OperationResult> WhenOperationExecuted { get; }

    public ReactiveCommand<Unit, Unit> Execute { get; }

    public ReplacerViewModel()
    {
        this.WhenAnyValue(x => x.FilePath, x => x.Directory)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .Where(x => PathIsValid(x.Item1) && PathIsValid(x.Item2))
            .DistinctUntilChanged()
            .Select(paths =>
            {
                Searching = true;

                var fileName = Path.GetFileName(paths.Item1);

                if (fileName is null) return Array.Empty<Operation>();

                var directory = new DirectoryInfo(paths.Item2);

                if (directory is null or { Exists: false }) return Array.Empty<Operation>();

                return directory
                    .EnumerateDirectories()
                    .Where(dir => dir.GetFiles().Any(file => file.Name == fileName))
                    .Select(dir => new Operation(Path.Combine(dir.FullName, fileName)))
                    .ToArray();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(directories =>
            {
                Operations.Clear();
                Operations.AddOrUpdate(directories);
                Searching = false;
            });

        var whenOperationExecutedSub = new Subject<OperationResult>();
        WhenOperationExecuted = whenOperationExecutedSub;

        Execute = ReactiveCommand.CreateFromObservable(
            canExecute: Operations.CountChanged.Select(count => count > 0),
            execute: () => Operations.Items
            .ToObservable()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Where(operation => operation.Result == OperationResult.NotStarted)
            .Select(operation =>
            {
                try
                {
                    operation.Result = OperationResult.Running;
                    File.Copy(FilePath, operation.Path, true);
                    operation.Result = OperationResult.Success;
                }
                catch (Exception ex)
                {
                    operation.Result = OperationResult.Failure;
                    operation.Error = ex.Message;
                }
                finally
                {
                    whenOperationExecutedSub.OnNext(operation.Result);
                }
                return operation;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToArray()
            .Do(operations => Operations.AddOrUpdate(operations))
            .Select(_ => Unit.Default));
    }

    private static bool PathIsValid(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }
        return true;
    }
}
