using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Player
{

    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private float detectionRadius = 0.0f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private IInteractable _currentTarget;
        private bool _interactRequested;
        private PlayerCarry _cachedCarry;

        public event System.Action<string> OnPromptChanged;
        public event System.Action OnPromptCleared;

        private string _lastPrompt = "";

        private Material _outlineMaterial;
        private System.Collections.Generic.List<GameObject> _highlightGhosts = new System.Collections.Generic.List<GameObject>();
        private float _lastInteractTime;

        public IInteractable CurrentTarget => _currentTarget;

        private void Awake()
        {
            _outlineMaterial = Resources.Load<Material>("OutlineMaterial");
            if (_outlineMaterial == null)
            {
                Debug.LogWarning("[PlayerInteraction] OutlineMaterial not found in Resources. Make sure OutlineMaterial.mat exists in Assets/Resources/");
            }
            _cachedCarry = GetComponent<PlayerCarry>();
        }

        private void Update()
        {
            DetectInteractable();
            ProcessInteraction();
            UpdatePrompt();
        }

        public void TriggerInteraction()
        {
            _interactRequested = true;
        }

        private void DetectInteractable()
        {
            IInteractable best = null;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                float maxCastDistance = interactionRange + 5f; // Extra buffer because camera is offset from player's feet

                RaycastHit[] hits;
                if (detectionRadius > 0.01f)
                {
                    hits = Physics.SphereCastAll(ray, detectionRadius, maxCastDistance, interactionLayers, QueryTriggerInteraction.Collide);
                }
                else
                {
                    hits = Physics.RaycastAll(ray, maxCastDistance, interactionLayers, QueryTriggerInteraction.Collide);
                }

                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                var carry = _cachedCarry;

                foreach (var hit in hits)
                {
                    // Ignore player's own collider
                    if (hit.collider.transform.root == transform.root)
                        continue;

                    // Ignore currently carried item collider
                    if (carry != null && carry.IsCarrying && carry.CarriedItem != null && hit.collider.transform.root == carry.CarriedItem.transform.root)
                        continue;

                    var interactable = GetInteractableFromHit(hit);
                    if (interactable != null)
                    {
                        // Measure distance from the player (body) to the hit point / closest point on collider
                        Vector3 playerRef = transform.position + Vector3.up * 0.8f;
                        float dist;
                        if (hit.distance > 0f)
                        {
                            dist = Vector3.Distance(playerRef, hit.point);
                        }
                        else
                        {
                            Vector3 closestPoint = hit.collider.ClosestPoint(playerRef);
                            dist = Vector3.Distance(playerRef, closestPoint);
                        }

                        if (dist <= interactionRange)
                        {
                            best = interactable;
                            break;
                        }
                    }
                }
            }

            if (best != _currentTarget)
            {
                ClearCurrentTargetHighlight();
                _currentTarget = best;
                ApplyCurrentTargetHighlight();
            }
        }

        private IInteractable GetInteractableFromHit(RaycastHit hit)
        {
            if (hit.collider == null) return null;

            // Find the GameObject in parent hierarchy that has IInteractable components
            GameObject holder = null;
            Transform curr = hit.collider.transform;
            while (curr != null)
            {
                if (curr.GetComponent<IInteractable>() != null)
                {
                    holder = curr.gameObject;
                    break;
                }
                curr = curr.parent;
            }

            if (holder == null) return null;

            var interactables = holder.GetComponents<IInteractable>();
            if (interactables.Length == 1)
            {
                return interactables[0];
            }
            else if (interactables.Length > 1)
            {
                // For objects with multiple interactables (like double doors),
                // match the specific child transform hierarchy to the interactable's target hinge/anchor
                foreach (var inter in interactables)
                {
                    if (inter is LemonEmpire.Production.InteractiveDoor door)
                    {
                        var field = door.GetType().GetField("hinge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            var hinge = field.GetValue(door) as Transform;
                            if (hinge != null && hit.collider.transform.IsChildOf(hinge))
                            {
                                return door;
                            }
                        }
                    }
                }

                // Fallback to the first one
                return interactables[0];
            }

            return null;
        }

        private void UpdatePrompt()
        {
            string currentPrompt = GetCurrentPrompt() ?? "";

            if (currentPrompt != _lastPrompt)
            {
                _lastPrompt = currentPrompt;
                if (!string.IsNullOrEmpty(currentPrompt))
                {
                    OnPromptChanged?.Invoke(currentPrompt);
                }
                else
                {
                    OnPromptCleared?.Invoke();
                }
            }
        }

        private void ProcessInteraction()
        {
            if (!_interactRequested) return;
            _interactRequested = false;

            if (Time.time - _lastInteractTime < 0.2f)
            {
                return;
            }
            _lastInteractTime = Time.time;

            var carry = _cachedCarry;

            // Detailed Debug Trace on Interact Request
            System.Text.StringBuilder debugSb = new System.Text.StringBuilder();
            debugSb.AppendLine($"[PlayerInteraction] Interact request processed.");
            debugSb.AppendLine($"Player Pos: {transform.position}");
            
            Camera cam = Camera.main;
            if (cam == null)
            {
                debugSb.AppendLine("Camera.main is null!");
            }
            else
            {
                debugSb.AppendLine($"Camera Pos: {cam.transform.position}, Forward: {cam.transform.forward}");
                var tpc = cam.GetComponent<ThirdPersonCamera>();
                debugSb.AppendLine($"Camera has ThirdPersonCamera: {tpc != null} (enabled: {(tpc != null ? tpc.enabled.ToString() : "N/A")})");
                
                var allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                debugSb.AppendLine($"All active cameras in scene: {allCams.Length}");
                foreach (var c in allCams)
                {
                    debugSb.AppendLine($"  - Cam: {c.name}, Pos: {c.transform.position}, Forward: {c.transform.forward}, Tag: {c.tag}, Enabled: {c.enabled}");
                }

                debugSb.AppendLine($"Current Target: {(_currentTarget != null ? _currentTarget.GetType().Name + " on " + ((MonoBehaviour)_currentTarget).name : "null")}");
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                float maxCastDistance = interactionRange + 5f;
                RaycastHit[] hits = detectionRadius > 0.01f ? 
                    Physics.SphereCastAll(ray, detectionRadius, maxCastDistance, interactionLayers, QueryTriggerInteraction.Collide) :
                    Physics.RaycastAll(ray, maxCastDistance, interactionLayers, QueryTriggerInteraction.Collide);
                
                debugSb.AppendLine($"Raycast/Spherecast hits count: {hits.Length}");
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];
                    if (hit.collider.transform.root == transform.root)
                    {
                        debugSb.AppendLine($"  Hit {i}: {hit.collider.name} (Ignored: Player own collider)");
                        continue;
                    }
                    
                    var interactable = GetInteractableFromHit(hit);
                    float dist = hit.distance > 0f ? Vector3.Distance(transform.position, hit.point) : Vector3.Distance(transform.position, hit.collider.ClosestPoint(transform.position));
                    
                    debugSb.AppendLine($"  Hit {i}: GO: {hit.collider.name}, Dist from Player: {dist:F2}, Hit Dist: {hit.distance:F2}");
                    if (interactable == null)
                    {
                        debugSb.AppendLine("    -> No IInteractable found in parent hierarchy");
                    }
                    else
                    {
                        debugSb.AppendLine($"    -> Found IInteractable: {interactable.GetType().Name} on {((MonoBehaviour)interactable).name}");
                        debugSb.AppendLine($"       CanInteract: {interactable.CanInteract}, Range check: {dist <= interactionRange}");
                    }
                }
            }
            Debug.Log(debugSb.ToString());

            if (carry != null && carry.IsCarrying)
            {

                if (IsTargetValid && !(_currentTarget is ItemBase) && _currentTarget.CanInteract)
                {
                    var ctx = new PlayerInteractionContext
                    {
                        PlayerTransform = transform,
                        PlayerCarry = carry
                    };
                    _currentTarget.Interact(ctx);
                    return;
                }

                carry.DropItem();
                return;
            }

            if (IsTargetValid && _currentTarget.CanInteract)
            {
                var ctx = new PlayerInteractionContext
                {
                    PlayerTransform = transform,
                    PlayerCarry = carry
                };
                _currentTarget.Interact(ctx);
            }
        }

        public void TriggerSecondaryInteraction()
        {
            var carry = _cachedCarry;

            // Check if looking at an active secondary interactable first
            if (IsTargetValid && _currentTarget is ISecondaryInteractable secondaryTarget && secondaryTarget.CanSecondaryInteract)
            {
                var ctx = new PlayerInteractionContext
                {
                    PlayerTransform = transform,
                    PlayerCarry = carry
                };
                secondaryTarget.SecondaryInteract(ctx);
                return;
            }

            // Otherwise, if carrying something, throw it
            if (carry != null && carry.IsCarrying)
            {
                carry.ThrowItem();
                return;
            }
            // Old machine interactions removed
        }

        private bool IsTargetValid => _currentTarget != null && (_currentTarget as UnityEngine.Object) != null;

        public string GetCurrentPrompt()
        {
            var carry = _cachedCarry;
            string primaryPrompt = "";
            string secondaryPrompt = "";
            bool isCarrying = carry != null && carry.IsCarrying;

            if (IsTargetValid)
            {
                if (isCarrying)
                {
                    if (!(_currentTarget is ItemBase) && _currentTarget.CanInteract)
                    {
                        primaryPrompt = _currentTarget.InteractionPrompt;
                    }
                }
                else
                {
                    primaryPrompt = _currentTarget.InteractionPrompt;
                }

                if (_currentTarget is ISecondaryInteractable secondaryTarget && secondaryTarget.CanSecondaryInteract)
                {
                    secondaryPrompt = secondaryTarget.SecondaryInteractionPrompt;
                }
            }

            // Append drink option if applicable
            string drinkPrompt = "";
            if (isCarrying && carry.CarriedItem != null && carry.CarriedItem.ItemType == ItemType.BottledLemonade && !carry.CarriedItem.IsCrate)
            {
                drinkPrompt = $"[R] Выпить {carry.CarriedItem.DisplayName}";
            }
            else if (!isCarrying && IsTargetValid && _currentTarget.CanInteract)
            {
                if (_currentTarget is ItemBase targetItem && targetItem.ItemType == ItemType.BottledLemonade && !targetItem.IsCrate)
                {
                    drinkPrompt = "[R] Выпить";
                }
                else if (_currentTarget is LemonEmpire.Production.ServiceCounter counter && counter.PlacedDrink != null && counter.PlacedDrink.ItemType == ItemType.BottledLemonade && !counter.PlacedDrink.IsCrate)
                {
                    drinkPrompt = "[R] Выпить";
                }
            }

            // Fallback for carrying drop prompt
            if (string.IsNullOrEmpty(primaryPrompt) && isCarrying)
            {
                primaryPrompt = $"E — Положить {carry.CarriedItem.DisplayName}";
            }

            // Combine prompts
            string finalPrompt = primaryPrompt;
            if (!string.IsNullOrEmpty(secondaryPrompt))
            {
                if (!string.IsNullOrEmpty(finalPrompt))
                    finalPrompt += " | " + secondaryPrompt;
                else
                    finalPrompt = secondaryPrompt;
            }

            if (!string.IsNullOrEmpty(drinkPrompt))
            {
                if (!string.IsNullOrEmpty(finalPrompt))
                    finalPrompt += " | " + drinkPrompt;
                else
                    finalPrompt = drinkPrompt;
            }

            return string.IsNullOrEmpty(finalPrompt) ? null : finalPrompt;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, interactionRange * 0.6f);
        }

        private void OnDisable()
        {
            ClearCurrentTargetHighlight();
        }

        private void ApplyCurrentTargetHighlight()
        {
            if (_currentTarget == null || _outlineMaterial == null) return;

            var monoBehaviour = _currentTarget as MonoBehaviour;
            if (monoBehaviour == null) return;

            var renderers = monoBehaviour.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r.sharedMaterials == null || r.sharedMaterials.Length == 0) continue;

                // Ignore components like local player body, hands etc.
                if (r.transform.root == transform.root) continue;

                // Skip particle systems and trail renderers
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;

                Mesh mesh = null;
                bool isSkinned = false;
                Transform[] bones = null;
                Transform rootBone = null;

                if (r is MeshRenderer)
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    mesh = mf.sharedMesh;
                }
                else if (r is SkinnedMeshRenderer smr)
                {
                    if (smr.sharedMesh == null) continue;
                    mesh = smr.sharedMesh;
                    isSkinned = true;
                    bones = smr.bones;
                    rootBone = smr.rootBone;
                }

                if (mesh == null) continue;

                // Create ghost highlight GameObject
                GameObject ghostGo = new GameObject("_HighlightGhost_" + r.name);
                ghostGo.transform.SetParent(r.transform, false);
                ghostGo.transform.localPosition = Vector3.zero;
                ghostGo.transform.localRotation = Quaternion.identity;
                ghostGo.transform.localScale = Vector3.one;

                // Fill materials array with _outlineMaterial
                Material[] highlightMats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < highlightMats.Length; i++)
                {
                    highlightMats[i] = _outlineMaterial;
                }

                if (isSkinned)
                {
                    var ghostSmr = ghostGo.AddComponent<SkinnedMeshRenderer>();
                    ghostSmr.sharedMesh = mesh;
                    ghostSmr.bones = bones;
                    ghostSmr.rootBone = rootBone;
                    ghostSmr.sharedMaterials = highlightMats;
                }
                else
                {
                    var ghostMf = ghostGo.AddComponent<MeshFilter>();
                    ghostMf.sharedMesh = mesh;
                    var ghostMr = ghostGo.AddComponent<MeshRenderer>();
                    ghostMr.sharedMaterials = highlightMats;
                }

                _highlightGhosts.Add(ghostGo);
            }
        }

        private void ClearCurrentTargetHighlight()
        {
            foreach (var ghost in _highlightGhosts)
            {
                if (ghost != null)
                {
                    Destroy(ghost);
                }
            }
            _highlightGhosts.Clear();
        }
    }
}
