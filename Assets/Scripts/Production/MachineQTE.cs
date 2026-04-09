using UnityEngine;
using UnityEngine.UI;

namespace LemonEmpire.Production
{

    public class MachineQTE : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _sliderTransform;

        private bool _isActive;
        private float _sliderPos;
        private float _speed = 2f;
        private int _dir = 1;

        private System.Action _onMissCallback;
        private float _timeoutTimer;

        private void Awake()
        {
            gameObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1.5f, 0.3f);
            gameObject.AddComponent<LookAtCamera>();

            var bgGo = new GameObject("Zones", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();

            bgImg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

            var greenGo = new GameObject("SweetSpot", typeof(RectTransform));
            greenGo.transform.SetParent(bgGo.transform, false);
            var greenRt = greenGo.GetComponent<RectTransform>();
            greenRt.anchorMin = new Vector2(0.4f, 0f);
            greenRt.anchorMax = new Vector2(0.6f, 1f);
            greenRt.sizeDelta = Vector2.zero;
            var greenImg = greenGo.AddComponent<Image>();
            greenImg.color = new Color(0.2f, 0.8f, 0.2f, 0.9f);

            var sliderGo = new GameObject("Cursor", typeof(RectTransform));
            sliderGo.transform.SetParent(transform, false);
            _sliderTransform = sliderGo.GetComponent<RectTransform>();
            _sliderTransform.anchorMin = new Vector2(0.5f, 0f);
            _sliderTransform.anchorMax = new Vector2(0.5f, 1f);
            _sliderTransform.sizeDelta = new Vector2(0.05f, 0.4f);
            var sliderImg = sliderGo.AddComponent<Image>();
            sliderImg.color = Color.white;

            Hide();
        }

        private void Update()
        {
            if (!_isActive) return;

            _sliderPos += _speed * _dir * Time.deltaTime;
            if (_sliderPos >= 1f)
            {
                _sliderPos = 1f;
                _dir = -1;
            }
            else if (_sliderPos <= 0f)
            {
                _sliderPos = 0f;
                _dir = 1;
            }

            _sliderTransform.anchorMin = new Vector2(_sliderPos, 0f);
            _sliderTransform.anchorMax = new Vector2(_sliderPos, 1f);

            _timeoutTimer -= Time.deltaTime;
            if (_timeoutTimer <= 0f)
            {
                _isActive = false;
                Hide();
                _onMissCallback?.Invoke();
            }
        }

        public void StartQte(System.Action onMiss)
        {
            _isActive = true;
            _sliderPos = 0f;
            _dir = 1;
            _speed = Random.Range(1.5f, 2.5f);
            _timeoutTimer = 5f;
            _onMissCallback = onMiss;

            gameObject.SetActive(true);
        }

        public float StopQte()
        {
            if (!_isActive) return 0f;

            _isActive = false;
            Hide();

            float dist = Mathf.Abs(0.5f - _sliderPos);

            float score = 1f - (dist * 2f);

            return Mathf.Clamp01(score);
        }

        private void Hide() => gameObject.SetActive(false);
    }
}
