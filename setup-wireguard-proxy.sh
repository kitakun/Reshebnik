#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🚀 Setting up WireGuard + Tinyproxy from scratch...${NC}\n"

# --- Step 1: Update System ---
echo -e "${YELLOW}1. Updating system packages...${NC}"
echo -e "--------------------------------"
sudo apt update
sudo apt upgrade -y
echo -e "${GREEN}✅ System updated${NC}\n"

# --- Step 2: Install WireGuard ---
echo -e "${YELLOW}2. Installing WireGuard...${NC}"
echo -e "--------------------------------"
if ! command -v wg &> /dev/null; then
    sudo apt install wireguard -y
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ WireGuard installed successfully${NC}"
    else
        echo -e "${RED}❌ Failed to install WireGuard. Exiting.${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✅ WireGuard is already installed${NC}"
fi
echo ""

# --- Step 3: Install Tinyproxy ---
echo -e "${YELLOW}3. Installing Tinyproxy...${NC}"
echo -e "--------------------------------"
if ! command -v tinyproxy &> /dev/null; then
    sudo apt install tinyproxy -y
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Tinyproxy installed successfully${NC}"
    else
        echo -e "${RED}❌ Failed to install Tinyproxy. Exiting.${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✅ Tinyproxy is already installed${NC}"
fi
echo ""

# --- Step 4: Create WireGuard Configuration Directory ---
echo -e "${YELLOW}4. Creating WireGuard configuration directory...${NC}"
echo -e "--------------------------------"
sudo mkdir -p /etc/wireguard
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ /etc/wireguard directory ensured${NC}"
else
    echo -e "${RED}❌ Failed to create /etc/wireguard directory. Exiting.${NC}"
    exit 1
fi
echo ""

# --- Step 5: Create WireGuard Configuration ---
echo -e "${YELLOW}5. Creating WireGuard configuration...${NC}"
echo -e "--------------------------------"
# Backup existing config if it exists
if [ -f "/etc/wireguard/wg0.conf" ]; then
    echo -e "${YELLOW}Backing up existing /etc/wireguard/wg0.conf to /etc/wireguard/wg0.conf.backup${NC}"
    sudo cp /etc/wireguard/wg0.conf /etc/wireguard/wg0.conf.backup
fi

# Create WireGuard configuration
sudo tee /etc/wireguard/wg0.conf > /dev/null << 'EOF'
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
EOF

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ WireGuard configuration created${NC}"
else
    echo -e "${RED}❌ Failed to create WireGuard configuration. Exiting.${NC}"
    exit 1
fi
echo ""

# --- Step 6: Create Tinyproxy Configuration ---
echo -e "${YELLOW}6. Creating Tinyproxy configuration...${NC}"
echo -e "--------------------------------"
# Backup existing config if it exists
if [ -f "/etc/tinyproxy/tinyproxy.conf" ]; then
    echo -e "${YELLOW}Backing up existing /etc/tinyproxy/tinyproxy.conf to /etc/tinyproxy/tinyproxy.conf.backup${NC}"
    sudo cp /etc/tinyproxy/tinyproxy.conf /etc/tinyproxy/tinyproxy.conf.backup
fi

# Create Tinyproxy configuration
sudo tee /etc/tinyproxy/tinyproxy.conf > /dev/null << 'EOF'
# Tinyproxy Configuration - WireGuard Integration
User tinyproxy
Group tinyproxy
Port 1080
Listen 0.0.0.0
Bind 10.8.0.14
Timeout 600
Logfile "/var/log/tinyproxy/tinyproxy.log"
LogLevel Info
PidFile "/var/run/tinyproxy/tinyproxy.pid"

# Allow connections
Allow 127.0.0.1
Allow 10.8.0.0/24
Allow 0.0.0.0/0

# HTTPS support
ConnectPort 443
ConnectPort 563
ConnectPort 80
ConnectPort 21
ConnectPort 70
ConnectPort 210
EOF

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Tinyproxy configuration created${NC}"
else
    echo -e "${RED}❌ Failed to create Tinyproxy configuration. Exiting.${NC}"
    exit 1
fi
echo ""

# --- Step 7: Start WireGuard Service ---
echo -e "${YELLOW}7. Starting WireGuard service...${NC}"
echo -e "--------------------------------"
sudo systemctl start wg-quick@wg0
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ WireGuard service started${NC}"
else
    echo -e "${RED}❌ Failed to start WireGuard service. Checking logs...${NC}"
    sudo journalctl -u wg-quick@wg0 --since "1 minute ago"
    exit 1
fi
echo ""

# --- Step 8: Enable WireGuard Auto-Start ---
echo -e "${YELLOW}8. Enabling WireGuard auto-start...${NC}"
echo -e "--------------------------------"
sudo systemctl enable wg-quick@wg0
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ WireGuard auto-start enabled${NC}"
else
    echo -e "${RED}❌ Failed to enable WireGuard auto-start${NC}"
fi
echo ""

# --- Step 9: Start Tinyproxy Service ---
echo -e "${YELLOW}9. Starting Tinyproxy service...${NC}"
echo -e "--------------------------------"
sudo systemctl start tinyproxy
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Tinyproxy service started${NC}"
else
    echo -e "${RED}❌ Failed to start Tinyproxy service. Checking logs...${NC}"
    sudo journalctl -u tinyproxy --since "1 minute ago"
    exit 1
