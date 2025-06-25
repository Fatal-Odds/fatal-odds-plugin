using UnityEngine;

namespace FatalOdds.Runtime
{
    
    /// Simple item animator component for floating and rotating item pickups
    
    [AddComponentMenu("Fatal Odds/Item Animator")]
    public class ItemAnimator : MonoBehaviour
    {
        [Header("Floating Animation")]
        [SerializeField] private bool enableFloating = true;
        [SerializeField] private float floatHeight = 0.3f;
        [SerializeField] private float floatSpeed = 2f;

        [Header("Rotation Animation")]
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private float rotationSpeed = 45f;

        [Header("Bobbing Animation")]
        [SerializeField] private bool enableBobbing = false;
        [SerializeField] private float bobbingHeight = 0.1f;
        [SerializeField] private float bobbingSpeed = 3f;

        [Header("Pulsing Animation")]
        [SerializeField] private bool enablePulsing = false;
        [SerializeField] private float pulseScale = 0.1f;
        [SerializeField] private float pulseSpeed = 2f;

        private Vector3 startPosition;
        private Vector3 startScale;
        private float timeOffset;

        private void Start()
        {
            startPosition = transform.position;
            startScale = transform.localScale;

            // Add random time offset so multiple items don't animate in sync
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float currentTime = Time.time + timeOffset;
            Vector3 newPosition = startPosition;
            Vector3 newScale = startScale;

            // Floating animation (up and down)
            if (enableFloating)
            {
                float floatY = Mathf.Sin(currentTime * floatSpeed) * floatHeight;
                newPosition.y = startPosition.y + floatY;
            }

            // Bobbing animation (secondary smaller movement)
            if (enableBobbing)
            {
                float bobY = Mathf.Sin(currentTime * bobbingSpeed) * bobbingHeight;
                newPosition.y += bobY;
            }

            // Apply position changes
            transform.position = newPosition;

            // Rotation animation
            if (enableRotation)
            {
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
            }

            // Pulsing animation (scale)
            if (enablePulsing)
            {
                float pulseMultiplier = 1f + Mathf.Sin(currentTime * pulseSpeed) * pulseScale;
                newScale = startScale * pulseMultiplier;
                transform.localScale = newScale;
            }
        }

        
        /// Reset animation to starting state
        
        public void ResetAnimation()
        {
            transform.position = startPosition;
            transform.localScale = startScale;
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        
        /// Enable/disable specific animations at runtime
        
        public void SetAnimationState(bool floating, bool rotation, bool bobbing = false, bool pulsing = false)
        {
            enableFloating = floating;
            enableRotation = rotation;
            enableBobbing = bobbing;
            enablePulsing = pulsing;
        }

        
        /// Set animation speeds
        
        public void SetAnimationSpeeds(float floatSpd = 2f, float rotSpd = 45f, float bobSpd = 3f, float pulseSpd = 2f)
        {
            floatSpeed = floatSpd;
            rotationSpeed = rotSpd;
            bobbingSpeed = bobSpd;
            pulseSpeed = pulseSpd;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw animation bounds in editor
            Gizmos.color = Color.yellow;
            Vector3 pos = Application.isPlaying ? startPosition : transform.position;

            if (enableFloating)
            {
                // Draw floating range
                Gizmos.DrawLine(pos + Vector3.up * floatHeight, pos - Vector3.up * floatHeight);
                Gizmos.DrawWireSphere(pos + Vector3.up * floatHeight, 0.1f);
                Gizmos.DrawWireSphere(pos - Vector3.up * floatHeight, 0.1f);
            }
        }
    }
}