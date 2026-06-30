# Project Setup: AWS EC2 & Unity Web Streaming


This README provides a step-by-step guide to deploying the Node.js signaling server on an AWS EC2 instance and connecting a Unity Linux build to a local web client.

---

## Prerequisites
* **AWS Account:** Access to launch instances with a specific AMI.
* **SSH Client:** [MobaXterm](https://mobaxterm.mobatek.net/) (recommended) or any terminal with SSH capabilities.
* **Local Environment:** Web browser and the project files located on your machine.

---

## Deployment Steps

### 1. Launch the EC2 Instance
* Log into your AWS Management Console.
* Launch a new EC2 instance using the **provided AMI (ID: ami-0994bdb529122d33f)** (inside the AMI, you should get the needed folders and needed security group rule. In case, you fail to access to the AMI, you can still found that 2 .zip files in our github repo in this branch 'main', which you can unzip them. And with new Security Group of SSH: TCP Port 22; Custom TCP: Port 8080; Custom UDP: Ports 1024 - 65535).

### 2. Start the Node.js Server
Open MobaXterm and connect to your instance using the public IP. Once connected, execute the following commands:

```bash
# Navigate to the web client directory
cd ClientWeb/

# Start the signaling server
node server.js
```

### 3. Configure the Web Client
Before opening the interface, you must point the client to your specific AWS instance:
1.  Locate `index.html` inside the `./ClientWeb/` folder on your **local machine**.
2.  Open the file in a text editor.
3.  Find the following line and replace the placeholder with your **Actual AWS Public IP**:
    ```javascript
    const YOUR_AWS_PUBLIC_IP = "3.80.165.210"; // <-- REPLACE THIS
    ```
4.  Save the file.

### 4. Connect the Browser Client
* Open your local `index.html` file in a web browser (Chrome or Firefox recommended).
* Check your MobaXterm terminal. You should see the following confirmation:
    > `Client connected. Total: 1`

---

## Running the Unity Build



### 5. Initialize the Linux Build
Open a **second terminal** tab in MobaXterm to keep the server running in the first. Navigate to the Linux build directory:

```bash
cd ./Linux
```

### 6. Set Permissions and Launch
Grant execution permissions to the build file and run the application:

```bash
# Make the build executable
chmod +x CloudBuild.x86_64

# Run the build
./CloudBuild.x86_64
```

### 7. Connection & Synchronization
* **Wait Time:** It may take up to **5 minutes** for the Unity build to launch and handshake with the Node.js server. 
* **Note:** Because the EC2 instance does not utilize a GPU, the system relies on CPU rendering, which significantly increases startup time.

### 8. Final Verification
Once the connection is established, the following should occur:
1.  **Terminal Output:** The server terminal will update to:
    > `Client connected. Total: 2`
2.  **Web Interface:** The `index.html` page in your browser will begin displaying the game screen.

---

## Troubleshooting
* **No Gameplay Steaming is shown in index.html at all:** Ensure you have (1) launch the server, (2) establish connection with index.html and (3) launch Unity Build in correct order. And be patient.
