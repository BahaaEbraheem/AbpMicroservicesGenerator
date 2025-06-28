# PowerShell script to rename AbpSolutionGenerator to AbpMicroservicesGenerator

Write-Host "Starting rename process from AbpSolutionGenerator to AbpMicroservicesGenerator..." -ForegroundColor Green

# Define old and new names
$oldName = "AbpSolutionGenerator"
$newName = "AbpMicroservicesGenerator"
$oldNameLower = "abpsolutiongenerator"
$newNameLower = "abpmicroservicesgenerator"

# Get current directory
$rootPath = Get-Location

Write-Host "Working in directory: $rootPath" -ForegroundColor Yellow

# Step 1: Rename solution file
Write-Host "Step 1: Renaming solution file..." -ForegroundColor Cyan
if (Test-Path "$oldName.sln") {
    Rename-Item "$oldName.sln" "$newName.sln"
    Write-Host "Renamed solution file to $newName.sln" -ForegroundColor Green
}

# Step 2: Rename directories
Write-Host "Step 2: Renaming directories..." -ForegroundColor Cyan
$directories = Get-ChildItem -Directory -Recurse | Where-Object { $_.Name -like "*$oldName*" } | Sort-Object FullName -Descending

foreach ($dir in $directories) {
    $newDirName = $dir.Name -replace $oldName, $newName
    $newPath = Join-Path $dir.Parent.FullName $newDirName
    
    Write-Host "Renaming directory: $($dir.FullName) -> $newPath" -ForegroundColor Yellow
    Rename-Item $dir.FullName $newPath
}

# Step 3: Rename files
Write-Host "Step 3: Renaming files..." -ForegroundColor Cyan
$files = Get-ChildItem -File -Recurse | Where-Object { $_.Name -like "*$oldName*" }

foreach ($file in $files) {
    $newFileName = $file.Name -replace $oldName, $newName
    $newPath = Join-Path $file.Directory.FullName $newFileName
    
    Write-Host "Renaming file: $($file.FullName) -> $newPath" -ForegroundColor Yellow
    Rename-Item $file.FullName $newPath
}

Write-Host "Step 4: Updating file contents..." -ForegroundColor Cyan

# Get all text files to update content
$textFiles = Get-ChildItem -File -Recurse -Include "*.cs", "*.csproj", "*.sln", "*.json", "*.razor", "*.html", "*.js", "*.css", "*.xml", "*.yml", "*.yaml", "*.md", "*.txt" | Where-Object { $_.Length -lt 10MB }

foreach ($file in $textFiles) {
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $originalContent = $content
        
        # Replace all variations of the old name
        $content = $content -replace $oldName, $newName
        $content = $content -replace $oldNameLower, $newNameLower
        
        # Save only if content changed
        if ($content -ne $originalContent) {
            Set-Content $file.FullName -Value $content -Encoding UTF8 -NoNewline
            Write-Host "Updated content in: $($file.FullName)" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "Error updating file $($file.FullName): $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "Rename process completed successfully!" -ForegroundColor Green
Write-Host "Please rebuild the solution to ensure everything works correctly." -ForegroundColor Yellow
