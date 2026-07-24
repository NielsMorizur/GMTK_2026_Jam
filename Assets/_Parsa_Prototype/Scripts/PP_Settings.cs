using UnityEngine;

namespace ParsaProto
{
    /// <summary>Player-facing settings, persisted across sessions.</summary>
    public static class PP_Settings
    {
        const string ShakeKey = "pp_disable_screenshake";

        public static bool ScreenShakeDisabled
        {
            get => PlayerPrefs.GetInt(ShakeKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
