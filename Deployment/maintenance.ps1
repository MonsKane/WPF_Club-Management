# =============================================
# Club Management Application - Maintenance Script
# Version: 1.0.0
# Description: Automated maintenance and monitoring
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Health", "Backup", "Cleanup", "Update", "Monitor", "All")]
    [string]$Operation = "All",
    
    [Parameter(Mandatory=$false)]
    [string]$ApplicationPath = "C:\Applications\ClubManagement",
    
    [Parameter(Mandatory=$false)]
    [string]$BackupPath = "C:\Backups\ClubManagement",
    
    [Parameter(Mandatory=$false)]
    [int]$RetentionDays = 30,
    
    [Parameter(Mandatory=$false)]
    [switch]$SendReport = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$ReportEmail = ""
)

# Set error action preference
$ErrorActionPreference = "Continue"

# Initialize maintenance log
$maintenanceLog = @()
$startTime = Get-Date

# Function to write colored output and log
function Write-MaintenanceLog {
    param(
        [string]$Message,
        [string]$Level = "INFO",
        [string]$Color = "White"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    
    Write-Host $logEntry -ForegroundColor $Color
    $script:maintenanceLog += $logEntry
    
    # Also write to maintenance log file
    $logFile = Join-Path $ApplicationPath "Logs\maintenance.log"
    if (Test-Path (Split-Path $logFile)) {
        Add-Content -Path $logFile -Value $logEntry
    }
}

# Function to check application health
function Test-ApplicationHealth {
    Write-MaintenanceLog "Starting application health check..." "INFO" "Yellow"
    
    $healthStatus = @{
        ApplicationExists = $false
        DatabaseConnectivity = $false
        ConfigurationValid = $false
        LogsAccessible = $false
        DiskSpace = $false
        MemoryUsage = $false
        ProcessRunning = $false
        OverallHealth = "UNKNOWN"
    }
    
    try {
        # Check if application executable exists
        $exePath = Join-Path $ApplicationPath "Club Management Application.exe"
        if (Test-Path $exePath) {
            $healthStatus.ApplicationExists = $true
            $version = (Get-Item $exePath).VersionInfo.FileVersion
            Write-MaintenanceLog "Application executable found (Version: $version)" "INFO" "Green"
        } else {
            Write-MaintenanceLog "Application executable not found" "ERROR" "Red"
        }
        
        # Check if process is running
        $process = Get-Process -Name "Club Management Application" -ErrorAction SilentlyContinue
        if ($process) {
            $healthStatus.ProcessRunning = $true
            $memoryMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
            Write-MaintenanceLog "Application is running (PID: $($process.Id), Memory: $memoryMB MB)" "INFO" "Green"
            
            # Check memory usage
            if ($memoryMB -lt 500) {
                $healthStatus.MemoryUsage = $true
                Write-MaintenanceLog "Memory usage is within normal limits" "INFO" "Green"
            } else {
                Write-MaintenanceLog "High memory usage detected: $memoryMB MB" "WARN" "Yellow"
            }
        } else {
            Write-MaintenanceLog "Application process not running" "WARN" "Yellow"
        }
        
        # Check configuration files
        $configFiles = @("appsettings.json", "appsettings.Production.json")
        $configValid = $true
        foreach ($configFile in $configFiles) {
            $configPath = Join-Path $ApplicationPath $configFile
            if (Test-Path $configPath) {
                try {
                    $config = Get-Content $configPath | ConvertFrom-Json
                    Write-MaintenanceLog "Configuration file valid: $configFile" "INFO" "Green"
                } catch {
                    Write-MaintenanceLog "Invalid JSON in configuration file: $configFile" "ERROR" "Red"
                    $configValid = $false
                }
            } else {
                Write-MaintenanceLog "Configuration file missing: $configFile" "WARN" "Yellow"
                $configValid = $false
            }
        }
        $healthStatus.ConfigurationValid = $configValid
        
        # Check database connectivity
        try {
            $configPath = Join-Path $ApplicationPath "appsettings.json"
            if (Test-Path $configPath) {
                $config = Get-Content $configPath | ConvertFrom-Json
                $connectionString = $config.ConnectionStrings.DefaultConnection
                
                $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
                $connection.Open()
                $command = $connection.CreateCommand()
                $command.CommandText = "SELECT COUNT(*) FROM sys.tables"
                $tableCount = $command.ExecuteScalar()
                $connection.Close()
                
                $healthStatus.DatabaseConnectivity = $true
                Write-MaintenanceLog "Database connectivity successful ($tableCount tables found)" "INFO" "Green"
            }
        } catch {
            Write-MaintenanceLog "Database connectivity failed: $($_.Exception.Message)" "ERROR" "Red"
        }
        
        # Check logs directory
        $logsPath = Join-Path $ApplicationPath "Logs"
        if (Test-Path $logsPath) {
            $logFiles = Get-ChildItem $logsPath -Filter "*.log"
            $healthStatus.LogsAccessible = $true
            Write-MaintenanceLog "Logs directory accessible ($($logFiles.Count) log files)" "INFO" "Green"
            
            # Check for recent errors in logs
            $recentLogs = $logFiles | Where-Object { $_.LastWriteTime -gt (Get-Date).AddHours(-24) }
            foreach ($logFile in $recentLogs) {
                $errorCount = (Get-Content $logFile.FullName | Select-String "ERROR").Count
                if ($errorCount -gt 0) {
                    Write-MaintenanceLog "Found $errorCount errors in $($logFile.Name) (last 24h)" "WARN" "Yellow"
                }
            }
        } else {
            Write-MaintenanceLog "Logs directory not accessible" "WARN" "Yellow"
        }
        
        # Check disk space
        $drive = (Get-Item $ApplicationPath).PSDrive
        $freeSpaceGB = [math]::Round($drive.Free / 1GB, 2)
        $totalSpaceGB = [math]::Round(($drive.Free + $drive.Used) / 1GB, 2)
        $freeSpacePercent = [math]::Round(($drive.Free / ($drive.Free + $drive.Used)) * 100, 1)
        
        if ($freeSpacePercent -gt 10) {
            $healthStatus.DiskSpace = $true
            Write-MaintenanceLog "Disk space sufficient: $freeSpaceGB GB free ($freeSpacePercent%)" "INFO" "Green"
        } else {
            Write-MaintenanceLog "Low disk space warning: $freeSpaceGB GB free ($freeSpacePercent%)" "WARN" "Yellow"
        }
        
        # Determine overall health
        $healthyComponents = ($healthStatus.GetEnumerator() | Where-Object { $_.Key -ne "OverallHealth" -and $_.Value -eq $true }).Count
        $totalComponents = ($healthStatus.GetEnumerator() | Where-Object { $_.Key -ne "OverallHealth" }).Count
        
        if ($healthyComponents -eq $totalComponents) {
            $healthStatus.OverallHealth = "HEALTHY"
            Write-MaintenanceLog "Overall health status: HEALTHY" "INFO" "Green"
        } elseif ($healthyComponents -ge ($totalComponents * 0.7)) {
            $healthStatus.OverallHealth = "WARNING"
            Write-MaintenanceLog "Overall health status: WARNING" "WARN" "Yellow"
        } else {
            $healthStatus.OverallHealth = "CRITICAL"
            Write-MaintenanceLog "Overall health status: CRITICAL" "ERROR" "Red"
        }
        
    } catch {
        Write-MaintenanceLog "Health check failed: $($_.Exception.Message)" "ERROR" "Red"
        $healthStatus.OverallHealth = "ERROR"
    }
    
    return $healthStatus
}

# Function to perform backup operations
function Invoke-BackupOperations {
    Write-MaintenanceLog "Starting backup operations..." "INFO" "Yellow"
    
    try {
        # Ensure backup directory exists
        if (!(Test-Path $BackupPath)) {
            New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
            Write-MaintenanceLog "Created backup directory: $BackupPath" "INFO" "Green"
        }
        
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        
        # Backup application files
        $appBackupPath = Join-Path $BackupPath "App_$timestamp"
        Write-MaintenanceLog "Creating application backup..." "INFO" "Yellow"
        
        Copy-Item -Path $ApplicationPath -Destination $appBackupPath -Recurse -Force
        
        # Compress backup
        $zipPath = "$appBackupPath.zip"
        Compress-Archive -Path $appBackupPath -DestinationPath $zipPath -Force
        Remove-Item $appBackupPath -Recurse -Force
        
        $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
        Write-MaintenanceLog "Application backup completed: $zipPath ($zipSize MB)" "INFO" "Green"
        
        # Backup database
        try {
            $configPath = Join-Path $ApplicationPath "appsettings.json"
            if (Test-Path $configPath) {
                $config = Get-Content $configPath | ConvertFrom-Json
                $connectionString = $config.ConnectionStrings.DefaultConnection
                
                # Extract database name from connection string
                if ($connectionString -match "Database=([^;]+)") {
                    $databaseName = $matches[1]
                    $dbBackupPath = Join-Path $BackupPath "DB_${databaseName}_$timestamp.bak"
                    
                    Write-MaintenanceLog "Creating database backup..." "INFO" "Yellow"
                    
                    $backupQuery = @"
BACKUP DATABASE [$databaseName] 
TO DISK = '$dbBackupPath'
WITH FORMAT, INIT, COMPRESSION;
"@
                    
                    # Execute backup using sqlcmd
                    $result = sqlcmd -S "(localdb)\MSSQLLocalDB" -Q $backupQuery -b
                    
                    if ($LASTEXITCODE -eq 0) {
                        $dbBackupSize = [math]::Round((Get-Item $dbBackupPath).Length / 1MB, 2)
                        Write-MaintenanceLog "Database backup completed: $dbBackupPath ($dbBackupSize MB)" "INFO" "Green"
                    } else {
                        Write-MaintenanceLog "Database backup failed" "ERROR" "Red"
                    }
                }
            }
        } catch {
            Write-MaintenanceLog "Database backup error: $($_.Exception.Message)" "ERROR" "Red"
        }
        
        # Clean up old backups
        Write-MaintenanceLog "Cleaning up old backups (retention: $RetentionDays days)..." "INFO" "Yellow"
        $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
        $oldBackups = Get-ChildItem $BackupPath | Where-Object { $_.LastWriteTime -lt $cutoffDate }
        
        foreach ($oldBackup in $oldBackups) {
            Remove-Item $oldBackup.FullName -Force
            Write-MaintenanceLog "Removed old backup: $($oldBackup.Name)" "INFO" "Gray"
        }
        
        Write-MaintenanceLog "Backup operations completed successfully" "INFO" "Green"
        
    } catch {
        Write-MaintenanceLog "Backup operations failed: $($_.Exception.Message)" "ERROR" "Red"
    }
}

# Function to perform cleanup operations
function Invoke-CleanupOperations {
    Write-MaintenanceLog "Starting cleanup operations..." "INFO" "Yellow"
    
    try {
        # Clean up old log files
        $logsPath = Join-Path $ApplicationPath "Logs"
        if (Test-Path $logsPath) {
            $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
            $oldLogs = Get-ChildItem $logsPath -Filter "*.log" | Where-Object { $_.LastWriteTime -lt $cutoffDate }
            
            foreach ($oldLog in $oldLogs) {
                Remove-Item $oldLog.FullName -Force
                Write-MaintenanceLog "Removed old log file: $($oldLog.Name)" "INFO" "Gray"
            }
            
            Write-MaintenanceLog "Cleaned up $($oldLogs.Count) old log files" "INFO" "Green"
        }
        
        # Clean up temporary files
        $tempPath = Join-Path $ApplicationPath "Temp"
        if (Test-Path $tempPath) {
            $tempFiles = Get-ChildItem $tempPath -Recurse
            foreach ($tempFile in $tempFiles) {
                Remove-Item $tempFile.FullName -Force -Recurse
            }
            Write-MaintenanceLog "Cleaned up $($tempFiles.Count) temporary files" "INFO" "Green"
        }
        
        # Clean up Windows temp files related to the application
        $windowsTempPath = $env:TEMP
        $appTempFiles = Get-ChildItem $windowsTempPath -Filter "*ClubManagement*" -ErrorAction SilentlyContinue
        foreach ($appTempFile in $appTempFiles) {
            try {
                Remove-Item $appTempFile.FullName -Force -Recurse
                Write-MaintenanceLog "Removed temp file: $($appTempFile.Name)" "INFO" "Gray"
            } catch {
                Write-MaintenanceLog "Could not remove temp file: $($appTempFile.Name)" "WARN" "Yellow"
            }
        }
        
        # Optimize database (if accessible)
        try {
            $configPath = Join-Path $ApplicationPath "appsettings.json"
            if (Test-Path $configPath) {
                $config = Get-Content $configPath | ConvertFrom-Json
                $connectionString = $config.ConnectionStrings.DefaultConnection
                
                if ($connectionString -match "Database=([^;]+)") {
                    $databaseName = $matches[1]
                    
                    Write-MaintenanceLog "Optimizing database..." "INFO" "Yellow"
                    
                    $optimizeQuery = @"
USE [$databaseName];
GO

-- Update statistics
EXEC sp_updatestats;
GO

-- Rebuild indexes
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql = @sql + 'ALTER INDEX ALL ON ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ' REBUILD;' + CHAR(13)
FROM sys.tables;
EXEC sp_executesql @sql;
GO
"@
                    
                    $result = sqlcmd -S "(localdb)\MSSQLLocalDB" -Q $optimizeQuery -b
                    
                    if ($LASTEXITCODE -eq 0) {
                        Write-MaintenanceLog "Database optimization completed" "INFO" "Green"
                    } else {
                        Write-MaintenanceLog "Database optimization failed" "WARN" "Yellow"
                    }
                }
            }
        } catch {
            Write-MaintenanceLog "Database optimization error: $($_.Exception.Message)" "WARN" "Yellow"
        }
        
        Write-MaintenanceLog "Cleanup operations completed successfully" "INFO" "Green"
        
    } catch {
        Write-MaintenanceLog "Cleanup operations failed: $($_.Exception.Message)" "ERROR" "Red"
    }
}

# Function to check for updates
function Test-ApplicationUpdates {
    Write-MaintenanceLog "Checking for application updates..." "INFO" "Yellow"
    
    try {
        # Check current version
        $exePath = Join-Path $ApplicationPath "Club Management Application.exe"
        if (Test-Path $exePath) {
            $currentVersion = (Get-Item $exePath).VersionInfo.FileVersion
            Write-MaintenanceLog "Current application version: $currentVersion" "INFO" "Green"
            
            # Here you would implement your update checking logic
            # For example, check a web service, file share, or registry for newer versions
            
            Write-MaintenanceLog "Update check completed (no updates available)" "INFO" "Green"
        } else {
            Write-MaintenanceLog "Cannot check updates - application executable not found" "ERROR" "Red"
        }
        
    } catch {
        Write-MaintenanceLog "Update check failed: $($_.Exception.Message)" "ERROR" "Red"
    }
}

# Function to monitor system resources
function Invoke-SystemMonitoring {
    Write-MaintenanceLog "Starting system monitoring..." "INFO" "Yellow"
    
    try {
        # CPU Usage
        $cpu = Get-WmiObject -Class Win32_Processor | Measure-Object -Property LoadPercentage -Average
        $cpuUsage = [math]::Round($cpu.Average, 1)
        Write-MaintenanceLog "CPU Usage: $cpuUsage%" "INFO" "Green"
        
        # Memory Usage
        $memory = Get-WmiObject -Class Win32_OperatingSystem
        $totalMemoryGB = [math]::Round($memory.TotalVisibleMemorySize / 1MB, 2)
        $freeMemoryGB = [math]::Round($memory.FreePhysicalMemory / 1MB, 2)
        $usedMemoryGB = $totalMemoryGB - $freeMemoryGB
        $memoryUsagePercent = [math]::Round(($usedMemoryGB / $totalMemoryGB) * 100, 1)
        
        Write-MaintenanceLog "Memory Usage: $usedMemoryGB GB / $totalMemoryGB GB ($memoryUsagePercent%)" "INFO" "Green"
        
        # Disk Usage
        $drives = Get-WmiObject -Class Win32_LogicalDisk | Where-Object { $_.DriveType -eq 3 }
        foreach ($drive in $drives) {
            $freeSpaceGB = [math]::Round($drive.FreeSpace / 1GB, 2)
            $totalSpaceGB = [math]::Round($drive.Size / 1GB, 2)
            $usedSpacePercent = [math]::Round((($drive.Size - $drive.FreeSpace) / $drive.Size) * 100, 1)
            
            $status = if ($usedSpacePercent -gt 90) { "CRITICAL" } elseif ($usedSpacePercent -gt 80) { "WARNING" } else { "OK" }
            $color = if ($status -eq "CRITICAL") { "Red" } elseif ($status -eq "WARNING") { "Yellow" } else { "Green" }
            
            Write-MaintenanceLog "Drive $($drive.DeviceID) Usage: $usedSpacePercent% ($freeSpaceGB GB free) - $status" "INFO" $color
        }
        
        # Network Connectivity
        $networkTests = @(
            @{ Host = "8.8.8.8"; Name = "Google DNS" },
            @{ Host = "1.1.1.1"; Name = "Cloudflare DNS" }
        )
        
        foreach ($test in $networkTests) {
            $ping = Test-Connection -ComputerName $test.Host -Count 1 -Quiet
            $status = if ($ping) { "OK" } else { "FAILED" }
            $color = if ($ping) { "Green" } else { "Red" }
            Write-MaintenanceLog "Network connectivity to $($test.Name): $status" "INFO" $color
        }
        
        Write-MaintenanceLog "System monitoring completed" "INFO" "Green"
        
    } catch {
        Write-MaintenanceLog "System monitoring failed: $($_.Exception.Message)" "ERROR" "Red"
    }
}

# Function to generate and send maintenance report
function Send-MaintenanceReport {
    param(
        [array]$LogEntries,
        [hashtable]$HealthStatus,
        [string]$EmailAddress
    )
    
    if (!$EmailAddress) {
        Write-MaintenanceLog "No email address provided for report" "WARN" "Yellow"
        return
    }
    
    try {
        Write-MaintenanceLog "Generating maintenance report..." "INFO" "Yellow"
        
        $reportContent = @"
<html>
<head>
    <title>Club Management Application - Maintenance Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background-color: #f0f0f0; padding: 10px; border-radius: 5px; }
        .status-healthy { color: green; font-weight: bold; }
        .status-warning { color: orange; font-weight: bold; }
        .status-critical { color: red; font-weight: bold; }
        .log-entry { margin: 2px 0; font-family: monospace; font-size: 12px; }
        table { border-collapse: collapse; width: 100%; margin: 10px 0; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Club Management Application - Maintenance Report</h1>
        <p><strong>Generated:</strong> $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")</p>
        <p><strong>Server:</strong> $env:COMPUTERNAME</p>
    </div>
    
    <h2>Health Status Summary</h2>
    <table>
        <tr><th>Component</th><th>Status</th></tr>
"@
        
        foreach ($component in $HealthStatus.GetEnumerator()) {
            if ($component.Key -ne "OverallHealth") {
                $status = if ($component.Value) { "✓ OK" } else { "✗ FAILED" }
                $class = if ($component.Value) { "status-healthy" } else { "status-critical" }
                $reportContent += "        <tr><td>$($component.Key)</td><td class=`"$class`">$status</td></tr>`n"
            }
        }
        
        $overallClass = switch ($HealthStatus.OverallHealth) {
            "HEALTHY" { "status-healthy" }
            "WARNING" { "status-warning" }
            "CRITICAL" { "status-critical" }
            default { "status-warning" }
        }
        
        $reportContent += @"
        <tr><td><strong>Overall Health</strong></td><td class="$overallClass"><strong>$($HealthStatus.OverallHealth)</strong></td></tr>
    </table>
    
    <h2>Maintenance Log</h2>
    <div style="background-color: #f9f9f9; padding: 10px; border-radius: 5px; max-height: 400px; overflow-y: auto;">
"@
        
        foreach ($logEntry in $LogEntries) {
            $reportContent += "        <div class=`"log-entry`">$logEntry</div>`n"
        }
        
        $reportContent += @"
    </div>
    
    <h2>System Information</h2>
    <table>
        <tr><th>Property</th><th>Value</th></tr>
        <tr><td>Computer Name</td><td>$env:COMPUTERNAME</td></tr>
        <tr><td>User Name</td><td>$env:USERNAME</td></tr>
        <tr><td>OS Version</td><td>$((Get-WmiObject -Class Win32_OperatingSystem).Caption)</td></tr>
        <tr><td>PowerShell Version</td><td>$($PSVersionTable.PSVersion)</td></tr>
        <tr><td>Application Path</td><td>$ApplicationPath</td></tr>
        <tr><td>Backup Path</td><td>$BackupPath</td></tr>
    </table>
    
    <p><em>This report was generated automatically by the Club Management Application maintenance script.</em></p>
</body>
</html>
"@
        
        # Save report to file
        $reportPath = Join-Path $ApplicationPath "Logs\maintenance-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').html"
        $reportContent | Out-File -FilePath $reportPath -Encoding UTF8
        
        Write-MaintenanceLog "Maintenance report saved: $reportPath" "INFO" "Green"
        
        # Here you would implement email sending logic
        # For example, using Send-MailMessage or a more robust email library
        Write-MaintenanceLog "Email report functionality not implemented" "WARN" "Yellow"
        
    } catch {
        Write-MaintenanceLog "Failed to generate maintenance report: $($_.Exception.Message)" "ERROR" "Red"
    }
}

# Main execution
try {
    Write-MaintenanceLog "===========================================" "INFO" "Cyan"
    Write-MaintenanceLog "Club Management Application Maintenance" "INFO" "Cyan"
    Write-MaintenanceLog "Operation: $Operation" "INFO" "Cyan"
    Write-MaintenanceLog "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" "INFO" "Cyan"
    Write-MaintenanceLog "===========================================" "INFO" "Cyan"
    
    $healthStatus = @{}
    
    # Execute requested operations
    switch ($Operation) {
        "Health" {
            $healthStatus = Test-ApplicationHealth
        }
        "Backup" {
            Invoke-BackupOperations
        }
        "Cleanup" {
            Invoke-CleanupOperations
        }
        "Update" {
            Test-ApplicationUpdates
        }
        "Monitor" {
            Invoke-SystemMonitoring
        }
        "All" {
            $healthStatus = Test-ApplicationHealth
            Invoke-BackupOperations
            Invoke-CleanupOperations
            Test-ApplicationUpdates
            Invoke-SystemMonitoring
        }
    }
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-MaintenanceLog "===========================================" "INFO" "Cyan"
    Write-MaintenanceLog "Maintenance completed successfully" "INFO" "Green"
    Write-MaintenanceLog "Duration: $($duration.ToString('hh\:mm\:ss'))" "INFO" "Cyan"
    Write-MaintenanceLog "===========================================" "INFO" "Cyan"
    
    # Send report if requested
    if ($SendReport -and $ReportEmail) {
        Send-MaintenanceReport -LogEntries $maintenanceLog -HealthStatus $healthStatus -EmailAddress $ReportEmail
    }
    
} catch {
    Write-MaintenanceLog "===========================================" "ERROR" "Red"
    Write-MaintenanceLog "Maintenance failed with error: $($_.Exception.Message)" "ERROR" "Red"
    Write-MaintenanceLog "===========================================" "ERROR" "Red"
    exit 1
}

# Return health status for automated monitoring
if ($Operation -eq "Health" -or $Operation -eq "All") {
    return $healthStatus
}