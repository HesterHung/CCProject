using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using UnityEngine;
using Unity.WebRTC;

namespace Unity.FPS.CloudGaming
{
    [Serializable]
    public class SignalingMessage
    {
        public string type;
        public string sdp;
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
    }

    public class CloudWebRTCManager : MonoBehaviour
    {
        [Tooltip("The camera to stream (should be a low resolution Render Texture for CPU instances)")]
        public Camera StreamCamera;

        [Tooltip("Reference to our earlier Motion Analyzer")]
        public MotionAnalyzer MotionAnalyzer;

        private RTCPeerConnection _peerConnection;
        private RTCDataChannel _dataChannel;
        private VideoStreamTrack _videoTrack;

        private ClientWebSocket _ws;
        private ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
        private CancellationTokenSource _cts;
        
        private static CloudWebRTCManager s_Instance;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this) 
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
            DontDestroyOnLoad(gameObject);

            if (GetComponent<CloudInputManager>() == null)
            {
                gameObject.AddComponent<CloudInputManager>();
            }
        }

        void Start()
        {
            _cts = new CancellationTokenSource();
            StartCoroutine(Unity.WebRTC.WebRTC.Update());
            StartCoroutine(InitWebRTC());
        }

        private IEnumerator InitWebRTC()
        {
            RTCConfiguration config = new RTCConfiguration
            {
                iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
            };

            _peerConnection = new RTCPeerConnection(ref config);

            // Network Pathing Handshake
            _peerConnection.OnIceCandidate = candidate =>
            {
                SignalingMessage msg = new SignalingMessage
                {
                    type = "candidate",
                    candidate = candidate.Candidate,
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMLineIndex ?? 0
                };
                SendSignalingMessage(msg);
            };

            // ----------------------------------------------------
            // 1. Establish the Video Track (CPU intensive)
            // ----------------------------------------------------
            if (StreamCamera != null)
            {
                _videoTrack = StreamCamera.CaptureStreamTrack(1280, 720); 
                _peerConnection.AddTrack(_videoTrack);
                
                // CRITICAL: We must explicitly tell WebRTC to send video data to the peer
                _peerConnection.AddTransceiver(_videoTrack);
            }

            // ----------------------------------------------------
            // 2. Establish the Data Channel (Innovation 2)
            // ----------------------------------------------------
            RTCDataChannelInit dataChannelConfig = new RTCDataChannelInit();
            dataChannelConfig.ordered = true; // Reliable transmission
            
            _dataChannel = _peerConnection.CreateDataChannel("MotionMetadata", dataChannelConfig);
            _dataChannel.OnOpen = () => Debug.Log("Motion Data Channel Opened!");
            
            // Connect to the local Node.js Server in the background
            _ = ConnectSignalingServer();

            yield return null;
        }

        private async Task ConnectSignalingServer()
        {
            _ws = new ClientWebSocket();
            Uri serverUri = new Uri("ws://127.0.0.1:8080"); // Better IPv4 compatibility on Linux

            try
            {
                await _ws.ConnectAsync(serverUri, _cts.Token);
                Debug.Log("Connected to Signaling Server!");

                // We are the Host. Create the Offer and set our local path.
                _mainThreadActions.Enqueue(() => {
                    if (this != null) StartCoroutine(CreateWebRTCOffer());
                });

                // Listen for Answers / Candidates from the Browser
                var buffer = new byte[1024 * 64]; // Use an expanding 64 KB buffer since SDP arrays can be massive
                while (_ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    int totalCount = 0;
                    WebSocketReceiveResult result;

                    do 
                    {
                        result = await _ws.ReceiveAsync(
                            new ArraySegment<byte>(buffer, totalCount, buffer.Length - totalCount), 
                            _cts.Token);
                        totalCount += result.Count;
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string json = Encoding.UTF8.GetString(buffer, 0, totalCount);
                        SignalingMessage msg = JsonUtility.FromJson<SignalingMessage>(json);
                        
                        if (msg.type == "answer")
                        {
                            _mainThreadActions.Enqueue(() => { if (this != null) StartCoroutine(SetRemoteDescription(msg.sdp)); });
                        }
                        else if (msg.type == "candidate")
                        {
                            _mainThreadActions.Enqueue(() => { if (this != null) AddIceCandidate(msg); });
                        }
                        else if (msg.type == "input" || json.Contains("\"type\":\"input\""))
                        {
                            // Route Web Inputs
                            _mainThreadActions.Enqueue(() => 
                            {
                                if (this == null) return;
                                var inputSys = GetComponent<CloudInputManager>();
                                if (inputSys == null)
                                {
                                    inputSys = gameObject.AddComponent<CloudInputManager>();
                                }
                                
                                if (inputSys) 
                                {
                                    inputSys.ProcessBrowserInput(json);
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Signaling Error: " + e.Message);
            }
        }

        private IEnumerator CreateWebRTCOffer()
        {
            // Give WebRTC a tiny moment to spin up 
            yield return new WaitForSeconds(0.5f);

            var op = _peerConnection.CreateOffer();
            yield return op;

            if (!op.IsError)
            {
                var desc = op.Desc;
                var opLocal = _peerConnection.SetLocalDescription(ref desc);
                yield return opLocal;

                SendSignalingMessage(new SignalingMessage { type = "offer", sdp = desc.sdp });
            }
        }

        private IEnumerator SetRemoteDescription(string sdp)
        {
            var desc = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = sdp };
            var op = _peerConnection.SetRemoteDescription(ref desc);
            yield return op;
        }

        private void AddIceCandidate(SignalingMessage msg)
        {
            var candidate = new RTCIceCandidate(new RTCIceCandidateInit
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex
            });
            _peerConnection.AddIceCandidate(candidate);
        }

        private async void SendSignalingMessage(SignalingMessage msg)
        {
            if (_ws != null && _ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                string json = JsonUtility.ToJson(msg);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                try 
                {
                    await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
                } catch { }
            }
        }

        void Update()
        {
            // Survive Scene Changes Innovation: Re-bind to cameras when levels change
            if (StreamCamera == null && _peerConnection != null)
            {
                StreamCamera = Camera.main; // Or any logic to find the specific streaming camera in the new scene
                if (StreamCamera != null)
                {
                    if (_videoTrack != null) _videoTrack.Dispose();
                    
                    _videoTrack = StreamCamera.CaptureStreamTrack(1280, 720);
                    
                    foreach (var sender in _peerConnection.GetSenders())
                    {
                        if (sender.Track != null && sender.Track.Kind == TrackKind.Video)
                        {
                            sender.ReplaceTrack(_videoTrack);
                        }
                    }
                }
            }

            if (MotionAnalyzer == null)
            {
                MotionAnalyzer = FindFirstObjectByType<MotionAnalyzer>();
            }

            // Execute network events on the Main Unity Thread to prevent crashes
            while (_mainThreadActions.TryDequeue(out Action action))
            {
                action.Invoke();
            }

            // Stream the Motion Metadata every frame (Innovation 2)
            if (_dataChannel != null && _dataChannel.ReadyState == RTCDataChannelState.Open && MotionAnalyzer != null)
            {
                string jsonPayload = $@"{{
                    ""frame"": {Time.frameCount},
                    ""motionScore"": {Math.Round(MotionAnalyzer.OverallMotionScore, 2)},
                    ""linearVel"": {Math.Round(MotionAnalyzer.CurrentLinearVelocity, 2)},
                    ""angularVel"": {Math.Round(MotionAnalyzer.CurrentAngularVelocity, 2)}
                }}";

                _dataChannel.Send(jsonPayload);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            
            _videoTrack?.Dispose();
            _dataChannel?.Close();
            _peerConnection?.Close();
        }
    }
}
