using Ardalis.SmartEnum;
using ReactiveUI;
using System.Linq;

namespace FileReplacer;

public class Operation : ReactiveObject
{
    private OperationResult _result;
    private string? _error;

    public string Path { get; }

    public Operation(string path)
    {
        Path = path;
        _result = OperationResult.NotStarted;
    }

    public OperationResult Result
    {
        get => _result;
        set => this.RaiseAndSetIfChanged(ref _result, value);
    }

    public string? Error
    {
        get => _error;
        set => this.RaiseAndSetIfChanged(ref _error, value);
    }
}

public class OperationResult : SmartEnum<OperationResult>
{
    public static readonly OperationResult NotStarted  = new("Not Started", 0);

    public static readonly OperationResult Running  = new("Running", 1);

    public static readonly OperationResult Success = new("Success", 2);

    public static readonly OperationResult Failure  = new("Failure", 3);

    private OperationResult(string name, int value) : base(name, value)
    {
    }
}
