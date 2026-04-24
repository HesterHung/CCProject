# Hybrid Cloud Gaming - Research Build (Windows Deployment)

This repository contains the signaling server, Unity standalone build, and a web-based analysis client designed for evaluating Hybrid Upscaling performance, E2E latency, and WebRTC network statistics. 

## Network Setup (Crucial)
Before running the server or the client, you must configure your Windows Firewall to allow the necessary WebRTC and WebSocket traffic.

1. Open **Windows Defender Firewall with Advanced Security**.
2. Select **Inbound Rules** and create a **New Rule**.
3. Allow the following ports:
   * **TCP:** `80`, `8080` (For the web server and WebSocket signaling).
   * **UDP:** `1024-65535` (For WebRTC video streaming and data channels).

## 1. Start the Signaling Server
Open a Command Prompt or PowerShell window and navigate to your server directory to initialize the Node application.

```bash
cd ClientWeb
node server.js
```

## 2. Launch the Web Client (Analysis Dashboard)
*Important Note:* Ensure the WebSocket URL in your `ClientWeb/public/index.html` file matches the IP of the machine running the Node server. Currently, it points to `ws://100.70.127.65:8080`. 

Choose your access method:
* **Localhost Testing:** Navigate to the `ClientWeb/public/` directory and simply double-click `index.html` to open it in your browser.
* **Remote Testing:** On a separate machine, open a web browser and enter the IP address of your host machine along with the server port (e.g., `http://YOUR_WINDOWS_IP:8080`).

## 3. Run the Unity Game Build
Keep the Node server running in the background and initialize the game environment.

1. Extract the contents of `Window-Build-new.zip` to a local folder.
2. Open the extracted folder and double-click **`My Project.exe`**.
3. Wait for the application to initialize and establish a WebRTC connection with the signaling server. The video stream will automatically appear in your web client once connected.

## 4. Usage & Data Collection
Once the stream is live, the web client functions as an analysis dashboard. You can interact with the game using standard mouse and keyboard controls (click the video to lock the pointer).

**Research Controls:**
* **Mode Switching:** Click the dashboard buttons (Static Bilinear, Static AI, Hybrid) to switch rendering modes dynamically during gameplay.
* **Measure Latency:** Press the **`Space`** key to trigger a ping-pong RTT measurement. The result will display on the dashboard.
* **Capture Frame (SSIM):** Press the **`F`** key or click the "Capture SSIM Frame" button to download a PNG snapshot of the current frame for structural similarity analysis. The file name will automatically label the current research mode.
* **Reset Stats:** Click the red "Reset Stats" button to clear motion scores, mode switch counts, and WebRTC averages (Decode/Jitter) for a new testing run.