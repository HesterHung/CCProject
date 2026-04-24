const express = require('express');
const WebSocket = require('ws');
const http = require('http');

const app = express();
app.use(express.static('public'));

const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

let clients = [];

wss.on('connection', (ws) => {
    clients.push(ws);
    console.log('Client connected. Total:', clients.length);

    ws.on('message', (message) => {
        const msgStr = message.toString();
        // console.log("Received data:", msgStr); // Optional: keep this if you still want debug logs

        // --- NEW PING INTERCEPT ---
        try {
            const data = JSON.parse(msgStr);
            if (data.type === 'ping') {
                // Immediately reply to the browser client and stop processing
                ws.send(JSON.stringify({ type: 'pong' }));
                return; 
            }
        } catch (err) {
            // Failsafe: if the message isn't JSON, just ignore and let it broadcast
        }
        // --------------------------

        // Broadcast the WebRTC signaling message to all OTHER clients (e.g., Unity)
        clients.forEach(client => {
            if (client !== ws && client.readyState === WebSocket.OPEN) {
                client.send(msgStr);
            }
        });
    });

    ws.on('close', () => {
        clients = clients.filter(c => c !== ws);
        console.log('Client disconnected. Total:', clients.length);
    });
});

server.listen(8080, () => {
    console.log('----------------------------------------------------');
    console.log('Signaling Server active on port 8080');
    console.log('Open your browser to: http://localhost:8080');
    console.log('----------------------------------------------------');
});