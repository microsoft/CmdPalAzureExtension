Param(
    [string]$ClientId,
    [string]$ClientSecret
)

$env:Build_RootDirectory = (Get-Item $PSScriptRoot).parent.parent.FullName

