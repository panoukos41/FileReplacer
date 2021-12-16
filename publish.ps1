[CmdletBinding()]
param (
    [Parameter(Position = 0)]
    [ValidateSet("win-x64", "win-x86")]
    $runtime = "win-x86",

    [Parameter(Position = 1)]
    [ValidateSet("true", "false")]
    [switch] $self_contained = $true,
    
    [Parameter(Position = 2)]
    [ValidateSet("true", "false")]
    [switch] $trimmed = $false,

    [Parameter(Position = 3)]
    [string] $out = "./publish"
)

$out = "$out/$runtime"

Remove-Item $out -r *>$null

dotnet publish `
    ./src/FileReplacer/FileReplacer.csproj `
    --nologo `
    -c "Release" `
    -r "$runtime" `
    -o "$out" `
    -p:PublishSingleFile=$true `
    -p:DebugType=embedded `
    -p:IsTransformWebConfigDisabled=true `
    --self-contained $self_contained `
    -p:PublishTrimmed=$trimmed