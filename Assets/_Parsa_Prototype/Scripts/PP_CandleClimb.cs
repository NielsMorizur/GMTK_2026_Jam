using System.Collections;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// The intro's closing cinematic: the elder hops up ascending candles one
    /// by one; the moment he leaves a candle, it lights (its Glow child turns
    /// on). After the last candle he walks off and the scene fades into the
    /// next one. Started by the director's IntroSectionBegin.
    /// </summary>
    public class PP_CandleClimb : MonoBehaviour
    {
        [Tooltip("The elder visual that hops")]
        public Transform elder;
        [Tooltip("Candle roots in climb order; each lights its 'Glow' child when he leaves it")]
        public Transform[] candles;
        [Tooltip("World offset from a candle's position where he stands")]
        public Vector2 standOffset = new Vector2(0f, 1.1f);

        [Header("Entrance")]
        [Tooltip("Candles start this far below their spots and float up")]
        public float riseFrom = 8f;
        [Tooltip("Seconds each candle takes to float up")]
        public float riseTime = 0.7f;
        [Tooltip("Seconds between one candle starting to rise and the next")]
        public float riseStagger = 0.3f;

        [Header("Hops")]
        public float startPause = 0.8f;
        public float hopDuration = 0.55f;
        [Tooltip("Extra arc height above the straight line, world units")]
        public float hopHeight = 1.2f;
        public float pauseOnCandle = 0.4f;

        [Header("Ending")]
        [Tooltip("Where the final off-screen leap carries him, relative to the last candle")]
        public Vector2 exitLeap = new Vector2(2.5f, 8f);
        public float exitLeapTime = 1f;
        [Tooltip("The screen fades over this many seconds while he leaves")]
        public float exitFadeTime = 0.9f;
        public string nextScene = "MainRoom";

        [Tooltip("Optional animator on the elder: JumpFall on hops, Idle on candles")]
        public Animator anim;
        [Tooltip("Optional sound bank on the elder (jump/landing per hop)")]
        public PP_SfxPlayer sfx;

        bool _running;

        void IntroSectionBegin()
        {
            if (!_running) StartCoroutine(Climb());
        }

        IEnumerator Climb()
        {
            _running = true;
            if (elder == null || candles == null || candles.Length == 0) yield break;

            // the camera stops chasing him here; he climbs out of frame
            var fall = elder.GetComponent<PP_IntroFall>();
            if (fall != null) fall.ReleaseCamera();

            // the candles float up from below, one at a time
            var homes = new Vector3[candles.Length];
            for (int i = 0; i < candles.Length; i++)
            {
                if (candles[i] == null) continue;
                homes[i] = candles[i].position;
                candles[i].position = homes[i] + Vector3.down * riseFrom;
            }
            float rt = 0f;
            float riseTotal = (candles.Length - 1) * riseStagger + riseTime;
            while (rt < riseTotal)
            {
                rt += Time.unscaledDeltaTime;
                for (int i = 0; i < candles.Length; i++)
                {
                    if (candles[i] == null) continue;
                    float k = Mathf.Clamp01((rt - i * riseStagger) / Mathf.Max(0.05f, riseTime));
                    candles[i].position = homes[i]
                        + Vector3.down * (riseFrom * (1f - Mathf.SmoothStep(0f, 1f, k)));
                }
                yield return null;
            }
            for (int i = 0; i < candles.Length; i++)
                if (candles[i] != null) candles[i].position = homes[i];

            float t = 0f;
            while (t < startPause) { t += Time.unscaledDeltaTime; yield return null; }

            Transform from = null;
            foreach (var candle in candles)
            {
                if (candle == null) continue;
                Vector3 a = elder.position;
                Vector3 b = candle.position + (Vector3)standOffset;

                if (anim != null) anim.CrossFade("JumpFall", 0.05f);
                if (sfx != null) sfx.Play(sfx.jump.For(AgeStage.Elderly));
                t = 0f;
                while (t < hopDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / hopDuration);
                    Vector3 p = Vector3.Lerp(a, b, k);
                    p.y += hopHeight * 4f * k * (1f - k);   // parabolic arc
                    elder.position = p;
                    yield return null;
                }
                elder.position = b;
                if (anim != null) anim.CrossFade("Idle", 0.08f);
                if (sfx != null) sfx.Play(sfx.landing.For(AgeStage.Elderly));

                // leaving the PREVIOUS candle lit it; light this one as he
                // pushes off next loop
                if (from != null) Light(from);
                from = candle;

                t = 0f;
                while (t < pauseOnCandle) { t += Time.unscaledDeltaTime; yield return null; }
            }
            if (from != null) Light(from);   // the last one lights as he leaves

            // one last leap off the top of the screen while the world fades
            if (anim != null) anim.CrossFade("JumpFall", 0.05f);
            if (sfx != null) sfx.Play(sfx.jump.For(AgeStage.Elderly));
            PP_ScreenFade.FadeOut(exitFadeTime);
            Vector3 s0 = elder.position;
            Vector3 s1 = s0 + (Vector3)exitLeap;
            t = 0f;
            while (t < exitLeapTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / exitLeapTime);
                Vector3 p = Vector3.Lerp(s0, s1, k);
                p.y += 1.2f * 4f * k * (1f - k);
                elder.position = p;
                yield return null;
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
        }

        void Light(Transform candle)
        {
            var glow = candle.Find("Glow");
            if (glow != null) glow.gameObject.SetActive(true);
        }
    }
}
