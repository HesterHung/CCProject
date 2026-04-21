using UnityEngine;

namespace Unity.FPS.CloudGaming
{
    public class MotionAnalyzer : MonoBehaviour
    {
        [Tooltip("The camera used to calculate screen motion.")]
        public Camera MainCamera;

        [Header("Motion Metrics")]
        public float CurrentLinearVelocity;
        public float CurrentAngularVelocity;
        
        [Tooltip("A blended score representing overall scene motion (0 = static, higher = high motion)")]
        public float OverallMotionScore;

        private Vector3 m_LastPosition;
        private Vector3 m_LastForward;

        void Start()
        {
            if (MainCamera == null) MainCamera = Camera.main;
            m_LastPosition = MainCamera.transform.position;
            m_LastForward = MainCamera.transform.forward;
        }

        void LateUpdate()
        {
            // 1. Linear Motion (Running, Falling)
            Vector3 currentPos = MainCamera.transform.position;
            CurrentLinearVelocity = Vector3.Distance(currentPos, m_LastPosition) / Time.deltaTime;
            m_LastPosition = currentPos;

            // 2. Angular Motion (Looking around / Mouse flicking)
            // Using Vector3.Angle to get the actual degree difference the camera rotated
            Vector3 currentForward = MainCamera.transform.forward;
            CurrentAngularVelocity = Vector3.Angle(m_LastForward, currentForward) / Time.deltaTime;
            m_LastForward = currentForward;

            // 3. Calculate Overall Scene Motion Score
            // Weights can be tuned based on how much visual blur they cause
            OverallMotionScore = (CurrentLinearVelocity * 0.5f) + (CurrentAngularVelocity * 2.0f);
        }
    }
}
