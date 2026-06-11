using UnityEngine;
using UnityEngine.UI;
using LemonEmpire.Core;

namespace LemonEmpire.UI
{
    public class VitalsUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _satietyFill;
        [SerializeField] private Image _moraleFill;
        [SerializeField] private Text _satietyText;
        [SerializeField] private Text _moraleText;

        private readonly Color _satietyColor = new Color(0.98f, 0.78f, 0.25f);
        private readonly Color _satietyLowColor = new Color(0.85f, 0.25f, 0.20f);
        private readonly Color _moraleColor = new Color(0.30f, 0.82f, 0.60f);
        private readonly Color _moraleLowColor = new Color(0.60f, 0.30f, 0.15f);

        private void Start()
        {
            RefreshUI();

            if (PlayerVitals.Instance != null)
                PlayerVitals.Instance.OnVitalsChanged += RefreshUI;
        }

        private void OnDestroy()
        {
            if (PlayerVitals.Instance != null)
                PlayerVitals.Instance.OnVitalsChanged -= RefreshUI;
        }

        private void RefreshUI()
        {
            var v = PlayerVitals.Instance;
            if (v == null) return;

            // Satiety
            if (_satietyFill != null)
            {
                float satNorm = v.Satiety / 100f;
                _satietyFill.rectTransform.anchorMax = new Vector2(satNorm, 1f);
                _satietyFill.color = Color.Lerp(_satietyLowColor, _satietyColor, satNorm);
            }
            if (_satietyText != null)
                _satietyText.text = $"{Mathf.CeilToInt(v.Satiety)}";

            // Morale
            if (_moraleFill != null)
            {
                float morNorm = v.Morale / 100f;
                _moraleFill.rectTransform.anchorMax = new Vector2(morNorm, 1f);
                _moraleFill.color = Color.Lerp(_moraleLowColor, _moraleColor, morNorm);
            }
            if (_moraleText != null)
                _moraleText.text = $"{Mathf.CeilToInt(v.Morale)}";
        }
    }
}
