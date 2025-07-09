# =============================================
# Club Management Application - Deployment Script
# Version: 1.0.0
# Description: Automates application deployment process
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Production",
    
    [Parameter(Mandatory=$false)]
    [string]$TargetPath = "C:\Applications\ClubManagement",
    
    [Parameter(Mandatory=$false)]
    [string]$BackupPath = "C:\Backups\ClubManagement",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBackup = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipDatabase = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = ""
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Function to create directory if it doesn't exist
function Ensure-Directory {
    param([string]$Path)
    if (!(Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-ColorOutput "Created directory: $Path" "Green"
    }
}

# Function to backup existing application
function Backup-Application {
    param(
        [string]$SourcePath,
        [string]$BackupPath
    )
    
    if (Test-Path $SourcePath) {
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $backupDir = Join-Path $BackupPath "Backup_$timestamp"
        
        Write-ColorOutput "Creating backup at: $backupDir" "Yellow"
        Copy-Item -Path $SourcePath -Destination $backupDir -Recurse -Force
        Write-ColorOutput "Backup completed successfully" "Green"
        
        # Clean up old backups (keep last 5)
        $backups = Get-ChildItem $BackupPath -Directory | Sort-Object CreationTime -Descending
        if ($backups.Count -gt 5) {
            $backups | Select-Object -Skip 5 | ForEach-Object {
                Remove-Item $_.FullName -Recurse -Force
                Write-ColorOutput "Removed old backup: $($_.Name)" "Gray"
            }
        }
    }
}

# Function to deploy database
function Deploy-Database {
    param([string]$ConnectionString)
    
    Write-ColorOutput "Deploying database..." "Yellow"
    
    $scriptPath = Join-Path $PSScriptRoot "deploy-database.sql"
    if (!(Test-Path $scriptPath)) {
        throw "Database deployment script not found: $scriptPath"
    }
    
    try {
        # Use sqlcmd to execute the script
        $result = sqlcmd -S "(localdb)\MSSQLLocalDB" -i $scriptPath -b
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "Database deployment completed successfully" "Green"
        } else {
            throw "Database deployment failed with exit code: $LASTEXITCODE"
        }
    }
    catch {
        Write-ColorOutput "Error deploying database: $($_.Exception.Message)" "Red"
        throw
    }
}

# Function to publish application
function Publish-Application {
    param(
        [string]$ProjectPath,
        [string]$OutputPath,
        [string]$Environment
    )
    
    Write-ColorOutput "Publishing application for $Environment environment..." "Yellow"
    
    $publishPath = Join-Path $OutputPath "publish"
    
    # Build and publish the application
    $buildArgs = @(
        "publish",
        $ProjectPath,
        "--configuration", "Release",
        "--runtime", "win-x64",
        "--self-contained", "true",
        "--output", $publishPath,
        "/p:PublishSingleFile=true",
        "/p:IncludeNativeLibrariesForSelfExtract=true"
    )
    
    try {
        & dotnet @buildArgs
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "Application published successfully" "Green"
        } else {
            throw "Application publish failed with exit code: $LASTEXITCODE"
        }
    }
    catch {
        Write-ColorOutput "Error publishing application: $($_.Exception.Message)" "Red"
        throw
    }
    
    return $publishPath
}

# Function to update configuration
function Update-Configuration {
    param(
        [string]$PublishPath,
        [string]$Environment,
        [string]$ConnectionString
    )
    
    Write-ColorOutput "Updating configuration for $Environment environment..." "Yellow"
    
    $configFile = Join-Path $PublishPath "appsettings.$Environment.json"
    
    if (Test-Path $configFile -and $ConnectionString) {
        try {
            $config = Get-Content $configFile | ConvertFrom-Json
            $config.ConnectionStrings.DefaultConnection = $ConnectionString
            $config | ConvertTo-Json -Depth 10 | Set-Content $configFile
            Write-ColorOutput "Configuration updated successfully" "Green"
        }
        catch {
            Write-ColorOutput "Warning: Could not update configuration file: $($_.Exception.Message)" "Yellow"
        }
    }
}

# Function to create Windows service
function Create-WindowsService {
    param(
        [string]$ServiceName,
        [string]$ExecutablePath,
        [string]$DisplayName,
        [string]$Description
    )
    
    Write-ColorOutput "Creating Windows service: $ServiceName" "Yellow"
    
    try {
        # Check if service already exists
        $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        
        if ($existingService) {
            Write-ColorOutput "Service $ServiceName already exists. Stopping and removing..." "Yellow"
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            & sc.exe delete $ServiceName
            Start-Sleep -Seconds 2
        }
        
        # Create new service
        & sc.exe create $ServiceName binPath= $ExecutablePath DisplayName= $DisplayName start= auto
        & sc.exe description $ServiceName $Description
        
        Write-ColorOutput "Windows service created successfully" "Green"
    }
    catch {
        Write-ColorOutput "Error creating Windows service: $($_.Exception.Message)" "Red"
        throw
    }
}

