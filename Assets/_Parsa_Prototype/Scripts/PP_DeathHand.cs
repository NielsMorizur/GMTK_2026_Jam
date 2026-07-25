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
        [Tooltip("Frame (numbered like the art files, from 1) where the player starts shrinking in the grip")]
        public int shrinkStartFrame = 7;
        [Tooltip("Frame (numbered from 1) where the player is fully gone")]
        public int claimFrame = 11;
        [Tooltip("Extra real-time pause after the last frame")]
        public float holdAfter = 0.2f;
        [Tooltip("Real-time seconds for the player to fade to black at death")]
        public float tintFadeTime = 0.25f;
        [Tooltip("The hand renderer (child of the player)")]
        public SpriteRenderer hand;

        public bool IsPlaying { get; private set; }

        /// <summary>True while any death claim is playing; gameplay and menu
        /// inputs check this to stay silent.</summary>
        public static bool AnyPlaying { get; private set; }

        Player _player;
        SpriteRenderer _playerSprite;
        SpriteRenderer _glow;

        void Awake()
        {
            AnyPlaying = false;
            _player = GetComponent<Player>();
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
            AnyPlaying = true;
            Time.timeScale = 0f;
            hand.gameObject.SetActive(true);

            // dead men press no buttons
            if (_player != null && _player.input != null)
                _player.input.Disable();

            // hard black the moment death takes hold; the halo goes out
            Color originalTint = _playerSprite != null ? _playerSprite.color : Color.white;
            if (_glow != null) _glow.enabled = false;

            // the squash-stretch spring owns the sprite's scale every frame;
            // pause it while the grip shrinks him
            var squash = GetComponent<PP_SquashStretch>();
            if (squash != null) squash.enabled = false;
            Transform spriteT = _playerSprite != null ? _playerSprite.transform : null;
            Vector3 baseScale = spriteT != null ? spriteT.localScale : Vector3.one;

            float dt = 1f / Mathf.Max(1f, framesPerSecond);
            int shrinkSpan = Mathf.Max(1, claimFrame - shrinkStartFrame);
            float seqTime = 0f;

            for (int i = 0; i < frames.Length; i++)
            {
                int frameNo = i + 1;   // matches the art file numbering
                if (frames[i] != null) hand.sprite = frames[i];
                if (frameNo == claimFrame) SetPlayerVisible(false);

                float t = 0f;
                while (t < dt)
                {
                    t += Time.unscaledDeltaTime;
                    seqTime += Time.unscaledDeltaTime;

                    // fade to black, then go transparent as the grip closes.
                    // enforced every frame so nothing else can recolor him.
                    float p = 0f;
                    if (frameNo >= shrinkStartFrame && frameNo < claimFrame)
                        p = Mathf.Clamp01(((frameNo - shrinkStartFrame) + t / dt) / shrinkSpan);

                    if (_playerSprite != null)
                    {
                        float k = Mathf.Clamp01(seqTime / Mathf.Max(0.01f, tintFadeTime));
                        Color c = Color.Lerp(originalTint, Color.black, k);
                        c.a = 1f - p;
                        _playerSprite.color = c;
                    }

                    // squeezed smaller and smaller between grip start and claim
                    if (spriteT != null && p > 0f)
                        spriteT.localScale = baseScale * (1f - p);
                    yield return null;
                }
            }

            float h = 0f;
            while (h < holdAfter) { h += Time.unscaledDeltaTime; yield return null; }

            hand.gameObject.SetActive(false);
            if (spriteT != null) spriteT.localScale = baseScale;
            if (_playerSprite != null) _playerSprite.color = originalTint;
            if (_glow != null) _glow.enabled = true;
            SetPlayerVisible(true);
            if (squash != null) squash.enabled = true;
            if (_player != null && _player.input != null)
                _player.input.Enable();
            Time.timeScale = PP_PauseMenu.IsOpen ? 0f : 1f;
            IsPlaying = false;
            AnyPlaying = false;
            onDone?.Invoke();
        }

        void SetPlayerVisible(bool on)
        {
            if (_playerSprite != null) _playerSprite.enabled = on;
            if (_glow != null) _glow.enabled = on;
        }
    }
}
