using System.Collections;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Hazard death: the player becomes a ghost. The game freezes, the body
    /// vanishes, the ghost animation plays once while drifting upward, then
    /// the level resets. Time-out deaths keep the hand claim instead.
    /// </summary>
    public class PP_DeathGhost : MonoBehaviour
    {
        [Tooltip("Ghost flipbook, played once in order")]
        public Sprite[] frames;
        public float framesPerSecond = 16f;
        [Tooltip("How many of the first frames linger longer")]
        public int startHoldFrames = 3;
        [Tooltip("Duration multiplier for those first frames")]
        public float startHoldMultiplier = 2.2f;
        [Tooltip("Upward drift while the ghost plays, units per real second")]
        public float riseSpeed = 1.2f;
        [Tooltip("Extra real-time pause after the last frame")]
        public float holdAfter = 0.15f;
        [Tooltip("The ghost renderer (child of the player)")]
        public SpriteRenderer ghost;

        public bool IsPlaying { get; private set; }

        Player _player;
        SpriteRenderer _playerSprite;
        SpriteRenderer _glow;

        void Awake()
        {
            _player = GetComponent<Player>();
            var anim = GetComponentInChildren<Animator>();
            _playerSprite = anim != null ? anim.GetComponent<SpriteRenderer>() : null;
            var glowT = transform.Find("PlayerGlow");
            _glow = glowT != null ? glowT.GetComponent<SpriteRenderer>() : null;

            if (ghost != null)
                ghost.gameObject.SetActive(false);   // authored visible for editing
        }

        public void PlayGhost(System.Action onDone)
        {
            // a second hazard touching him in the same frame must be ignored
            // outright: running onDone here would reset the level mid-ghost
            if (IsPlaying || PP_DeathHand.AnyPlaying) return;

            if (ghost == null || frames == null || frames.Length == 0)
            {
                onDone?.Invoke();
                return;
            }
            PP_DeathCounter.Add();
            StartCoroutine(Run(onDone));
        }

        IEnumerator Run(System.Action onDone)
        {
            IsPlaying = true;
            PP_DeathHand.AnyPlaying = true;
            Time.timeScale = 0f;

            // darkness closes in over the whole ghost rise
            float fdt = 1f / Mathf.Max(1f, framesPerSecond);
            float total = startHoldFrames * fdt * startHoldMultiplier
                          + Mathf.Max(0, frames.Length - startHoldFrames) * fdt
                          + holdAfter;
            PP_ScreenFade.FadeOut(total);

            if (_player != null && _player.input != null)
                _player.input.Disable();

            SetPlayerVisible(false);
            ghost.gameObject.SetActive(true);
            Vector3 basePos = ghost.transform.localPosition;

            // the ghost rises where he died: detached, so a respawn snap
            // (or anything else moving the player) cannot carry it along
            Transform parent = ghost.transform.parent;
            Quaternion baseRot = ghost.transform.localRotation;
            Vector3 baseScale = ghost.transform.localScale;
            ghost.transform.SetParent(null, true);

            float dt = 1f / Mathf.Max(1f, framesPerSecond);
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) ghost.sprite = frames[i];

                // the first frames linger so the transformation reads clearly
                float frameDt = i < startHoldFrames ? dt * startHoldMultiplier : dt;
                float t = 0f;
                while (t < frameDt)
                {
                    t += Time.unscaledDeltaTime;
                    ghost.transform.localPosition += Vector3.up * (riseSpeed * Time.unscaledDeltaTime);
                    yield return null;
                }
            }

            float h = 0f;
            while (h < holdAfter) { h += Time.unscaledDeltaTime; yield return null; }

            // the respawn snap happens under a black wipe
            PP_ScreenFade.Wipe(() =>
            {
                ghost.gameObject.SetActive(false);
                ghost.transform.SetParent(parent, false);
                ghost.transform.localPosition = basePos;
                ghost.transform.localRotation = baseRot;
                ghost.transform.localScale = baseScale;
                SetPlayerVisible(true);

                if (_player != null && _player.input != null)
                    _player.input.Enable();

                Time.timeScale = PP_PauseMenu.IsOpen ? 0f : 1f;
                IsPlaying = false;
                PP_DeathHand.AnyPlaying = false;
                onDone?.Invoke();
            });
        }

        void SetPlayerVisible(bool on)
        {
            if (_playerSprite != null) _playerSprite.enabled = on;
            if (_glow != null) _glow.enabled = on;
        }
    }
}
