Param(
    [string]$Platform = "x64",
    [string]$Configuration = "debug",
    [switch]$IsAzurePipelineBuild = $false,
    [switch]$Help = $false
)

$StartTime = Get-Date

if ($Help) {
    Write-Host @"
Copyright (c) Microsoft Corporation.
Licensed under the MIT License.

Syntax:
      Test.cmd [options]

Description:
      Runs AzureExtension tests.

Options:

  -Platform <platform>
      Only buil the selected platform(s)
      Example: -Platform x64
      Example: -Platform "x86,x64,arm64"

  -Configuration <configuration>
      Only build the selected configuration(s)
      Example: -Configuration release
      Example: -Configuration "debug,release"

  -Help
      Display this usage message.
"@
  Exit
}

# Root is two levels up from the script location.
$env:Build_SourcesDirectory = (Get-Item $PSScriptRoot).parent.parent.FullName
$env:Build_Platform = $Platform.ToLower()
$env:Build_Configuration = $Configuration.ToLower()

$vstestPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -find **\TestPlatform\vstest.console.exe

$ErrorActionPreference = "Stop"

Try {
  foreach ($platform in $env:Build_Platform.Split(",")) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      $vstestArgs = @(
          ("/Platform:$platform"),
          ("/Logger:trx;LogFileName=AzureExtension.Test-$platform-$configuration.trx"),
          ("/TestCaseFilter:""TestCategory!=LiveData"""),
          ("BuildOutput\$configuration\$platform\AzureExtension.Test\AzureExtension.Test.dll")
      )

      & $vstestPath $vstestArgs
    }
  }
} Catch {
  $formatString = "`n{0}`n`n{1}`n`n"
  $fields = $_, $_.ScriptStackTrace
  Write-Host ($formatString -f $fields) -ForegroundColor RED
  Exit 1
}

$TotalTime = (Get-Date)-$StartTime
$TotalMinutes = [math]::Floor($TotalTime.TotalMinutes)
$TotalSeconds = [math]::Ceiling($TotalTime.TotalSeconds)

Write-Host @"

Total Running Time:
$TotalMinutes minutes and $TotalSeconds seconds
"@ -ForegroundColor CYAN