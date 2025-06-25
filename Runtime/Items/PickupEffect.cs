using System.Collections;
using UnityEngine;
using FatalOdds.Runtime;

namespace FatalOdds.Runtime
{
    [AddComponentMenu("Fatal Odds/Pickup Effect")]
    public class PickupEffect : MonoBehaviour
    {
        [Header("Pickup Animation")]
        [SerializeField] private AnimationType animationType = AnimationType.WarpToPlayer;
        [SerializeField] private float animationDuration = 0.6f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Warp Effect")]
        [SerializeField] private float initialPause = 0.1f;
        [SerializeField] private float warpSpeed = 8f;
        [SerializeField] private bool addVerticalBoost = true;
        [SerializeField] private float verticalBoostHeight = 1f;

        [Header("Visual Effects")]
        [SerializeField] private bool spawnPickupEffect = true;
        [SerializeField] private bool fadeOutMaterial = true;
        [SerializeField] private bool disableParticlesOnPickup = true;
        [SerializeField] private bool pulseLightOnPickup = true;

        [Header("Audio")]
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private float soundVolume = 1f;

        private bool isBeingCollected = false;
        private Vector3 originalScale;
        private Material[] originalMaterials;
        private ParticleSystem[] attachedParticles;
        private Light[] attachedLights;

        public enum AnimationType
        {
            WarpToPlayer,
            FadeOut,
            ShrinkAndDisappear,
            TeleportEffect
        }

        private void Awake()
        {
            originalScale = transform.localScale;

            // Cache materials for fade effect
            var renderers = GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                originalMaterials = renderers[0].materials;
            }

            // Cache particle systems and lights
            attachedParticles = GetComponentsInChildren<ParticleSystem>();
            attachedLights = GetComponentsInChildren<Light>();
        }

        // Start the pickup animation
        public void StartPickupAnimation(GameObject player)
        {
            if (isBeingCollected) return;

            isBeingCollected = true;

            // Play pickup sound
            PlayPickupSound();

            // Start the appropriate animation
            switch (animationType)
            {
                case AnimationType.WarpToPlayer:
                    StartCoroutine(WarpToPlayerAnimation(player));
                    break;
                case AnimationType.FadeOut:
                    StartCoroutine(FadeOutAnimation());
                    break;
                case AnimationType.ShrinkAndDisappear:
                    StartCoroutine(ShrinkAnimation());
                    break;
                case AnimationType.TeleportEffect:
                    StartCoroutine(TeleportAnimation(player));
                    break;
            }
        }

        // Warp to player animation with smooth movement
        private IEnumerator WarpToPlayerAnimation(GameObject player)
        {
            if (player == null)
            {
                yield return StartCoroutine(FadeOutAnimation());
                yield break;
            }

            // Initial pause for impact
            yield return new WaitForSeconds(initialPause);

            // Pulse light effect
            if (pulseLightOnPickup)
            {
                StartCoroutine(PulseLights());
            }

            // Disable particles early
            if (disableParticlesOnPickup)
            {
                foreach (var particles in attachedParticles)
                {
                    if (particles != null)
                    {
                        var emission = particles.emission;
                        emission.enabled = false;
                    }
                }
            }

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = player.transform.position + Vector3.up * 1.5f; // Aim for chest height

            // Add vertical boost for more dramatic effect
            Vector3 midPoint = Vector3.Lerp(startPosition, targetPosition, 0.5f);
            if (addVerticalBoost)
            {
                midPoint.y += verticalBoostHeight;
            }

            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / animationDuration;

                // Apply movement curve for smooth acceleration
                float curveValue = movementCurve.Evaluate(t);

                // Bezier curve for smooth arc movement
                Vector3 currentPos = CalculateBezierPoint(t, startPosition, midPoint, targetPosition);
                transform.position = currentPos;

                // Scale animation
                float scaleValue = scaleCurve.Evaluate(t);
                transform.localScale = originalScale * scaleValue;

                // Fade out material
                if (fadeOutMaterial && originalMaterials != null)
                {
                    float alpha = Mathf.Lerp(1f, 0f, curveValue);
                    SetMaterialAlpha(alpha);
                }

                // Update target position if player is moving
                if (player != null)
                {
                    targetPosition = player.transform.position + Vector3.up * 1.5f;
                }

                yield return null;
            }

            // Spawn pickup effect at player location
            if (spawnPickupEffect && player != null)
            {
                SpawnPickupEffect(player.transform.position);
            }

