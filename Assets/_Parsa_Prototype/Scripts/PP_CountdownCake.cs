using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Birthday-cake countdown: the numbered candle on the cake shows the
    /// seconds left in the current form (values above 9 show the 9 candle).
    /// Runs alongside PP_TimeBar while it's being tweaked; meant to replace
    /// it eventually. Cake and Candle are plain child sprites, move and
    /// scale them freely in the prefab.
    /// </summary>
    public class PP_CountdownCake : MonoBehaviour
    {
        [Tooltip("Auto-found in the scene when empty.")]
        public LevelAgeController ageController;
        [Tooltip("Digit sprites 0-9, in order.")]
        public Sprite[] digits = new Sprite[10];
        [Tooltip("The renderer showing the candle digit.")]
        public SpriteRenderer candle;

        bool _searched;

        void Update()
        {
            if (candle == null || !Application.isPlaying) return;

            if (ageController == null && !_searched)
            {
                _searched = true;
                ageController = Object.FindFirstObjectByType<LevelAgeController>();
            }
            if (ageController == null) return;

            int remaining = Mathf.CeilToInt(ageController.TimeRemainingInStage);
            int shown = Mathf.Clamp(remaining, 0, 9);
            if (digits != null && shown < digits.Length && digits[shown] != null)
                candle.sprite = digits[shown];
        }
    }
}
