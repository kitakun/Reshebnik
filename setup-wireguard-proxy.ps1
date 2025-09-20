# WireGuard + Tinyproxy Setup Script for Windows
# This script sets up WireGuard and Tinyproxy on Windows

# Colors for output
$Green = "Green"
$Yellow = "Yellow"
$Red = "Red"

Write-Host "🚀 Setting up WireGuard + Tinyproxy on Windows..." -ForegroundColor $Yellow
Write-Host ""

# --- Step 1: Check if running as Administrator ---
Write-Host "1. Checking administrator privileges..." -ForegroundColor $Yellow
Write-Host "--------------------------------" -ForegroundColor $Yellow
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ This script requires administrator privileges. Please run as Administrator." -ForegroundColor $Red
    exit 1
}
Write-Host "✅ Running as Administrator" -ForegroundColor $Green
Write-Host ""

# --- Step 2: Install Chocolatey (if not installed) ---
Write-Host "2. Installing Chocolatey package manager..." -ForegroundColor $Yellow
Write-Host "--------------------------------" -ForegroundColor $Yellow
if (!(Get-Command choco -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Chocolatey..."
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Chocolatey installed successfully" -ForegroundColor $Green
    } else {
        Write-Host "❌ Failed to install Chocolatey. Exiting." -ForegroundColor $Red
        exit 1
    }
} else {
    Write-Host "✅ Chocolatey is already installed" -ForegroundColor $Green
}
Write-Host ""

# --- Step 3: Install WireGuard ---
Write-Host "3. Installing WireGuard..." -ForegroundColor $Yellow
Write-Host "--------------------------------" -ForegroundColor $Yellow
if (!(Get-Command wg -ErrorAction SilentlyContinue)) {
    Write-Host "Installing WireGuard..."
    choco install wireguard -y
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ WireGuard installed successfully" -ForegroundColor $Green
    } else {
        Write-Host "❌ Failed to install WireGuard. Exiting." -ForegroundColor $Red
        exit 1
    }
} else {
    Write-Host "✅ WireGuard is already installed" -ForegroundColor $Green
}
Write-Host ""

# --- Step 4: Install Tinyproxy (using alternative method) ---
Write-Host "4. Setting up Tinyproxy alternative..." -ForegroundColor $Yellow
Write-Host "--------------------------------" -ForegroundColor $Yellow
Write-Host "Note: Tinyproxy is not available for Windows. Using alternative approach:" -ForegroundColor $Yellow
Write-Host "• Install a Windows-compatible HTTP proxy (like CCProxy or 3proxy)" -ForegroundColor $Yellow
Write-Host "• Or use Windows built-in proxy features" -ForegroundColor $Yellow
Write-Host ""

# --- Step 5: Create WireGuard Configuration Directory ---
Write-Host "5. Creating WireGuard configuration directory..." -ForegroundColor $Yellow
Write-Host "--------------------------------" -ForegroundColor $Yellow
$WireGuardConfigDir = "$env:USERPROFILE\WireGuard"
if (!(Test-Path $WireGuardConfigDir)) {
    New-Item -ItemType Directory -Path $WireGuardConfigDir -Force | Out-Null
    Write-Host "✅ WireGuard config directory created: $WireGuardConfigDir" -ForegroundColor $Green
} else {
    Write-Host "✅ WireGuard config directory already exists: $WireGuardConfigDir" -ForegroundColor $Green
}
Write-Host ""

# --- Step 6: Create WireGuard Configuration File ---
Write-Host "6. Creating WireGuard configuration..." -ForegroundColor $Yellow
Write-Host "--------------------------------" -ForegroundColor $Yellow
$WireGuardConfigPath = "$WireGuardConfigDir\wg0.conf"
$WireGuardConfig = @"
[Interface]
PrivateKey = YOUR_PRIVATE_KEY_HERE
Address = 10.8.0.14/24
DNS = 1.1.1.1
MTU = 1420

[Peer]
PublicKey = YOUR_PUBLIC_KEY_HERE
PresharedKey = YOUR_PRESHARED_KEY_HERE
# Route all traffic through VPN
AllowedIPs = 0.0.0.0/0
PersistentKeepalive = 25
Endpoint = 194.147.115.107:51820
"@

# Backup existing config if it exists
if (Test-Path $WireGuardConfigPath) {
    $BackupPath = "$WireGuardConfigPath.backup"
    Copy-Item $WireGuardConfigPath $BackupPath -Force
    Write-Host "✅ Backed up existing config to: $BackupPath" -ForegroundColor $Green
}

# Write new config
$WireGuardConfig | Out-File -FilePath $WireGuardConfigPath -Encoding UTF8
if (Test-Path $WireGuardConfigPath) {
    Write-Host "✅ WireGuard configuration created: $WireGuardConfigPath" -ForegroundColor $Green
} else {
    Write-Host "❌ Failed to create WireGuard configuration. Exiting." -ForegroundColor $Red
    exit 1
}
Write-Host ""

