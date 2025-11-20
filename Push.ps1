$scriptName = $MyInvocation.MyCommand.Name
$artifacts = "./artifacts"

if ([string]::IsNullOrEmpty($Env:NUGET_API_KEY)) {
    Write-Host "${scriptName}: NUGET_API_KEY is empty or not set. Skipped pushing package(s)."
} else {
    Get-ChildItem $artifacts -Filter "*.nupkg" | ForEach-Object {
        Write-Host "$($scriptName): Pushing $($_.Name)"
        dotnet nuget push $_ --source $Env:NUGET_URL --api-key $Env:NUGET_API_KEY --skip-duplicate
        if ($lastexitcode -ne 0) {
            throw ("Exec: " + $errorMessage)
        }
    }
}
