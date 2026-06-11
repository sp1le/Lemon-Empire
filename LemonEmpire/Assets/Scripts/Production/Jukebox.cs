using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class Jukebox : MonoBehaviour, IInteractable
    {
        private bool _isPlaying = false;
        private AudioSource _audioSource;

        public bool IsPlaying => _isPlaying;

        public string InteractionPrompt => _isPlaying ? "[E] Выключить музыку" : "[E] Включить Lo-Fi музыку";

        public bool CanInteract => UpgradeManager.HasJukebox;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void Interact(PlayerInteractionContext context)
        {
            _isPlaying = !_isPlaying;

            if (_audioSource != null)
            {
                if (_isPlaying) _audioSource.Play();
                else _audioSource.Stop();
            }

            Debug.Log($"[Jukebox] Music toggled. Playing: {_isPlaying}");
        }

        private void Update()
        {
            if (!_isPlaying) return;

            var vitals = PlayerVitals.Instance;
            if (vitals != null)
            {
                float distance = Vector3.Distance(transform.position, vitals.transform.position);
                if (distance <= 5.0f)
                {
                    vitals.ReplenishMorale(1.0f * Time.deltaTime);
                }
            }
        }
    }
}
