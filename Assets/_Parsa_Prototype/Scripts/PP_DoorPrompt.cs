using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// The little E keycap: floats over the player whenever he stands in
    /// range of the door that is actually open (the current level's door).
    /// Reads Niels' EnterDoor scripts via reflection so they stay untouched.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PP_DoorPrompt : MonoBehaviour
    {
        [Tooltip("World units above the player's position")]
        public float heightAbovePlayer = 1.5f;
        public float bobAmplitude = 0.12f;
        public float bobPeriod = 1.6f;
        [Tooltip("How fast the prompt fades in/out")]
        public float fadeSpeed = 10f;

        Player _player;
        SpriteRenderer _sr;
        readonly List<(MonoBehaviour door, FieldInfo inRange, FieldInfo level)> _doors = new();
        float _alpha;

        void Start()
        {
            _player = FindAnyObjectByType<Player>();
            _sr = GetComponent<SpriteRenderer>();
            SetAlpha(0f);

            foreach (var mb in FindObjectsByType<MonoBehaviour>())
            {
                var t = mb.GetType();
                if (!t.Name.StartsWith("EnterDoor")) continue;
                var inRange = t.GetField("playerInRange",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var level = t.GetField("levelToLoad");
                if (inRange != null && level != null)
                    _doors.Add((mb, inRange, level));
            }
        }

        void Update()
        {
            if (_player == null || _sr == null) return;

            bool show = false;
            if (!PP_DeathHand.AnyPlaying)
            {
                var gm = GameManager.Instance;
                foreach (var (door, inRange, level) in _doors)
                {
                    if (door == null || !(bool)inRange.GetValue(door)) continue;
                    if (gm == null || gm.GetLevel() == (string)level.GetValue(door))
                    {
                        show = true;
                        break;
                    }
                }
            }

            _alpha = Mathf.MoveTowards(_alpha, show ? 1f : 0f,
                fadeSpeed * Time.deltaTime * 0.25f);
            SetAlpha(_alpha);

            if (_alpha <= 0f) return;
            float bob = Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.2f, bobPeriod))
                        * bobAmplitude;
            transform.position = _player.transform.position
                                 + new Vector3(0f, heightAbovePlayer + bob, 0f);
        }

        void SetAlpha(float a)
        {
            var c = _sr.color;
            c.a = a;
            _sr.color = c;
        }
    }
}