# --- Step 7: Create Application Configuration ---
Write-Host "7. Creating application configuration..." -ForegroundColor $Yellow
Write-Host "--------------------------------" -ForegroundColor $Yellow
$AppConfigPath = "$env:USERPROFILE\appsettings.Development.json"
$AppConfig = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=YOUR_DATABASE;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Key": "YOUR_JWT_SECRET_KEY_HERE",
    "Issuer": "YOUR_JWT_ISSUER_HERE"
  },
  "Clickhouse": {
    "prefix": "dev",
    "host": "YOUR_CLICKHOUSE_HOST",
    "port":"8123",
    "username":"YOUR_CLICKHOUSE_USERNAME",
    "password":"YOUR_CLICKHOUSE_PASSWORD",
    "dbName":"YOUR_CLICKHOUSE_DATABASE"
  },
  "Email": {
    "login": "YOUR_EMAIL_LOGIN",
    "password": "YOUR_EMAIL_PASSWORD",
    "onetimepass": "YOUR_EMAIL_ONETIME_PASSWORD",
    "name": "YOUR_EMAIL_NAME"
  },
  "Gpt": {
    "ApiKey": "YOUR_OPENAI_API_KEY_HERE",
    "BaseUrl": "https://api.openai.com/v1",
    "ProxyUrl": "http://YOUR_UBUNTU_SERVER_IP:1080",
    "ProxyType": "Http",
    "ProxyUsername": null,
    "ProxyPassword": null,
    "ProxyBypassOnLocal": true,
    "UseAllowList": true,
    "ProxyAllowList": [
      "api.openai.com",
      "*.openai.com",
      "chatgpt.com",
      "*.chatgpt.com",
      "platform.openai.com",
      "*.platform.openai.com"
    ]
  },
  "WireGuard": {
    "Interface": {
      "PrivateKey": "YOUR_PRIVATE_KEY_HERE",
      "Address": "10.8.0.14/24",
      "DNS": "1.1.1.1",
      "MTU": 1420
    },
    "Peer": {
      "PublicKey": "YOUR_PUBLIC_KEY_HERE",
      "PresharedKey": "YOUR_PRESHARED_KEY_HERE",
      "AllowedIPs": "0.0.0.0/0",
      "PersistentKeepalive": 25,
      "Endpoint": "YOUR_WIREGUARD_SERVER_IP:51820"
    }
  }
}
"@

$AppConfig | Out-File -FilePath $AppConfigPath -Encoding UTF8
if (Test-Path $AppConfigPath) {
    Write-Host "✅ Application configuration created: $AppConfigPath" -ForegroundColor $Green
} else {
    Write-Host "❌ Failed to create application configuration. Exiting." -ForegroundColor $Red
    exit 1
}
Write-Host ""

# --- Step 8: Create Management Scripts ---
Write-Host "8. Creating management scripts..." -ForegroundColor $Yellow
Write-Host "--------------------------------" -ForegroundColor $Yellow

# Create start script
$StartScript = @"
# Start WireGuard
Write-Host "Starting WireGuard..." -ForegroundColor Yellow
& "C:\Program Files\WireGuard\wireguard.exe" /installtunnelservice "$env:USERPROFILE\WireGuard\wg0.conf"
if (`$LASTEXITCODE -eq 0) {
    Write-Host "✅ WireGuard started successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to start WireGuard" -ForegroundColor Red
}
"@
$StartScript | Out-File -FilePath "$env:USERPROFILE\start-wireguard.ps1" -Encoding UTF8

# Create stop script
$StopScript = @"
# Stop WireGuard
Write-Host "Stopping WireGuard..." -ForegroundColor Yellow
& "C:\Program Files\WireGuard\wireguard.exe" /uninstalltunnelservice wg0
if (`$LASTEXITCODE -eq 0) {
    Write-Host "✅ WireGuard stopped successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to stop WireGuard" -ForegroundColor Red
}
"@
$StopScript | Out-File -FilePath "$env:USERPROFILE\stop-wireguard.ps1" -Encoding UTF8

Write-Host "✅ Management scripts created:" -ForegroundColor $Green
Write-Host "  • start-wireguard.ps1" -ForegroundColor $Green
Write-Host "  • stop-wireguard.ps1" -ForegroundColor $Green
Write-Host ""

# --- Step 9: Test Network Connectivity ---
Write-Host "9. Testing network connectivity..." -ForegroundColor $Yellow
Write-Host "--------------------------------" -ForegroundColor $Yellow
try {
    $DirectIP = (Invoke-WebRequest -Uri "http://icanhazip.com" -TimeoutSec 10).Content.Trim()
    Write-Host "✅ Direct connection successful. IP: $DirectIP" -ForegroundColor $Green
} catch {
    Write-Host "❌ Direct connection failed: $($_.Exception.Message)" -ForegroundColor $Red
}
Write-Host ""

# --- Final Summary ---
Write-Host "🎯 Windows Setup Complete!" -ForegroundColor $Yellow
Write-Host "========================" -ForegroundColor $Yellow
Write-Host "• WireGuard: Installed and configured" -ForegroundColor $Green
Write-Host "• Configuration: $WireGuardConfigPath" -ForegroundColor $Green
Write-Host "• App Config: $AppConfigPath" -ForegroundColor $Green
Write-Host "• Management Scripts: Created in $env:USERPROFILE" -ForegroundColor $Green
Write-Host ""

Write-Host "🔧 Next steps:" -ForegroundColor $Yellow
Write-Host "1. Update ProxyUrl in appsettings.Development.json with your Ubuntu server IP" -ForegroundColor $Yellow
Write-Host "2. Run start-wireguard.ps1 to start WireGuard" -ForegroundColor $Yellow
Write-Host "3. Test your application" -ForegroundColor $Yellow
Write-Host "4. Use stop-wireguard.ps1 to stop WireGuard if needed" -ForegroundColor $Yellow
Write-Host ""

Write-Host "🚨 Important Notes:" -ForegroundColor $Yellow
Write-Host "• WireGuard will route ALL traffic through the VPN" -ForegroundColor $Yellow
Write-Host "• This may affect your internet connection" -ForegroundColor $Yellow
Write-Host "• Use stop-wireguard.ps1 to restore normal internet" -ForegroundColor $Yellow
Write-Host "• For proxy functionality, you need to set up a proxy server on your Ubuntu server" -ForegroundColor $Yellow
Write-Host ""

Write-Host "✅ Windows setup completed successfully!" -ForegroundColor $Green
