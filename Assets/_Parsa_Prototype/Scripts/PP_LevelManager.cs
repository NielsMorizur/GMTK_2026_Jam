using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Hub-and-spoke level flow in one scene. The hub has no timer and one
    /// portal per level; portals unlock in order as levels are completed.
    /// A level's PP_Exit calls LevelCompleted() which returns you to the hub.
    /// When every level is complete, the hub's big door opens.
    /// </summary>
    public class PP_LevelManager : MonoBehaviour
    {
        [System.Serializable]
        public class PP_Level
        {
            public string levelName;
            [Tooltip("Drag the level's PP_SpawnPoint marker here.")]
            public Transform spawnPoint;
            [Tooltip("Where the still camera sits for this level (z is kept from the camera).")]
            public Vector3 cameraPoint;
            [Tooltip("Forms available: 1 = old man only ... 4 = full loop.")]
            public int formCount = 4;
            [Tooltip("Playtest sandbox: no timer, E cycles forms freely, completion never counts.")]
            public bool sandbox = false;
            [Tooltip("Player faces left on spawn/reset in this level (default: right).")]
            public bool faceLeft = false;
        }

        public PP_AgeLoop player;
        public Camera cam;
        public PP_Level[] levels;

        [Header("Hub")]
        [Tooltip("Drag the hub's PP_SpawnPoint marker here.")]
        public Transform hubSpawnPoint;
        public Vector3 hubCamera;
        [Tooltip("The final door in the hub; opens once every level is complete.")]
        public GameObject bigDoor;

        [Header("Editor Preview")]
        [Tooltip("Snap the camera to a view in the Game window: -1 = hub, otherwise a Levels list index. Edit-mode convenience; ignored in Play mode.")]
        public int previewLevelIndex = -1;

        int _lastPreview = -2;

        [Tooltip("Tick to save the camera's CURRENT position as the camera point of the previewed level (or the hub when previewing -1). Unticks itself.")]
        public bool saveCameraHere = false;

        int _current = -1;            // -1 = in the hub
        int _highestCompleted = -1;

        public bool InHub => _current < 0;
        public int CurrentIndex => _current;
        public int HighestCompleted => _highestCompleted;

        [Header("Debug")]
        [Tooltip("Where Play mode starts: -1 = hub, otherwise a Levels list index.")]
        public int startLevelIndex = -1;

        [Tooltip("Run the real countdown in the playground (E stops free-cycling and ages down normally). Live: flip during Play.")]
        public bool playgroundTimerOn = false;

        [Tooltip("Camera follows the player instead of sitting still. Live: flip during Play.")]
        public bool followPlayer = false;

        [Tooltip("How quickly the follow camera catches up. Lower = snappier.")]
        public float followSmoothTime = 0.15f;

        Vector3 _followVelocity;

        void Start()
        {
            if (cam == null) cam = Camera.main;

            // spawn markers are editor tools; the player never sees them
            HideMarker(hubSpawnPoint);
            if (levels != null)
                foreach (var lv in levels)
                    HideMarker(lv.spawnPoint);
            if (startLevelIndex < 0) EnterHub();
            else EnterLevel(startLevelIndex);
        }

        void Update()
        {
            // live-apply the playground timer toggle while inside a sandbox level
            if (_current >= 0 && levels != null && _current < levels.Length
                && levels[_current].sandbox && player != null)
                player.freeCycle = !playgroundTimerOn;
        }

        void LateUpdate()
        {
            if (!followPlayer || cam == null || player == null) return;
            Vector3 goal = new Vector3(player.transform.position.x,
                                       player.transform.position.y + 1f,
                                       cam.transform.position.z);
            cam.transform.position = Vector3.SmoothDamp(
                cam.transform.position, goal, ref _followVelocity, followSmoothTime);
        }

        public bool IsUnlocked(int levelIndex) => levelIndex == _highestCompleted + 1;

        public void EnterHub()
        {
            if (player == null) return;
            _current = -1;
            player.activeFormCount = 1;      // you idle in the hub as the old man
            player.timerPaused = true;
            player.freeCycle = false;
            player.spawnFacing = 1;
            if (hubSpawnPoint != null) player.SetSpawn(hubSpawnPoint.position);
            SnapCamera(hubCamera);
            player.RestartLevel();
        }

        public void EnterLevel(int index)
        {
            if (levels == null || levels.Length == 0 || player == null) return;

            _current = Mathf.Clamp(index, 0, levels.Length - 1);
            PP_Level lv = levels[_current];

            player.activeFormCount = lv.formCount;
            player.timerPaused = false;
            player.freeCycle = lv.sandbox && !playgroundTimerOn;
            player.spawnFacing = lv.faceLeft ? -1 : 1;
            if (lv.spawnPoint != null) player.SetSpawn(lv.spawnPoint.position);
            SnapCamera(lv.cameraPoint);
            player.RestartLevel();
        }

        /// <summary>Called by a level's exit: back to the hub, next portal unlocked.</summary>
        public void LevelCompleted()
        {
            if (_current >= 0 && !levels[_current].sandbox)
                _highestCompleted = Mathf.Max(_highestCompleted, _current);

            if (bigDoor != null && AllRealLevelsDone())
                bigDoor.SetActive(false);    // every real level done: the big door opens

            EnterHub();
        }

        void OnValidate()
        {
            if (Application.isPlaying) return;

            if (saveCameraHere)
            {
                saveCameraHere = false;
                Camera sc = cam != null ? cam : Camera.main;
                if (sc != null)
                {
                    Vector3 cp = sc.transform.position;
                    if (previewLevelIndex >= 0 && levels != null && previewLevelIndex < levels.Length)
                        levels[previewLevelIndex].cameraPoint = new Vector3(cp.x, cp.y, 0f);
                    else
                        hubCamera = new Vector3(cp.x, cp.y, 0f);
                }
                return;
            }

            if (previewLevelIndex == _lastPreview) return;
            _lastPreview = previewLevelIndex;

            Camera c = cam != null ? cam : Camera.main;
            if (c == null) return;

            Vector3 point = hubCamera;
            if (previewLevelIndex >= 0 && levels != null && previewLevelIndex < levels.Length)
                point = levels[previewLevelIndex].cameraPoint;

            c.transform.position = new Vector3(point.x, point.y, c.transform.position.z);
        }

        bool AllRealLevelsDone()
        {
            if (levels == null || levels.Length == 0) return false;
            for (int i = 0; i < levels.Length; i++)
                if (!levels[i].sandbox && i > _highestCompleted) return false;
            return true;
        }

        static void HideMarker(Transform t)
        {
            if (t == null) return;
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        void SnapCamera(Vector3 pos)
        {
            if (cam != null)
                cam.transform.position = new Vector3(pos.x, pos.y, cam.transform.position.z);
        }
    }
}
