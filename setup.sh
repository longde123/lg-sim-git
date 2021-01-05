#!/usr/bin/env bash

# Setup host machine for LG-Sim
# American Haval 2020


# Dependancies
sudo apt update
sudo apt install -y \
    gconf-service lib32gcc1 lib32stdc++6 libasound2 libc6 libc6-i386 libcairo2 libcap2 libcups2 \
    libdbus-1-3 libexpat1 libfontconfig1 libfreetype6 libgcc1 libgconf-2-4 libgdk-pixbuf2.0-0 \
    libgl1 libglib2.0-0 libglu1 libgtk2.0-0 libgtk-3-0 libnspr4 libnss3 libpango1.0-0 libstdc++6 \
    libx11-6 libxcomposite1 libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 libxrandr2 \
    libxrender1 libxtst6 zlib1g debconf libgtk2.0-0 libsoup2.4-1 libarchive13 libpng16-16 \
    git-lfs curl

# Nvidia Support
sudo apt install -y vulkan-utils

# Git LFS
git lfs install
git lfs pull

# Temp folder for unity Installer
mkdir ~/.unity3d
pushd ~/.unity3d

# Node JS
curl -sL https://deb.nodesource.com/setup_10.x | sudo -E bash -
sudo apt install -y nodejs
node -v 

# Unity Editor
curl -fLo UnitySetup https://beta.unity3d.com/download/f007ed779b7a/UnitySetup-2019.1.10f1
chmod +x UnitySetup
sudo ./UnitySetup --unattended --install-location=/opt/Unity --components=Unity

# Unity Hub Installer
wget https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage
chmod +x UnityHub.AppImage
./UnityHub.AppImage &

# Done with system installs
popd

# Web UI
pushd WebUI/
npm install
popd



