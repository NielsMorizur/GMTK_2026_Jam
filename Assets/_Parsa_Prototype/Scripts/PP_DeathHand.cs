using System.Collections;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Death claim: the hand rises and takes the player. The game pauses for
    /// the whole flipbook (played on real time), the player vanishes at the
    /// claim frame, then control returns and the level resets.
    /// </summary>
    public class PP_DeathHand : MonoBehaviour
    {
        [Tooltip("Flipbook frames, in order")]
        public Sprite[] frames;
        public float framesPerSecond = 18f;
        [Tooltip("Frame index at which the player disappears (claimed)")]
        public int claimFrame = 6;
        [Tooltip("Extra real-time pause after the last frame")]
        public float holdAfter = 0.2f;
        [Tooltip("The hand renderer (child of the player)")]
        public SpriteRenderer hand;

        public bool IsPlaying { get; private set; }

        SpriteRenderer _playerSprite;
        SpriteRenderer _glow;

        void Awake()
        {
            var anim = GetComponentInChildren<Animator>();
            _playerSprite = anim != null ? anim.GetComponent<SpriteRenderer>() : null;
            var glowT = transform.Find("PlayerGlow");
            _glow = glowT != null ? glowT.GetComponent<SpriteRenderer>() : null;

            if (hand != null)
                hand.gameObject.SetActive(false);   // authored visible for editing
        }

        public void PlayClaim(System.Action onDone)
        {
            if (IsPlaying || hand == null || frames == null || frames.Length == 0)
            {
                onDone?.Invoke();
                return;
            }
            StartCoroutine(Claim(onDone));
        }

        IEnumerator Claim(System.Action onDone)
        {
            IsPlaying = true;
            Time.timeScale = 0f;
            hand.gameObject.SetActive(true);

            float dt = 1f / Mathf.Max(1f, framesPerSecond);
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) hand.sprite = frames[i];
                if (i == claimFrame) SetPlayerVisible(false);

                float t = 0f;
                while (t < dt) { t += Time.unscaledDeltaTime; yield return null; }
            }

            float h = 0f;
            while (h < holdAfter) { h += Time.unscaledDeltaTime; yield return null; }

            hand.gameObject.SetActive(false);
            SetPlayerVisible(true);
            Time.timeScale = PP_PauseMenu.IsOpen ? 0f : 1f;
            IsPlaying = false;
            onDone?.Invoke();
        }

        void SetPlayerVisible(bool on)
        {
            if (_playerSprite != null) _playerSprite.enabled = on;
            if (_glow != null) _glow.enabled = on;
        }
    }
}
