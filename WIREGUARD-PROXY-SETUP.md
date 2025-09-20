# WireGuard + Tinyproxy Setup Guide

This guide provides scripts and configurations to set up WireGuard VPN with Tinyproxy HTTP proxy on Ubuntu and Windows.

## 🚀 Quick Start

### Prerequisites

**Before running the setup scripts, you need to:**

1. **Get your WireGuard keys and server information:**
   - `YOUR_PRIVATE_KEY_HERE` - Your WireGuard private key
   - `YOUR_PUBLIC_KEY_HERE` - Your WireGuard server's public key
   - `YOUR_PRESHARED_KEY_HERE` - Your WireGuard preshared key (optional)
   - `YOUR_WIREGUARD_SERVER_IP` - Your WireGuard server's IP address

2. **Update the scripts with your actual values:**
   - Replace all placeholder values in the scripts before running
   - Or edit the generated configuration files after running the scripts

### Ubuntu/Linux Setup

1. **Update the script with your WireGuard details:**
   ```bash
   # Edit the script and replace placeholders
   nano setup-wireguard-proxy.sh
   ```

2. **Run the setup script:**
   ```bash
   chmod +x setup-wireguard-proxy.sh
   ./setup-wireguard-proxy.sh
   ```

3. **The script will:**
   - Install WireGuard and Tinyproxy
   - Create configuration files
   - Start and enable services
   - Test connectivity

### Windows Setup

1. **Update the script with your WireGuard details:**
   ```powershell
   # Edit the script and replace placeholders
   notepad setup-wireguard-proxy.ps1
   ```

2. **Run PowerShell as Administrator:**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   .\setup-wireguard-proxy.ps1
   ```

3. **The script will:**
   - Install WireGuard via Chocolatey
   - Create configuration files
   - Create management scripts

## 📁 Files Created

### Ubuntu/Linux
- `/etc/wireguard/wg0.conf` - WireGuard configuration
- `/etc/tinyproxy/tinyproxy.conf` - Tinyproxy configuration
- Services enabled and started

### Windows
- `%USERPROFILE%\WireGuard\wg0.conf` - WireGuard configuration
- `%USERPROFILE%\appsettings.Development.json` - Application configuration
- `%USERPROFILE%\start-wireguard.ps1` - Start script
- `%USERPROFILE%\stop-wireguard.ps1` - Stop script

## ⚙️ Configuration

### WireGuard Configuration

The WireGuard config routes all traffic through the VPN:
```ini
[Interface]
PrivateKey = YOUR_PRIVATE_KEY_HERE
Address = 10.8.0.14/24
DNS = 1.1.1.1
MTU = 1420

[Peer]
PublicKey = YOUR_PUBLIC_KEY_HERE
PresharedKey = YOUR_PRESHARED_KEY_HERE
AllowedIPs = 0.0.0.0/0
PersistentKeepalive = 25
Endpoint = YOUR_WIREGUARD_SERVER_IP:51820
```

### Tinyproxy Configuration

The Tinyproxy config binds to the WireGuard interface:
```ini
User tinyproxy
Group tinyproxy
Port 1080
Listen 0.0.0.0
Bind 10.8.0.14
Timeout 600
Logfile "/var/log/tinyproxy/tinyproxy.log"
LogLevel Info
PidFile "/var/run/tinyproxy/tinyproxy.pid"

Allow 127.0.0.1
Allow 10.8.0.0/24
Allow 0.0.0.0/0

ConnectPort 443
ConnectPort 563
ConnectPort 80
ConnectPort 21
ConnectPort 70
ConnectPort 210
```

## 🔧 Management Commands

### Ubuntu/Linux

```bash
# Start services
sudo systemctl start wg-quick@wg0
sudo systemctl start tinyproxy

# Stop services
sudo systemctl stop wg-quick@wg0
sudo systemctl stop tinyproxy

# Check status
sudo systemctl status wg-quick@wg0
sudo systemctl status tinyproxy

