using UnityEngine;
using UnityEngine.UI;

namespace LemonEmpire.Production
{

    public class ProgressUI : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _fillRT;

        private void Awake()
        {
            gameObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1f, 0.15f);

            gameObject.AddComponent<LookAtCamera>();

            var bgGo = new GameObject("BG", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(transform, false);
            _fillRT = fillGo.GetComponent<RectTransform>();
            _fillRT.anchorMin = Vector2.zero;
            _fillRT.anchorMax = new Vector2(0f, 1f);
            _fillRT.sizeDelta = Vector2.zero;
            _fillRT.offsetMin = Vector2.zero;
            _fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            Hide();
        }

        public void Show()
        {
            if (_fillRT != null) _fillRT.anchorMax = new Vector2(0f, 1f);
            gameObject.SetActive(true);
        }
        public void Hide() => gameObject.SetActive(false);

        public void UpdateProgress(float fillAmount)
        {
            if (_fillRT != null)
                _fillRT.anchorMax = new Vector2(Mathf.Clamp01(fillAmount), 1f);
        }
    }

    public class LookAtCamera : MonoBehaviour
    {
        private Camera _cam;
        private void Start() => _cam = Camera.main;
        private void LateUpdate()
        {
            if (_cam != null)
            {
                transform.rotation = _cam.transform.rotation;
            }
        }
    }
}
