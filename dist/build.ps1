param($version, $suffix, $env='release', [switch]$push=$false)

$fullVersion = $version
if (![string]::IsNullOrWhiteSpace($suffix))
{
    $fullVersion = -join($version, '-', $suffix);
}
$outFolder = ".\$fullVersion"


msbuild ..\Our.Umbraco.Queue.sln -t:Rebuild -p:Configuration=$env -clp:Verbosity=minimal

.\nuget pack ..\Our.Umbraco.Queue\Our.Umbraco.Queue.nuspec -build -OutputDirectory $outFolder -version $fullVersion -properties "Configuration=$env"

if ($push) {
    .\nuget.exe push "$outFolder\*.nupkg" -ApiKey AzureDevOps -src https://pkgs.dev.azure.com/jumoo/Public/_packaging/nightly/nuget/v3/index.json
}