            // Cleanup
            Destroy(gameObject);
        }

        // Simple fade out animation
        private IEnumerator FadeOutAnimation()
        {
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / animationDuration;

                // Fade out material
                if (originalMaterials != null)
                {
                    SetMaterialAlpha(1f - t);
                }

                // Slightly shrink
                transform.localScale = originalScale * (1f - t * 0.2f);

                yield return null;
            }

            Destroy(gameObject);
        }

        // Shrink and disappear animation
        private IEnumerator ShrinkAnimation()
        {
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / animationDuration;

                // Scale down with curve
                float scaleValue = scaleCurve.Evaluate(t);
                transform.localScale = originalScale * scaleValue;

                // Slight upward movement
                transform.position += Vector3.up * Time.deltaTime * 2f;

                yield return null;
            }

            Destroy(gameObject);
        }

        // Teleport effect animation
        private IEnumerator TeleportAnimation(GameObject player)
        {
            // Quick flash effect
            if (pulseLightOnPickup)
            {
                StartCoroutine(PulseLights());
            }

            // Rapid shrink
            float shrinkTime = 0.2f;
            float elapsedTime = 0f;

            while (elapsedTime < shrinkTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / shrinkTime;

                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                yield return null;
            }

            // Spawn effect at player
            if (spawnPickupEffect && player != null)
            {
                SpawnPickupEffect(player.transform.position);
            }

            Destroy(gameObject);
        }

        // Calculate bezier curve point for smooth arcing movement
        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 p = uu * p0; // (1-t)^2 * P0
            p += 2 * u * t * p1; // 2(1-t)t * P1
            p += tt * p2; // t^2 * P2

            return p;
        }

        // Set alpha on all materials
        private void SetMaterialAlpha(float alpha)
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        Color color = material.color;
                        color.a = alpha;
                        material.color = color;
                    }
                    else if (material.HasProperty("_BaseColor"))
                    {
                        Color color = material.GetColor("_BaseColor");
                        color.a = alpha;
                        material.SetColor("_BaseColor", color);
                    }
                }
            }
        }

        // Pulse lights for pickup effect
        private IEnumerator PulseLights()
        {
            if (attachedLights == null || attachedLights.Length == 0) yield break;

            float originalIntensity = attachedLights[0].intensity;
            float pulseTime = 0.3f;
            float elapsedTime = 0f;

            while (elapsedTime < pulseTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / pulseTime;

                float intensity = originalIntensity + Mathf.Sin(t * Mathf.PI * 4f) * originalIntensity * 0.5f;

                foreach (var light in attachedLights)
                {
                    if (light != null)
                    {
                        light.intensity = intensity;
                    }
                }

                yield return null;
            }
        }

        // Spawn pickup effect particles
        private void SpawnPickupEffect(Vector3 position)
        {
            // Create a simple burst effect
            GameObject effectGO = new GameObject("PickupEffect");
            effectGO.transform.position = position;

            ParticleSystem effect = effectGO.AddComponent<ParticleSystem>();
            var main = effect.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 3f;
            main.startSize = 0.1f;

            // Try to get rarity color from UniversalItemPickup
            main.startColor = GetPickupRarityColor();
            main.maxParticles = 20;

            var emission = effect.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 20)
            });

            var shape = effect.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            effect.Play();

            // Auto-destroy
            Destroy(effectGO, 2f);
        }

        // Get rarity color from the item pickup
        private Color GetPickupRarityColor()
        {
            // Try to get color from UniversalItemPickup
            var universalPickup = GetComponent<ItemPickup>();
            if (universalPickup != null && universalPickup.GetItemDefinition() != null)
            {
                return GetRarityColor(universalPickup.GetItemDefinition().rarity);
            }

            // Fallback to white
            return Color.white;
        }

        // Play pickup sound
        private void PlayPickupSound()
        {
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
            }
        }

        // Get rarity color (fallback method)
        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return new Color(0.85f, 0.85f, 0.85f, 1f);
                case ItemRarity.Uncommon: return new Color(0.4f, 0.8f, 0.4f, 1f);
                case ItemRarity.Rare: return new Color(0.3f, 0.5f, 0.9f, 1f);
                case ItemRarity.Epic: return new Color(0.7f, 0.4f, 0.9f, 1f);
                case ItemRarity.Legendary: return new Color(1f, 0.8f, 0.3f, 1f);
                case ItemRarity.Artifact: return new Color(0.8f, 0.3f, 0.3f, 1f);
                default: return Color.white;
            }
        }

        // Force collect with default animation
        public void ForceCollect()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            StartPickupAnimation(player);
        }
    }
}