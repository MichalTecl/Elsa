param (
   [string]$targetProj
)

# $targetProj = "C:\Elsa\DEV\PsSenchaTest\Elsa\Elsa.Portal\Elsa.Portal.csproj"

# Set $targetDir to the directory containing $targetProj
$targetDir = Split-Path -Parent $targetProj

# Function to find the directory containing the *.sln file
function Find-SlnRoot($path) {
    $currentDir = $path
    while ($currentDir -ne [System.IO.Path]::GetPathRoot($currentDir)) {
        if (Test-Path (Join-Path $currentDir "*.sln")) {
            return $currentDir
        }
        $currentDir = Split-Path -Parent $currentDir
    }
    return $null
}

# Find $slnRoot
$slnRoot = Find-SlnRoot $targetDir

if ($slnRoot -eq $null) {
    Write-Error "No .sln file found in the directory structure above $targetDir"
    exit 1
}

# Process all directories inside $slnRoot looking for sencha.json
Get-ChildItem -Path $slnRoot -Recurse -Filter "sencha.json" | ForEach-Object {
    $senchaFilePath = $_.FullName
    $senchaDir = Split-Path -Parent $senchaFilePath

    # Load the contents of sencha.json
    $senchaConfig = Get-Content -Path $senchaFilePath | ConvertFrom-Json

    # Process each SenchaRoot
    foreach ($senchaRoot in $senchaConfig.SenchaRoots) {
        $sourceDir = Join-Path -Path $senchaDir -ChildPath $senchaRoot
        $targetSenchaDir = Join-Path -Path $targetDir -ChildPath $senchaRoot

        if (-Not (Test-Path -Path $sourceDir)) {
            Write-Host "Skipping $sourceDir because it does not exist."
            continue
        }

        if (-Not (Test-Path -Path $targetSenchaDir)) {
            New-Item -ItemType Directory -Path $targetSenchaDir -Force
        }

        # Copy all files and directories from $sourceDir to $targetSenchaDir
        Get-ChildItem -Path $sourceDir -Recurse | ForEach-Object {
            $sourceFilePath = $_.FullName
            $relativePath = $sourceFilePath.Substring($sourceDir.Length)
            $destinationFilePath = Join-Path -Path $targetSenchaDir -ChildPath $relativePath

            # Create the directory if it does not exist
            $destinationDir = Split-Path -Parent $destinationFilePath
            if (-Not (Test-Path -Path $destinationDir)) {
                New-Item -ItemType Directory -Path $destinationDir -Force
            }

            Write-Host "Copying $sourceFilePath to $destinationFilePath"
            Copy-Item -Path $sourceFilePath -Destination $destinationFilePath -Force
        }
    }
}

Write-Host "Copy operation completed."