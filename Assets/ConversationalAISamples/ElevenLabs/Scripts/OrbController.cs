using UnityEngine;
using UnityEngine.Assertions;

namespace ElevenLabs {
    public class OrbController : MonoBehaviour
    {
        [Header("User Speech Particles")]
        [SerializeField] private ParticleSystem innerPat;
        [SerializeField] private ParticleSystem glow;
        
        [Header("Agent Speech Particles")]
        [SerializeField] private ParticleSystem circle;
        
        [Header("VAD Score Thresholds")]
        [SerializeField] private float userSpeechThreshold = 0.001f;
        [SerializeField] private float agentSpeechThreshold = 1e-3f;
        
        [Header("Scale Settings")]
        [SerializeField] private float maxScaleMultiplier = 1.1f;
        [SerializeField] private float minScaleMultiplier = 0.95f;
        [SerializeField] private float scaleSmoothSpeed = 5f;

        private ParticleSystem.SizeOverLifetimeModule _innerSize;
        private ParticleSystem.SizeOverLifetimeModule _circleSize;
        private float _currentInnerScale = 1f;
        private float _currentCircleScale = 1f;
        private float _targetInnerScale = 1f;
        private float _targetCircleScale = 1f;

        private void Start()
        {
            Assert.IsNotNull(innerPat, "InnerPat not assigned!");
            _innerSize = innerPat.sizeOverLifetime;

            if (!circle) return;
            _circleSize = circle.sizeOverLifetime;
            _circleSize.enabled = true;
        }

        private void Update()
        {
            _currentInnerScale = Mathf.Lerp(_currentInnerScale, _targetInnerScale, Time.deltaTime * scaleSmoothSpeed);
            _currentCircleScale = Mathf.Lerp(_currentCircleScale, _targetCircleScale, Time.deltaTime * scaleSmoothSpeed);
            
            _innerSize.size = new ParticleSystem.MinMaxCurve(_currentInnerScale);
            if (circle)
            {
                _circleSize.size = new ParticleSystem.MinMaxCurve(_currentCircleScale);
            }
        }

        public void OnVadScore(float vadScore)
        {
            var isUserSpeech = vadScore > userSpeechThreshold;
            var isAgentSpeech = vadScore < agentSpeechThreshold && vadScore > 0;
            
            _targetInnerScale = isUserSpeech ? Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, Mathf.Clamp01(vadScore)) : 1f;
            _targetCircleScale = isAgentSpeech ? Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, Mathf.Clamp01(vadScore * 10000f)) : 1f;
        }
    }
}
