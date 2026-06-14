using UnityEngine;
using UnityEngine.UI;

namespace LemonEmpire.UI
{
    public class NPCPatienceUI : MonoBehaviour
    {
        private Canvas _canvas;
        private Image _bgImage;
        private Image _fillImage;
        private Text _timerText;
        private LemonEmpire.Trading.NPCBuyer _npc;
        private Camera _mainCamera;
        private GameObject _canvasGo;
        private GameObject _censorGo;
        private Transform _headBone;

        public void Initialize(LemonEmpire.Trading.NPCBuyer npc)
        {
            _npc = npc;
            _mainCamera = Camera.main;

            // Find Head bone
            foreach (var t in npc.GetComponentsInChildren<Transform>())
            {
                if (t.name == "Head")
                {
                    _headBone = t;
                    break;
                }
            }

            // Create Canvas
            _canvasGo = new GameObject("PatienceCanvas");
            _canvasGo.transform.SetParent(transform, false);
            _canvasGo.transform.localPosition = new Vector3(0f, 0.35f, 0f); // Floating 35cm above the head bone
            _canvasGo.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f); // Scale down for world space

            _canvas = _canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            // Add background panel
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(_canvasGo.transform, false);
            var panelRT = panelGo.AddComponent<RectTransform>();
            panelRT.sizeDelta = new Vector2(70f, 20f);
            _bgImage = panelGo.AddComponent<Image>();
            _bgImage.color = new Color(0.08f, 0.08f, 0.09f, 0.65f); // Transparent sleek dark

            // Add progress bar track
            var trackGo = new GameObject("Track");
            trackGo.transform.SetParent(panelGo.transform, false);
            var trackRT = trackGo.AddComponent<RectTransform>();
            trackRT.sizeDelta = new Vector2(55f, 3f);
            trackRT.anchoredPosition = new Vector2(0f, -5f);
            var trackImg = trackGo.AddComponent<Image>();
            trackImg.color = new Color(0.19f, 0.13f, 0.09f, 1f);

            // Add progress bar fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(trackGo.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(1f, 1f);
            fillRT.sizeDelta = Vector2.zero;
            _fillImage = fillGo.AddComponent<Image>();
            _fillImage.color = new Color(0.33f, 0.87f, 0.42f, 1f); // Green

            // Add timer text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panelGo.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.sizeDelta = new Vector2(65f, 14f);
            textRT.anchoredPosition = new Vector2(0f, 3f);
            _timerText = textGo.AddComponent<Text>();
            _timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_timerText.font == null)
            {
                _timerText.font = Font.CreateDynamicFontFromOSFont("Arial", 10);
            }
            _timerText.fontSize = 9;
            _timerText.alignment = TextAnchor.MiddleCenter;
            _timerText.color = Color.white;

            // Create Censor Bar (3D billboard covering eyes)
            _censorGo = new GameObject("CensorBarCanvas");
            _censorGo.transform.SetParent(transform, false);
            _censorGo.transform.localPosition = new Vector3(0f, 0.08f, -0.12f); // Eye level relative to head bone, shifted towards camera
            _censorGo.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f);

            var censorCanvas = _censorGo.AddComponent<Canvas>();
            censorCanvas.renderMode = RenderMode.WorldSpace;

            var censorPanel = new GameObject("CensorPanel");
            censorPanel.transform.SetParent(_censorGo.transform, false);
            var censorRT = censorPanel.AddComponent<RectTransform>();
            censorRT.sizeDelta = new Vector2(40f, 8f); // Wide black strip covering eyes
            var censorImg = censorPanel.AddComponent<Image>();
            censorImg.color = Color.black;

            _censorGo.SetActive(false); // Off by default
        }

        private void Update()
        {
            if (_npc == null)
            {
                Destroy(gameObject);
                return;
            }

            // Position at the head bone dynamically to follow animations and prevent spawn-frame Y mismatch
            if (_headBone != null)
            {
                transform.position = _headBone.position;
            }
            else
            {
                transform.position = _npc.transform.position + Vector3.up * 1.45f;
            }

            // Hide UI while walking to the queue
            if (_npc.BarState == LemonEmpire.Trading.NPCBuyer.BarCustomerState.WalkingToQueue)
            {
                if (_canvasGo != null && _canvasGo.activeSelf) _canvasGo.SetActive(false);
                
                // Still billboard rotation even if disabled
                if (_mainCamera == null) _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    Vector3 dir = transform.position - _mainCamera.transform.position;
                    dir.y = 0f;
                    if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
                }
                return;
            }

            // Show UI once waiting
            if (_canvasGo != null && !_canvasGo.activeSelf) _canvasGo.SetActive(true);

            // Handle 3D Paranoia Censor Bar visibility
            if (_censorGo != null)
            {
                bool paranoia = Player.PlayerStatusEffects.Instance != null && Player.PlayerStatusEffects.Instance.IsLemonParanoiaActive;
                bool showCensor = paranoia && _npc.BarState != LemonEmpire.Trading.NPCBuyer.BarCustomerState.WalkingToQueue;
                if (_censorGo.activeSelf != showCensor)
                {
                    _censorGo.SetActive(showCensor);
                }
            }

            // Billboard effect: Face camera (horizontal only, keep vertical upright)
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera != null)
            {
                Vector3 dir = transform.position - _mainCamera.transform.position;
                dir.y = 0f; // Keep vertical axis straight
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                }
            }

            // Update fill and text
            float pct = _npc.QueuePatienceTimer / _npc.MaxQueuePatience;
            if (_fillImage != null)
            {
                _fillImage.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
                _fillImage.color = Color.Lerp(new Color(0.95f, 0.38f, 0.35f, 1f), new Color(0.33f, 0.87f, 0.42f, 1f), pct);
            }

            if (_timerText != null)
            {
                string stateIcon = _npc.BarState == LemonEmpire.Trading.NPCBuyer.BarCustomerState.WaitingForOrder ? "📋" : "⏳";
                _timerText.text = $"{stateIcon} {Mathf.CeilToInt(_npc.QueuePatienceTimer)}с";
            }
        }
    }
}