fi
echo ""

# --- Step 10: Enable Tinyproxy Auto-Start ---
echo -e "${YELLOW}10. Enabling Tinyproxy auto-start...${NC}"
echo -e "--------------------------------"
sudo systemctl enable tinyproxy
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Tinyproxy auto-start enabled${NC}"
else
    echo -e "${RED}❌ Failed to enable Tinyproxy auto-start${NC}"
fi
echo ""

# --- Step 11: Verify Services Status ---
echo -e "${YELLOW}11. Verifying services status...${NC}"
echo -e "--------------------------------"
echo -e "WireGuard status:"
sudo systemctl status wg-quick@wg0 --no-pager -l
echo ""

echo -e "Tinyproxy status:"
sudo systemctl status tinyproxy --no-pager -l
echo ""

# --- Step 12: Test Connectivity ---
echo -e "${YELLOW}12. Testing connectivity...${NC}"
echo -e "--------------------------------"
echo -e "Testing direct connection:"
DIRECT_IP=$(curl --max-time 10 -s http://icanhazip.com)
if [ $? -eq 0 ] && [ -n "$DIRECT_IP" ]; then
    echo -e "${GREEN}✅ Direct connection successful. IP: $DIRECT_IP${NC}"
else
    echo -e "${RED}❌ Direct connection failed${NC}"
fi
echo ""

echo -e "Testing proxy connection:"
PROXY_IP=$(curl --max-time 10 -s --proxy http://127.0.0.1:1080 http://icanhazip.com)
if [ $? -eq 0 ] && [ -n "$PROXY_IP" ]; then
    echo -e "${GREEN}✅ Proxy connection successful. IP: $PROXY_IP${NC}"
else
    echo -e "${RED}❌ Proxy connection failed${NC}"
fi
echo ""

# --- Step 13: Test HTTPS through Proxy ---
echo -e "${YELLOW}13. Testing HTTPS through proxy...${NC}"
echo -e "--------------------------------"
echo -e "Testing HTTPS proxy connection:"
HTTPS_PROXY_IP=$(curl --max-time 10 -s --proxy http://127.0.0.1:1080 https://httpbin.org/ip)
if [ $? -eq 0 ] && [ -n "$HTTPS_PROXY_IP" ]; then
    echo -e "${GREEN}✅ HTTPS proxy connection successful${NC}"
    echo -e "Response: $HTTPS_PROXY_IP"
else
    echo -e "${RED}❌ HTTPS proxy connection failed${NC}"
fi
echo ""

# --- Step 14: Show Network Information ---
echo -e "${YELLOW}14. Network information...${NC}"
echo -e "--------------------------------"
echo -e "WireGuard interface:"
ip addr show wg0 2>/dev/null || echo "WireGuard interface not found"
echo ""

echo -e "Tinyproxy listening ports:"
netstat -tuln | grep :1080 || echo "Tinyproxy not listening on port 1080"
echo ""

echo -e "WireGuard peer status:"
wg show 2>/dev/null || echo "WireGuard not running"
echo ""

# --- Final Summary ---
echo -e "${YELLOW}🎯 Setup Complete!${NC}"
echo -e "================"
echo -e "• WireGuard: ${GREEN}Installed and configured${NC}"
echo -e "• Tinyproxy: ${GREEN}Installed and configured${NC}"
echo -e "• Services: ${GREEN}Started and enabled${NC}"
echo -e "• Proxy URL: ${GREEN}http://127.0.0.1:1080${NC}"
echo ""

if [ "$DIRECT_IP" != "$PROXY_IP" ]; then
    echo -e "${GREEN}🎉 SUCCESS: Proxy is routing through WireGuard!${NC}"
    echo -e "• Direct IP: $DIRECT_IP"
    echo -e "• Proxy IP: $PROXY_IP"
else
    echo -e "${YELLOW}⚠️  WARNING: Proxy shows same IP as direct connection${NC}"
    echo -e "• Both showing: $DIRECT_IP"
    echo -e "• This might mean WireGuard server isn't routing internet traffic"
fi
echo ""

echo -e "${YELLOW}🔧 Next steps:${NC}"
echo -e "1. Update your app's ProxyUrl to: http://$(curl -s http://icanhazip.com):1080"
echo -e "2. Test your application with the proxy"
echo -e "3. Check logs if needed:"
echo -e "   • WireGuard: sudo journalctl -u wg-quick@wg0 -f"
echo -e "   • Tinyproxy: sudo journalctl -u tinyproxy -f"
echo ""

echo -e "${YELLOW}🚨 Important Notes:${NC}"
echo -e "• SSH access may be lost when WireGuard is active (routing all traffic)"
echo -e "• Use VPS web console to manage the server if SSH is lost"
echo -e "• To stop WireGuard: sudo systemctl stop wg-quick@wg0"
echo -e "• To stop Tinyproxy: sudo systemctl stop tinyproxy"
echo ""

echo -e "${GREEN}✅ Setup completed successfully!${NC}"
