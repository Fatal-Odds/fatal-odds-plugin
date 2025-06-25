using UnityEngine;

namespace FatalOdds.Runtime
{
    
    /// Detects ground below item and positions it floating above the surface
    
    [AddComponentMenu("Fatal Odds/Item Ground Detector")]
    public class ItemGroundDetector : MonoBehaviour
    {
        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 50f;
        [SerializeField] private float floatHeightAboveGround = 1f;
        [SerializeField] private LayerMask groundLayerMask = -1; // All layers by default
        [SerializeField] private bool checkOnStart = true;
        [SerializeField] private bool continuousCheck = false;
        [SerializeField] private float checkInterval = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugRay = false;
        [SerializeField] private Color debugRayColor = Color.red;

        private Vector3 originalPosition;
        private bool hasFoundGround = false;
        private float lastCheckTime;

        private void Start()
        {
            originalPosition = transform.position;

            if (checkOnStart)
            {
                PositionAboveGround();
            }
        }

        private void Update()
        {
            if (continuousCheck && Time.time - lastCheckTime > checkInterval)
            {
                PositionAboveGround();
                lastCheckTime = Time.time;
            }
        }

        
        /// Perform raycast to find ground and position item above it
        
        public void PositionAboveGround()
        {
            Vector3 rayStart = transform.position;
            Vector3 rayDirection = Vector3.down;

            // Raycast downward to find ground
            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDirection, out hit, groundCheckDistance, groundLayerMask))
            {
                // Found ground, position above it
                Vector3 newPosition = hit.point + Vector3.up * floatHeightAboveGround;
                transform.position = newPosition;
                hasFoundGround = true;

                if (showDebugRay)
                {
                    Debug.DrawLine(rayStart, hit.point, Color.green, 2f);
                    Debug.Log($"[ItemGroundDetector] {gameObject.name} positioned above ground at {newPosition}. Ground type: {hit.collider.name}");
                }
            }
            else
            {
                // No ground found, try raycast upward (in case item spawned below ground)
                if (Physics.Raycast(rayStart, Vector3.up, out hit, groundCheckDistance, groundLayerMask))
                {
                    Vector3 newPosition = hit.point + Vector3.up * floatHeightAboveGround;
                    transform.position = newPosition;
                    hasFoundGround = true;

                    if (showDebugRay)
                    {
                        Debug.DrawLine(rayStart, hit.point, Color.blue, 2f);
                        Debug.Log($"[ItemGroundDetector] {gameObject.name} positioned above ground (upward cast) at {newPosition}");
                    }
                }
                else
                {
                    // No ground found in either direction, keep original position
                    hasFoundGround = false;
                    if (showDebugRay)
                    {
                        Debug.DrawLine(rayStart, rayStart + rayDirection * groundCheckDistance, debugRayColor, 2f);
                        Debug.LogWarning($"[ItemGroundDetector] {gameObject.name} could not find ground within {groundCheckDistance} units");
                    }
                }
            }
        }

        
        /// Force reposition above ground
        
        public void ForceRepositionAboveGround()
        {
            PositionAboveGround();
        }

        
        /// Reset to original spawn position
        
        public void ResetToOriginalPosition()
        {
            transform.position = originalPosition;
            hasFoundGround = false;
        }

        
        /// Set custom ground layers
        
        public void SetGroundLayers(LayerMask layers)
        {
            groundLayerMask = layers;
        }

        
        /// Set floating height above ground
        
        public void SetFloatHeight(float height)
        {
            floatHeightAboveGround = height;
            if (hasFoundGround)
            {
                PositionAboveGround(); // Reposition with new height
            }
        }

        
        /// Check if ground was found
        
        public bool HasFoundGround => hasFoundGround;

        
        /// Get the distance to ground
        
        public float GetDistanceToGround()
        {
            Vector3 rayStart = transform.position;
            RaycastHit hit;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayerMask))
            {
                return hit.distance;
            }

            return -1f; // No ground found
        }

        private void OnDrawGizmosSelected()
        {
            // Draw ground detection range
            Gizmos.color = debugRayColor;
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.down * groundCheckDistance;
            Gizmos.DrawLine(start, end);

            // Draw float height indicator
            if (hasFoundGround)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, 0.2f);

                // Draw connection to ground
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayerMask))
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position, hit.point);
                    Gizmos.DrawWireCube(hit.point, Vector3.one * 0.1f);
                }
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.2f);
            }
        }

#if UNITY_EDITOR
        [Header("Editor Tools")]
        [SerializeField] private bool testInEditor = false;

        private void OnValidate()
        {
            if (testInEditor && Application.isPlaying)
            {
                PositionAboveGround();
                testInEditor = false;
            }
        }
#endif
    }
}