using UnityEngine;
using UnityEngine.UIElements;

namespace LemonEmpire.Production
{
    public class ProgressUI : MonoBehaviour
    {
        private UIDocument _uiDoc;
        private VisualElement _root;
        private VisualElement _fill;
        private Label _label;

        private void Awake()
        {
            _uiDoc = gameObject.AddComponent<UIDocument>();
            var settings = Resources.Load<PanelSettings>("TabletPanelSettings");
            if (settings != null)
            {
                _uiDoc.panelSettings = settings;
            }

            // Create Visual Element container centered at bottom-middle of screen
            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.left = Length.Percent(50);
            container.style.bottom = 150f; // Placed neatly above HUD hotbar
            container.style.width = 300f;
            container.style.height = 76f;
            container.style.marginLeft = -150f; // Center by offsetting half width
            
            // Sleek dark modern glassmorphism styling
            container.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.90f));
            container.style.borderTopLeftRadius = 12f;
            container.style.borderTopRightRadius = 12f;
            container.style.borderBottomLeftRadius = 12f;
            container.style.borderBottomRightRadius = 12f;
            
            // Subtle gold/brown border trim
            container.style.borderLeftWidth = 1f;
            container.style.borderRightWidth = 1f;
            container.style.borderTopWidth = 1f;
            container.style.borderBottomWidth = 1f;
            container.style.borderLeftColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
            container.style.borderRightColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
            container.style.borderTopColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
            container.style.borderBottomColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
            
            container.style.paddingLeft = 16f;
            container.style.paddingRight = 16f;
            container.style.paddingTop = 10f;
            container.style.paddingBottom = 10f;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;

            // QTE Gold Label
            _label = new Label("");
            _label.style.fontSize = 18f;
            _label.style.color = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f)); // Gold color
            _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            _label.style.marginBottom = 6f;
            container.Add(_label);

            // Progress bar track
            var track = new VisualElement();
            track.style.width = Length.Percent(100);
            track.style.height = 12f;
            track.style.backgroundColor = new StyleColor(new Color(0.16f, 0.16f, 0.18f, 1f));
            track.style.borderTopLeftRadius = 4f;
            track.style.borderTopRightRadius = 4f;
            track.style.borderBottomLeftRadius = 4f;
            track.style.borderBottomRightRadius = 4f;
            container.Add(track);

            // Green fill
            _fill = new VisualElement();
            _fill.style.width = Length.Percent(0);
            _fill.style.height = Length.Percent(100);
            _fill.style.backgroundColor = new StyleColor(new Color(0.33f, 0.87f, 0.42f, 1f)); // Green
            _fill.style.borderTopLeftRadius = 4f;
            _fill.style.borderTopRightRadius = 4f;
            _fill.style.borderBottomLeftRadius = 4f;
            _fill.style.borderBottomRightRadius = 4f;
            track.Add(_fill);

            _root = container;
            _uiDoc.rootVisualElement.Add(_root);

            Hide();
        }

        public void Show()
        {
            if (_fill != null) _fill.style.width = Length.Percent(0);
            if (_label != null) _label.text = "";
            if (_root != null) _root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        public void UpdateProgress(float fillAmount)
        {
            if (_fill != null)
            {
                _fill.style.width = Length.Percent(Mathf.Clamp01(fillAmount) * 100f);
            }
        }

        public void SetLabelText(string text)
        {
            if (_label != null)
            {
                _label.text = text;
            }
        }
    }
}
