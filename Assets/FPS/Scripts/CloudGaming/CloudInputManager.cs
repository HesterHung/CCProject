using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Layouts;

namespace Unity.FPS.CloudGaming
{
    [Serializable]
    public class CloudInputMessage
    {
        public string type;
        public string keyType;
        public string key;
        public float moveX;
        public float moveY;
        public float absX;
        public float absY;
        public int button;
    }

    public class CloudInputManager : MonoBehaviour
    {
        private Keyboard m_VirtualKeyboard;
        private Mouse m_VirtualMouse;

        private KeyboardState m_KeyboardState = new KeyboardState();
        private MouseState m_MouseState = new MouseState();
        private Vector2 m_CurrentMousePos;

        void Awake()
        {
            // Force the game to run and accept input even if looking at the browser window
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;

            // Generate Headless Virtual Input Devices to trick the FPS controller
            // Awake ensures they are created before PlayerInputHandler runs Start()
            m_VirtualKeyboard = InputSystem.AddDevice<Keyboard>();
            m_VirtualMouse = InputSystem.AddDevice<Mouse>();
            
            // Register them so the game accepts them as human input
            InputSystem.EnableDevice(m_VirtualKeyboard);
            InputSystem.EnableDevice(m_VirtualMouse);

            m_CurrentMousePos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        }

        void LateUpdate()
        {
            if (m_VirtualMouse != null)
            {
                // Silently clear the delta at the end of the frame so the character doesn't endlessly spin
                m_MouseState.delta = Vector2.zero;
                InputSystem.QueueStateEvent(m_VirtualMouse, m_MouseState);
            }
        }

        public void ProcessBrowserInput(string jsonPayload)
        {
            var inputMsg = JsonUtility.FromJson<CloudInputMessage>(jsonPayload);

            if (inputMsg.keyType == "keydown" || inputMsg.keyType == "keyup")
            {
                Key unityKey = MapHtmlKeyToUnityKey(inputMsg.key);
                if (unityKey != Key.None)
                {
                    bool isDown = (inputMsg.keyType == "keydown");
                    m_KeyboardState.Set(unityKey, isDown);
                    InputSystem.QueueStateEvent(m_VirtualKeyboard, m_KeyboardState);
                }
            }
            else if (inputMsg.keyType == "mousemove")
            {
                m_MouseState.delta = new Vector2(inputMsg.moveX, -inputMsg.moveY);
                m_CurrentMousePos += m_MouseState.delta;
                
                // Clamp absolute position bounds to fake screen containment
                m_CurrentMousePos.x = Mathf.Clamp(m_CurrentMousePos.x, 0, Screen.width);
                m_CurrentMousePos.y = Mathf.Clamp(m_CurrentMousePos.y, 0, Screen.height);
                m_MouseState.position = m_CurrentMousePos;

                InputSystem.QueueStateEvent(m_VirtualMouse, m_MouseState);
            }
            else if (inputMsg.keyType == "mousemove_abs")
            {
                // UI interaction when not pointer locked
                m_CurrentMousePos = new Vector2(inputMsg.absX * Screen.width, inputMsg.absY * Screen.height);
                m_MouseState.position = m_CurrentMousePos;
                m_MouseState.delta = Vector2.zero;

                InputSystem.QueueStateEvent(m_VirtualMouse, m_MouseState);
            }
            else if (inputMsg.keyType == "mousedown" || inputMsg.keyType == "mouseup")
            {
                bool isDown = (inputMsg.keyType == "mousedown");

                // Map HTML button index to Unity bitmask index
                int unityBtnIndex = 0;
                if (inputMsg.button == 0) unityBtnIndex = 0;      // Left Click
                else if (inputMsg.button == 2) unityBtnIndex = 1; // Right Click (Aiming)
                else if (inputMsg.button == 1) unityBtnIndex = 2; // Middle Click

                ushort mask = (ushort)(1 << unityBtnIndex);

                if (isDown)
                    m_MouseState.buttons |= mask;
                else
                    m_MouseState.buttons &= (ushort)~mask;

                InputSystem.QueueStateEvent(m_VirtualMouse, m_MouseState);
            }
        }

        private Key MapHtmlKeyToUnityKey(string htmlCode)
        {
            switch (htmlCode)
            {
                case "KeyW": return Key.W;
                case "KeyA": return Key.A;
                case "KeyS": return Key.S;
                case "KeyD": return Key.D;
                case "Space": return Key.Space;
                case "ShiftLeft": return Key.LeftShift;
                case "KeyR": return Key.R; // Reload
                case "KeyC": return Key.C; // Crouch
                case "KeyQ": return Key.Q; 
                case "Digit1": return Key.Digit1;
                case "Digit2": return Key.Digit2;
                case "Digit3": return Key.Digit3;
                default: return Key.None;
            }
        }

        void OnDestroy()
        {
            if (m_VirtualKeyboard != null) InputSystem.RemoveDevice(m_VirtualKeyboard);
            if (m_VirtualMouse != null) InputSystem.RemoveDevice(m_VirtualMouse);
        }
    }
}
