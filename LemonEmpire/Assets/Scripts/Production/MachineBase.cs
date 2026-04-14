using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public enum MachineState
    {
        Idle,
        WaitingForQte,
        Processing,
        Finished
    }

    public abstract class MachineBase : MonoBehaviour, IInteractable
    {
        [Header("Machine Settings")]
        [SerializeField] protected string machineName = "Станок";
        [SerializeField] protected float baseProcessingTime = 5f;

        [Header("Output")]
        [SerializeField] protected Transform outputPoint;
        [SerializeField] protected ItemType outputItemType;
        [SerializeField] protected string outputItemName = "Готовый продукт";
        [SerializeField] protected Color outputColor = Color.white;
        [SerializeField] protected int outputItemAmount = 1;
        [SerializeField] protected GameObject outputPrefab;

        [Header("Packing")]
        [SerializeField] protected GameObject boxPrefab;

        protected MachineState _state = MachineState.Idle;
        protected float _processingTimer;
        protected float _currentProcessingTime;

        protected ProgressUI _progressUI;
        protected MachineQTE _qte;

        public virtual bool HasPendingIngredients() => false;
        public bool CanBePacked => _state == MachineState.Idle && !HasPendingIngredients();

        protected ItemBase _readyProduct;
        protected float _lastQteScore;

        protected virtual void Awake()
        {
            _progressUI = GetComponentInChildren<ProgressUI>();
            if (_progressUI == null)
            {

                var uiGo = new GameObject("ProgressUI");
                uiGo.transform.SetParent(transform);
                uiGo.transform.localPosition = Vector3.up * 1.5f;
                _progressUI = uiGo.AddComponent<ProgressUI>();
            }

            _qte = GetComponentInChildren<MachineQTE>();
            if (_qte == null)
            {
                var qteGo = new GameObject("QTE");
                qteGo.transform.SetParent(transform);
                qteGo.transform.localPosition = Vector3.up * 1.8f;
                _qte = qteGo.AddComponent<MachineQTE>();
            }
        }

        protected virtual void Update()
        {
            if (_state == MachineState.Processing)
            {
                _processingTimer -= Time.deltaTime;

                if (_progressUI != null)
                    _progressUI.UpdateProgress(1f - (_processingTimer / _currentProcessingTime));

                if (_processingTimer <= 0f)
                {
                    FinishProcessing();
                }
            }

            if (_state == MachineState.Finished && _readyProduct == null)
            {
                _state = MachineState.Idle;
            }
        }

        #region IInteractable

        public virtual bool CanInteract => true;

        public virtual string InteractionPrompt
        {
            get
            {
                switch (_state)
                {
                    case MachineState.Idle:
                        return GetIdlePrompt() + "\n[F] Упаковать";
                    case MachineState.WaitingForQte:
                        return "[E] Сладкая точка!";
                    case MachineState.Processing:
                        return $"{machineName} (в работе...)";
                    case MachineState.Finished:
                        return $"[E] Забрать {_readyProduct?.DisplayName ?? "готовый продукт"}";
                    default:
                        return "";
                }
            }
        }

        public virtual void Interact(PlayerInteractionContext context)
        {
            switch (_state)
            {
                case MachineState.Idle:
                    TryAcceptItem(context);
                    break;
                case MachineState.WaitingForQte:
                    HandleQteInput();
                    break;
                case MachineState.Processing:

                    break;
                case MachineState.Finished:
                    CollectProduct(context);
                    break;
            }
        }

        #endregion

        protected abstract string GetIdlePrompt();

        protected abstract void TryAcceptItem(PlayerInteractionContext context);

        protected void StartQtePhase()
        {
            _state = MachineState.WaitingForQte;
            _qte.StartQte(OnQteMissed);
        }

        private void HandleQteInput()
        {
            _lastQteScore = _qte.StopQte();

            float speedMultiplier = Mathf.Lerp(1.0f, 0.2f, _lastQteScore);

            _currentProcessingTime = baseProcessingTime * speedMultiplier;
            _processingTimer = _currentProcessingTime;

            _state = MachineState.Processing;

            if (_progressUI != null)
                _progressUI.Show();
        }

        private void OnQteMissed()
        {
            _lastQteScore = 0f;
            _currentProcessingTime = baseProcessingTime;
            _processingTimer = _currentProcessingTime;
            _state = MachineState.Processing;

            if (_progressUI != null)
                _progressUI.Show();
        }

        protected virtual void FinishProcessing()
        {
            _state = MachineState.Finished;
            if (_progressUI != null) _progressUI.Hide();

            Vector3 spawnPos = outputPoint != null ? outputPoint.position : transform.position + Vector3.up;

            GameObject go;
            if (outputPrefab != null)
            {
                go = Instantiate(outputPrefab, spawnPos, transform.rotation);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = outputItemName;
                go.transform.position = spawnPos;
                go.transform.localScale = Vector3.one;

                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = outputColor;
                    rend.material = mat;
                }
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.mass = 1.5f;

            _readyProduct = go.GetComponent<ItemBase>();
            if (_readyProduct == null) _readyProduct = go.AddComponent<ItemBase>();

            float quality = 30f + _lastQteScore * 70f;
            _readyProduct.Setup(outputItemType, outputItemName, quality, outputItemAmount);
        }

        private void CollectProduct(PlayerInteractionContext context)
        {
            if (_readyProduct != null && context.PlayerCarry != null && !context.PlayerCarry.IsCarrying)
            {
                context.PlayerCarry.TryPickup(_readyProduct);
                _readyProduct = null;
                _state = MachineState.Idle;
            }
        }

        public void PackToBox()
        {
            if (boxPrefab != null)
            {

                float surfaceY = transform.position.y;
                var machineCol = GetComponent<Collider>();
                if (machineCol != null)
                    surfaceY = machineCol.bounds.min.y;

                var go = Instantiate(boxPrefab, transform.position, transform.rotation);
                var box = go.GetComponent<MachineBox>();

                var boxCol = go.GetComponent<Collider>();
                if (boxCol != null)
                {
                    Physics.SyncTransforms();
                    float distToBottom = go.transform.position.y - boxCol.bounds.min.y;
                    go.transform.position = new Vector3(go.transform.position.x, surfaceY + distToBottom, go.transform.position.z);
                }

                if (box != null)
                {

                }
            }
            else
            {

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Box: " + machineName;
                go.transform.position = transform.position;
                go.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
                var rend = go.GetComponent<Renderer>();
                if (rend != null) rend.material.color = new Color(0.6f, 0.4f, 0.2f);

                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 5f;
                var box = go.AddComponent<MachineBox>();

                Debug.LogWarning("MachineBase: Cannot pack because boxPrefab is not assigned!");
                Destroy(go);
                return;
            }

            Destroy(gameObject);
        }
    }
}
