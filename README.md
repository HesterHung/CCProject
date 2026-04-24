# Hybrid Cloud Gaming - Research Build (Windows Deployment)

This repository contains the signaling server, Unity standalone build, and a web-based analysis client designed for evaluating **Hybrid Upscaling performance**, **E2E latency**, and **WebRTC network statistics**.

---

## Network Setup (Crucial)
Before running the server or the client, you must configure your Windows Firewall to allow the necessary WebRTC and WebSocket traffic.

1.  Open **Windows Defender Firewall with Advanced Security**.
2.  Select **Inbound Rules** and create a **New Rule**.
3.  Allow the following ports:
    * **TCP:** `80`, `8080` (For the web server and WebSocket signaling).
    * **UDP:** `1024-65535` (For WebRTC video streaming and data channels).

---

## 1. Start the Signaling Server
Open a Command Prompt or PowerShell window, navigate to your server directory, and initialize the Node.js application.

```bash
cd ClientWeb
node server.js
```

---

## 2. Configure & Launch the Web Client
Before opening the analysis dashboard, you **must** update the client to point to your server's current IP address.

### Step A: Update the IP Address
1.  Open `ClientWeb/public/index.html` in a text editor (like Notepad++ or VS Code).
2.  Search for `const ws`.
3.  Replace the hardcoded IP (`100.70.127.65`) with the **IP address of the machine running the Node server**:
    ```javascript
    // CHANGE THIS LINE to your server's IP
    const ws = new WebSocket(`ws://YOUR_SERVER_IP_HERE:8080`);
    ```
4.  Save the file.

### Step B: Access the Dashboard
* **Localhost Testing:** Simply double-click your updated `index.html` to open it in a browser.
* **Remote Testing:** On a separate machine, enter the host IP in your browser: `http://YOUR_SERVER_IP:8080`.

---

## 3. Run the Unity Game Build
Keep the Node server running in the background while you initialize the game environment.

1.  Extract the contents of `Window-Build-new.zip` to a local folder.
2.  Open the folder and double-click **`My Project.exe`**.
3.  Wait for the application to initialize. The video stream will automatically appear in your web client once the WebRTC handshake is complete.

---

## 4. Usage & Data Collection
Once the stream is live, you can interact with the game using mouse and keyboard (click the video to lock the pointer).

### Research Controls
* **Mode Switching:** Use the dashboard buttons to toggle between **Static Bilinear**, **Static AI**, and **Hybrid** rendering modes.
* **Measure Latency:** Press the **`Space`** key to trigger a ping-pong RTT measurement. The result will update on the dashboard.
* **Capture Frame (SSIM):** Press the **`F`** key or click the 📸 button to download a PNG snapshot. The filename will automatically include the current research mode for easier data sorting.
* **Reset Stats:** Use the red **Reset Stats** button to clear motion scores and WebRTC averages for a fresh data collection run.

> **Note:** If the connection fails, double-check that your server IP in `index.html` matches your current network configuration exactly. Also, you have to launch these thing (1. server.js, 2. index.html, 3. My project.exe) in order.