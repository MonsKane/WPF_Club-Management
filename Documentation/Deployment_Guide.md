# Club Management Application - Deployment Guide

## Table of Contents
1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Environment Setup](#environment-setup)
4. [Database Deployment](#database-deployment)
5. [Application Deployment](#application-deployment)
6. [Configuration](#configuration)
7. [Verification](#verification)
8. [Troubleshooting](#troubleshooting)
9. [Maintenance](#maintenance)
10. [Rollback Procedures](#rollback-procedures)

## Overview

This guide provides step-by-step instructions for deploying the Club Management Application in various environments. The application is built using .NET 8.0 WPF and requires SQL Server for data storage.

### Deployment Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Deployment Architecture                   │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │   WPF Client    │    │   SQL Server    │                │
│  │   Application   │◄──►│    Database     │                │
│  │                 │    │                 │                │
│  └─────────────────┘    └─────────────────┘                │
│           │                       │                        │
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │ Configuration   │    │   File System   │                │
│  │ Files (JSON)    │    │   (Logs, Temp)  │                │
│  └─────────────────┘    └─────────────────┘                │
└─────────────────────────────────────────────────────────────┘
```

## Prerequisites

### System Requirements

#### Minimum Requirements
- **Operating System**: Windows 10 (version 1809) or Windows Server 2019
- **RAM**: 4 GB minimum, 8 GB recommended
- **Storage**: 500 MB free disk space
- **Display**: 1024x768 minimum resolution
- **Network**: Internet connection for initial setup and updates

#### Software Dependencies
- **.NET 8.0 Runtime** (Desktop Runtime)
- **SQL Server** (LocalDB, Express, or Full version)
- **Visual C++ Redistributable** (latest version)

### Development Environment (for building from source)
- **Visual Studio 2022** (17.8 or later) or **Visual Studio Code**
- **.NET 8.0 SDK**
- **SQL Server Management Studio** (optional, for database management)

## Environment Setup

### 1. Install .NET 8.0 Runtime

```powershell
# Download and install .NET 8.0 Desktop Runtime
# Visit: https://dotnet.microsoft.com/download/dotnet/8.0

# Verify installation
dotnet --version
```

### 2. Install SQL Server

#### Option A: SQL Server LocalDB (Recommended for single-user)
```powershell
# Download SQL Server Express with LocalDB
# Visit: https://www.microsoft.com/sql-server/sql-server-downloads

# Verify LocalDB installation
sqllocaldb info
```

#### Option B: SQL Server Express (Multi-user)
```powershell
# Install SQL Server Express
# Configure for mixed-mode authentication
# Create database user for the application
```

### 3. Prepare Deployment Directory

```powershell
# Create application directory
New-Item -ItemType Directory -Path "C:\Applications\ClubManagement" -Force

# Create backup directory
New-Item -ItemType Directory -Path "C:\Backups\ClubManagement" -Force

# Set permissions
$acl = Get-Acl "C:\Applications\ClubManagement"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Users", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl "C:\Applications\ClubManagement" $acl
```

## Database Deployment

### Automated Database Deployment

1. **Navigate to deployment directory**:
   ```powershell
   cd "C:\Path\To\Club Management Application\Deployment"
   ```

2. **Run database deployment script**:
   ```powershell
   sqlcmd -S "(localdb)\MSSQLLocalDB" -i deploy-database.sql
   ```

3. **Verify database creation**:
   ```sql
   -- Connect to LocalDB and verify
   USE ClubManagementDB;
   SELECT name FROM sys.tables;
   ```

### Manual Database Setup

#### Step 1: Create Database
```sql
-- Create database
CREATE DATABASE ClubManagementDB;
GO

USE ClubManagementDB;
GO
```

#### Step 2: Create Application User
```sql
-- Create login and user
CREATE LOGIN clubapp_user WITH PASSWORD = 'SecurePassword123!';
CREATE USER clubapp_user FOR LOGIN clubapp_user;

-- Grant permissions
ALTER ROLE db_datareader ADD MEMBER clubapp_user;
ALTER ROLE db_datawriter ADD MEMBER clubapp_user;
ALTER ROLE db_ddladmin ADD MEMBER clubapp_user;
GO
```

#### Step 3: Run Schema Script
Execute the complete `deploy-database.sql` script to create all tables, indexes, and constraints.

#### Step 4: Initialize Test Data
The application uses `DatabaseInitializer.cs` to create test data automatically:
- **1 Admin account**: `admin@university.edu` / `admin123`
- **5 Member accounts** (password: `password123`):
  - `john.doe@university.edu` - Chairman of Computer Science Club
  - `jane.smith@university.edu` - Member of Computer Science Club
  - `mike.johnson@university.edu` - Member of Computer Science Club
  - `sarah.wilson@university.edu` - Admin of Photography Club
  - `david.brown@university.edu` - Member of Photography Club
- **2 Clubs**: Computer Science Club (3 members) and Photography Club (2 members)

### Database Configuration for Different Environments

#### Development Environment
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ClubManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

#### Production Environment
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=PROD-SQL-SERVER;Database=ClubManagementDB;User Id=clubapp_user;Password=SecurePassword123!;TrustServerCertificate=True;"
  }
}
```

#### Azure SQL Database
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Database=ClubManagementDB;User ID=clubapp_user;Password=SecurePassword123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

## Application Deployment

### Automated Deployment

#### Using PowerShell Script

1. **Basic deployment**:
   ```powershell
   .\deploy-application.ps1
   ```

2. **Custom deployment**:
   ```powershell
   .\deploy-application.ps1 -Environment "Production" -TargetPath "D:\Apps\ClubManagement" -ConnectionString "Server=PROD-SQL;Database=ClubManagementDB;Trusted_Connection=True;"
   ```

3. **Skip database deployment**:
   ```powershell
   .\deploy-application.ps1 -SkipDatabase
   ```

#### Script Parameters

| Parameter | Description | Default | Required |
|-----------|-------------|---------|----------|
| `Environment` | Target environment (Development, Production) | Production | No |
| `TargetPath` | Application installation directory | C:\Applications\ClubManagement | No |
| `BackupPath` | Backup directory for existing installations | C:\Backups\ClubManagement | No |
| `SkipBackup` | Skip backup of existing installation | False | No |
| `SkipDatabase` | Skip database deployment | False | No |
| `ConnectionString` | Custom database connection string | Empty | No |

### Manual Deployment

#### Step 1: Build Application

```powershell
# Navigate to project directory
cd "C:\Path\To\Club Management Application"

# Build and publish
dotnet publish "Club Management Application.csproj" `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output "C:\Applications\ClubManagement" `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true
```

#### Step 2: Copy Configuration Files

```powershell
# Copy configuration files
Copy-Item "appsettings.json" "C:\Applications\ClubManagement\"
Copy-Item "appsettings.Production.json" "C:\Applications\ClubManagement\"
```

#### Step 3: Create Directory Structure

```powershell
# Create required directories
New-Item -ItemType Directory -Path "C:\Applications\ClubManagement\Logs" -Force
New-Item -ItemType Directory -Path "C:\Applications\ClubManagement\Backups" -Force
New-Item -ItemType Directory -Path "C:\Applications\ClubManagement\Temp" -Force
```

### Creating Installation Package

#### Using ClickOnce Deployment

1. **Configure ClickOnce in Visual Studio**:
   - Right-click project → Publish
   - Choose publish location
   - Configure update settings
   - Set security permissions

2. **Publish application**:
   ```xml
   <!-- Add to .csproj file -->
   <PropertyGroup>
     <PublishUrl>\\server\share\ClubManagement\</PublishUrl>
     <IsWebBootstrapper>false</IsWebBootstrapper>
     <UpdateEnabled>true</UpdateEnabled>
     <UpdateMode>Foreground</UpdateMode>
     <UpdateInterval>7</UpdateInterval>
     <UpdateIntervalUnits>Days</UpdateIntervalUnits>
   </PropertyGroup>
   ```

#### Using Windows Installer (MSI)

1. **Install WiX Toolset**
2. **Create installer project**
3. **Configure installation components**
4. **Build MSI package**

## Configuration

### Application Configuration

#### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ClubManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "File": {
      "Path": "Logs/app-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Club Management System"
  },
  "SecuritySettings": {
    "PasswordPolicy": {
      "MinimumLength": 8,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigit": true,
      "RequireSpecialCharacter": true
    },
    "SessionSettings": {
      "TimeoutMinutes": 30,
      "MaxConcurrentSessions": 3
    },
    "LockoutPolicy": {
      "MaxFailedAttempts": 5,
      "LockoutDurationMinutes": 15
    }
  },
  "ApplicationSettings": {
    "ApplicationName": "Club Management System",
    "Version": "1.0.0",
    "MaxFileUploadSizeMB": 10,
    "SupportedFileTypes": [".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx"]
  }
}
```

### Environment-Specific Configuration

#### Development (appsettings.Development.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ClubManagementDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

#### Production (appsettings.Production.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Error"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=PROD-SQL-SERVER;Database=ClubManagementDB;User Id=clubapp_user;Password=SecurePassword123!;TrustServerCertificate=True;"
  }
}
```

### Email Configuration

#### Gmail SMTP Setup
1. Enable 2-factor authentication
2. Generate app-specific password
3. Update configuration:
   ```json
   {
     "EmailSettings": {
       "SmtpServer": "smtp.gmail.com",
       "SmtpPort": 587,
       "EnableSsl": true,
       "Username": "your-email@gmail.com",
       "Password": "your-16-char-app-password"
     }
   }
   ```

#### Outlook/Office 365 SMTP Setup
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp-mail.outlook.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "your-email@outlook.com",
    "Password": "your-password"
  }
}
```

#### Corporate Exchange Server
```json
{
  "EmailSettings": {
    "SmtpServer": "mail.company.com",
    "SmtpPort": 25,
    "EnableSsl": false,
    "Username": "domain\\username",
    "Password": "password"
  }
}
```

## Verification

### Post-Deployment Checklist

#### 1. Application Startup
- [ ] Application starts without errors
- [ ] Main window displays correctly
- [ ] All menu items are accessible
- [ ] No missing dependencies

#### 2. Database Connectivity
- [ ] Application connects to database
- [ ] All tables are created
- [ ] Sample data can be inserted
- [ ] Queries execute successfully

#### 3. Core Functionality
- [ ] User registration works
- [ ] User login/logout functions
- [ ] Club creation and management
- [ ] Event creation and management
- [ ] Report generation
- [ ] Email notifications (if configured)

#### 4. Security Features
- [ ] Password hashing works
- [ ] Role-based access control
- [ ] Session management
- [ ] Audit logging

### Testing Scripts

#### Database Connection Test
```powershell
# Test database connection
$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=ClubManagementDB;Trusted_Connection=True;"
try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "Database connection successful" -ForegroundColor Green
    $connection.Close()
} catch {
    Write-Host "Database connection failed: $($_.Exception.Message)" -ForegroundColor Red
}
```

#### Application Health Check
```powershell
# Check if application executable exists and is valid
$appPath = "C:\Applications\ClubManagement\Club Management Application.exe"
if (Test-Path $appPath) {
    $version = (Get-Item $appPath).VersionInfo.FileVersion
    Write-Host "Application found. Version: $version" -ForegroundColor Green
} else {
    Write-Host "Application executable not found" -ForegroundColor Red
}
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Application Won't Start

**Symptoms**: Application crashes on startup or shows error dialog

**Possible Causes**:
- Missing .NET runtime
- Corrupted configuration files
- Database connection issues
- Missing permissions

**Solutions**:
```powershell
# Check .NET installation
dotnet --version

# Verify configuration files
Test-Path "C:\Applications\ClubManagement\appsettings.json"

# Check application logs
Get-Content "C:\Applications\ClubManagement\Logs\app-*.log" | Select-Object -Last 50

# Reset configuration to defaults
Copy-Item "appsettings.json" "C:\Applications\ClubManagement\appsettings.json" -Force
```

#### 2. Database Connection Errors

**Symptoms**: "Cannot connect to database" errors

**Solutions**:
```powershell
# Test SQL Server service
Get-Service -Name "MSSQL*" | Where-Object {$_.Status -eq "Running"}

# Test LocalDB
sqllocaldb info
sqllocaldb start MSSQLLocalDB

# Verify connection string
# Check appsettings.json for correct server name and credentials
```

#### 3. Permission Errors

**Symptoms**: Access denied errors when reading/writing files

**Solutions**:
```powershell
# Fix application directory permissions
$path = "C:\Applications\ClubManagement"
$acl = Get-Acl $path
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Users", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl $path $acl

# Run application as administrator (temporary solution)
Start-Process "C:\Applications\ClubManagement\Club Management Application.exe" -Verb RunAs
```

#### 4. Email Configuration Issues

**Symptoms**: Email notifications not working

**Solutions**:
1. **Verify SMTP settings**:
   ```powershell
   # Test SMTP connection
   $smtp = New-Object Net.Mail.SmtpClient("smtp.gmail.com", 587)
   $smtp.EnableSsl = $true
   $smtp.Credentials = New-Object System.Net.NetworkCredential("username", "password")
   # Test connection (this will throw an exception if it fails)
   ```

2. **Check firewall settings**:
   - Ensure SMTP ports (25, 587, 465) are not blocked
   - Add application to Windows Firewall exceptions

3. **Verify email provider settings**:
   - Gmail: Use app-specific passwords
   - Outlook: Enable SMTP authentication
   - Corporate: Check with IT department

### Log Analysis

#### Application Logs Location
```
C:\Applications\ClubManagement\Logs\
├── app-20240101.log
├── app-20240102.log
└── ...
```

#### Common Log Patterns

**Successful startup**:
```
[2024-01-01 10:00:00] INFO: Application starting...
[2024-01-01 10:00:01] INFO: Database connection established
[2024-01-01 10:00:02] INFO: Services initialized
[2024-01-01 10:00:03] INFO: Application ready
```

**Database connection error**:
```
[2024-01-01 10:00:00] ERROR: Failed to connect to database
[2024-01-01 10:00:00] ERROR: System.Data.SqlClient.SqlException: A network-related or instance-specific error occurred...
```

**Configuration error**:
```
[2024-01-01 10:00:00] ERROR: Configuration file not found or invalid
[2024-01-01 10:00:00] ERROR: System.IO.FileNotFoundException: Could not find file 'appsettings.json'
```

## Maintenance

### Regular Maintenance Tasks

#### Daily Tasks
- [ ] Monitor application logs for errors
- [ ] Check disk space usage
- [ ] Verify backup completion

#### Weekly Tasks
- [ ] Review system performance
- [ ] Clean up old log files
- [ ] Update virus definitions
- [ ] Test backup restoration

#### Monthly Tasks
- [ ] Apply security updates
- [ ] Review user accounts and permissions
- [ ] Analyze usage statistics
- [ ] Update documentation

### Backup Procedures

#### Database Backup
```sql
-- Full database backup
BACKUP DATABASE ClubManagementDB
TO DISK = 'C:\Backups\ClubManagement\ClubManagementDB_Full.bak'
WITH FORMAT, INIT, COMPRESSION;

-- Differential backup
BACKUP DATABASE ClubManagementDB
TO DISK = 'C:\Backups\ClubManagement\ClubManagementDB_Diff.bak'
WITH DIFFERENTIAL, COMPRESSION;
```

#### Application Backup
```powershell
# Backup application files
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "C:\Backups\ClubManagement\App_Backup_$timestamp"
Copy-Item "C:\Applications\ClubManagement" $backupPath -Recurse

# Compress backup
Compress-Archive -Path $backupPath -DestinationPath "$backupPath.zip"
Remove-Item $backupPath -Recurse
```

#### Automated Backup Script
```powershell
# Create scheduled task for daily backups
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File C:\Scripts\backup-clubmanagement.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At "2:00 AM"
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
Register-ScheduledTask -TaskName "ClubManagement-Backup" -Action $action -Trigger $trigger -Settings $settings
```

### Update Procedures

#### Application Updates
1. **Download new version**
2. **Stop application** (if running as service)
3. **Backup current installation**
4. **Deploy new version**
5. **Update configuration** (if needed)
6. **Test functionality**
7. **Start application**

#### Database Updates
1. **Backup database**
2. **Apply migration scripts**
3. **Verify data integrity**
4. **Update application configuration**
5. **Test connectivity**

## Rollback Procedures

### Application Rollback

```powershell
# Stop current application
Stop-Process -Name "Club Management Application" -Force -ErrorAction SilentlyContinue

# Restore from backup
$latestBackup = Get-ChildItem "C:\Backups\ClubManagement" -Directory | Sort-Object CreationTime -Descending | Select-Object -First 1
Remove-Item "C:\Applications\ClubManagement" -Recurse -Force
Copy-Item $latestBackup.FullName "C:\Applications\ClubManagement" -Recurse

# Start application
Start-Process "C:\Applications\ClubManagement\Club Management Application.exe"
```

### Database Rollback

```sql
-- Restore database from backup
USE master;
GO

ALTER DATABASE ClubManagementDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

RESTORE DATABASE ClubManagementDB
FROM DISK = 'C:\Backups\ClubManagement\ClubManagementDB_Full.bak'
WITH REPLACE;
GO

ALTER DATABASE ClubManagementDB SET MULTI_USER;
GO
```

### Emergency Recovery

#### Complete System Recovery
1. **Restore database** from latest backup
2. **Reinstall application** from deployment package
3. **Restore configuration** files
4. **Verify all services** are running
5. **Test critical functionality**
6. **Notify users** of system restoration

#### Data Recovery
```sql
-- Recover deleted data (if within transaction log retention)
USE ClubManagementDB;
GO

-- Point-in-time recovery
RESTORE DATABASE ClubManagementDB_Recovery
FROM DISK = 'C:\Backups\ClubManagement\ClubManagementDB_Full.bak'
WITH MOVE 'ClubManagementDB' TO 'C:\Temp\ClubManagementDB_Recovery.mdf',
     MOVE 'ClubManagementDB_Log' TO 'C:\Temp\ClubManagementDB_Recovery.ldf',
     NORECOVERY;

RESTORE LOG ClubManagementDB_Recovery
FROM DISK = 'C:\Backups\ClubManagement\ClubManagementDB_Log.trn'
WITH STOPAT = '2024-01-01 14:30:00';
```

---

## Support and Contact Information

### Technical Support
- **Email**: support@clubmanagement.com
- **Phone**: +1 (555) 123-4567
- **Hours**: Monday-Friday, 9:00 AM - 5:00 PM EST

### Documentation
- **User Manual**: `Documentation/User_Manual.md`
- **API Documentation**: `Documentation/API_Documentation.md`
- **Technical Documentation**: `Documentation/Technical_Documentation.md`

### Version Information
- **Document Version**: 1.0.0
- **Last Updated**: January 2024
- **Application Version**: 1.0.0

---

*This deployment guide is part of the Club Management Application documentation suite. For the most current version, please check the project repository.*
