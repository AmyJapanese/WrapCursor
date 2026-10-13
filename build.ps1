param([string]$DotnetPath='dotnet',[switch]$SelfContained)
$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
    & $DotnetPath restore tests/WrapCursor.Tests --configfile NuGet.Config
    if($LASTEXITCODE) { throw 'Test restore failed' }
    & $DotnetPath build tests/WrapCursor.Tests -c Release --no-restore --disable-build-servers
    if($LASTEXITCODE) { throw 'Test build failed' }
    & $DotnetPath tests/WrapCursor.Tests/bin/Release/net8.0/WrapCursor.Tests.dll
    if($LASTEXITCODE) { throw 'Tests failed' }
    & $DotnetPath publish src/WrapCursor -c Release -r win-x64 --self-contained ($SelfContained.IsPresent.ToString().ToLowerInvariant()) -o dist --configfile NuGet.Config --disable-build-servers -m:1
    if($LASTEXITCODE) { throw 'Publish failed' }
} finally { Pop-Location }