# Function to create desktop shortcut
function Create-DesktopShortcut {
    param(
        [string]$TargetPath,
        [string]$ShortcutName
    )
    
    try {
        $WshShell = New-Object -comObject WScript.Shell
        $desktopPath = [System.Environment]::GetFolderPath('Desktop')
        $shortcutPath = Join-Path $desktopPath "$ShortcutName.lnk"
        
        $Shortcut = $WshShell.CreateShortcut($shortcutPath)
        $Shortcut.TargetPath = $TargetPath
        $Shortcut.WorkingDirectory = Split-Path $TargetPath
        $Shortcut.Description = "Club Management Application"
        $Shortcut.Save()
        
        Write-ColorOutput "Desktop shortcut created: $shortcutPath" "Green"
    }
    catch {
        Write-ColorOutput "Warning: Could not create desktop shortcut: $($_.Exception.Message)" "Yellow"
    }
}

# Main deployment process
try {
    Write-ColorOutput "===========================================" "Cyan"
    Write-ColorOutput "Club Management Application Deployment" "Cyan"
    Write-ColorOutput "Environment: $Environment" "Cyan"
    Write-ColorOutput "Target Path: $TargetPath" "Cyan"
    Write-ColorOutput "===========================================" "Cyan"
    
    # Validate prerequisites
    Write-ColorOutput "Validating prerequisites..." "Yellow"
    
    # Check if .NET is installed
    try {
        $dotnetVersion = & dotnet --version
        Write-ColorOutput "Found .NET version: $dotnetVersion" "Green"
    }
    catch {
        throw ".NET SDK not found. Please install .NET 8.0 SDK."
    }
    
    # Check if SQL Server is available (if not skipping database)
    if (!$SkipDatabase) {
        try {
            $sqlResult = & sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION" -h -1
            Write-ColorOutput "SQL Server LocalDB is available" "Green"
        }
        catch {
            Write-ColorOutput "Warning: SQL Server LocalDB not available. Use -SkipDatabase to skip database deployment." "Yellow"
        }
    }
    
    # Create necessary directories
    Write-ColorOutput "Creating directories..." "Yellow"
    Ensure-Directory $TargetPath
    if (!$SkipBackup) {
        Ensure-Directory $BackupPath
    }
    
    # Backup existing application
    if (!$SkipBackup) {
        Backup-Application $TargetPath $BackupPath
    }
    
    # Deploy database
    if (!$SkipDatabase) {
        Deploy-Database $ConnectionString
    }
    
    # Find project file
    $projectFile = Get-ChildItem -Path $PSScriptRoot\.. -Filter "*.csproj" | Select-Object -First 1
    if (!$projectFile) {
        throw "Project file (.csproj) not found in parent directory"
    }
    
    Write-ColorOutput "Found project file: $($projectFile.Name)" "Green"
    
    # Publish application
    $publishPath = Publish-Application $projectFile.FullName $TargetPath $Environment
    
    # Update configuration
    if ($ConnectionString) {
        Update-Configuration $publishPath $Environment $ConnectionString
    }
    
    # Copy published files to target directory
    Write-ColorOutput "Copying files to target directory..." "Yellow"
    $appFiles = Get-ChildItem $publishPath
    foreach ($file in $appFiles) {
        $destPath = Join-Path $TargetPath $file.Name
        Copy-Item $file.FullName $destPath -Force
    }
    
    # Create desktop shortcut
    $exePath = Join-Path $TargetPath "Club Management Application.exe"
    if (Test-Path $exePath) {
        Create-DesktopShortcut $exePath "Club Management"
    }
    
    # Set permissions
    Write-ColorOutput "Setting permissions..." "Yellow"
    try {
        $acl = Get-Acl $TargetPath
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Users", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($accessRule)
        Set-Acl $TargetPath $acl
        Write-ColorOutput "Permissions set successfully" "Green"
    }
    catch {
        Write-ColorOutput "Warning: Could not set permissions: $($_.Exception.Message)" "Yellow"
    }
    
    # Create logs directory
    $logsPath = Join-Path $TargetPath "Logs"
    Ensure-Directory $logsPath
    
    # Create backups directory
    $appBackupsPath = Join-Path $TargetPath "Backups"
    Ensure-Directory $appBackupsPath
    
    Write-ColorOutput "===========================================" "Green"
    Write-ColorOutput "Deployment completed successfully!" "Green"
    Write-ColorOutput "Application Path: $TargetPath" "Green"
    Write-ColorOutput "Executable: $exePath" "Green"
    Write-ColorOutput "===========================================" "Green"
    
    # Display next steps
    Write-ColorOutput "Next Steps:" "Cyan"
    Write-ColorOutput "1. Update connection strings in appsettings.$Environment.json" "White"
    Write-ColorOutput "2. Configure email settings if needed" "White"
    Write-ColorOutput "3. Test the application" "White"
    Write-ColorOutput "4. Create user accounts" "White"
    
}
catch {
    Write-ColorOutput "===========================================" "Red"
    Write-ColorOutput "Deployment failed!" "Red"
    Write-ColorOutput "Error: $($_.Exception.Message)" "Red"
    Write-ColorOutput "===========================================" "Red"
    exit 1
}

# Pause to allow user to read output
Write-ColorOutput "Press any key to continue..." "Gray"
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")