# View logs
sudo journalctl -u wg-quick@wg0 -f
sudo journalctl -u tinyproxy -f
```

### Windows

```powershell
# Start WireGuard
.\start-wireguard.ps1

# Stop WireGuard
.\stop-wireguard.ps1
```

## 🧪 Testing

### Test Direct Connection
```bash
curl http://icanhazip.com
```

### Test Proxy Connection
```bash
curl --proxy http://127.0.0.1:1080 http://icanhazip.com
```

### Test HTTPS through Proxy
```bash
curl --proxy http://127.0.0.1:1080 https://httpbin.org/ip
```

## 🎯 Application Configuration

Update your application's `appsettings.Development.json` with your actual values:

```json
{
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
    "ProxyUrl": "http://YOUR_SERVER_IP:1080",
    "ProxyType": "Http",
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
```

### Required Placeholders

Replace these placeholders with your actual values:

- **Database**: `YOUR_DATABASE`, `YOUR_USERNAME`, `YOUR_PASSWORD`
- **JWT**: `YOUR_JWT_SECRET_KEY_HERE`, `YOUR_JWT_ISSUER_HERE`
- **ClickHouse**: `YOUR_CLICKHOUSE_HOST`, `YOUR_CLICKHOUSE_USERNAME`, `YOUR_CLICKHOUSE_PASSWORD`, `YOUR_CLICKHOUSE_DATABASE`
- **Email**: `YOUR_EMAIL_LOGIN`, `YOUR_EMAIL_PASSWORD`, `YOUR_EMAIL_ONETIME_PASSWORD`, `YOUR_EMAIL_NAME`
- **OpenAI**: `YOUR_OPENAI_API_KEY_HERE`
- **Server**: `YOUR_SERVER_IP` (your Ubuntu server's IP address)
- **WireGuard**: `YOUR_PRIVATE_KEY_HERE`, `YOUR_PUBLIC_KEY_HERE`, `YOUR_PRESHARED_KEY_HERE`, `YOUR_WIREGUARD_SERVER_IP`

## 🚨 Important Notes

### SSH Access Warning
- **With `AllowedIPs = 0.0.0.0/0`**: SSH access will be lost (all traffic routed through VPN)
- **With `AllowedIPs = 10.8.0.0/24`**: SSH access preserved (only VPN network routed)

### Recovery
If you lose SSH access:
1. Use VPS web console
2. Stop WireGuard: `sudo systemctl stop wg-quick@wg0`
3. Or reboot the server

### Safe Configuration
For development/testing, use:
```ini
AllowedIPs = 10.8.0.0/24
```
This preserves SSH access while allowing VPN network connectivity.

## 🔍 Troubleshooting

### WireGuard Not Connecting
```bash
# Check WireGuard status
wg show

# Check logs
sudo journalctl -u wg-quick@wg0 -f

# Test server connectivity
ping 194.147.115.107
nc -zv 194.147.115.107 51820
```

### Tinyproxy Not Working
```bash
# Check if listening
netstat -tuln | grep :1080

# Check logs
sudo journalctl -u tinyproxy -f

# Test configuration
sudo tinyproxy -d -c /etc/tinyproxy/tinyproxy.conf
```

### Proxy Shows Same IP
This usually means:
1. WireGuard server isn't routing internet traffic
2. WireGuard connection isn't established
3. Tinyproxy isn't binding to WireGuard interface

## 📋 Templates

Use the provided templates to create custom configurations:
- `wireguard.conf.template` - WireGuard configuration template
- `tinyproxy.conf.template` - Tinyproxy configuration template

## 🎉 Success Indicators

- ✅ WireGuard interface shows IP: `ip addr show wg0`
- ✅ Tinyproxy listening on port 1080: `netstat -tuln | grep :1080`
- ✅ Proxy returns different IP than direct connection
- ✅ Application can connect through proxy

## 📞 Support

If you encounter issues:
1. Check the logs for error messages
2. Verify network connectivity
3. Test with safe configuration first
4. Use VPS web console for recovery
