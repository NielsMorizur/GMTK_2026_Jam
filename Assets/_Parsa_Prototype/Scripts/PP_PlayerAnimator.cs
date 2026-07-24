using UnityEngine;

namespace ParsaProto
{
    /// <summary>
    /// Plays each form's animation set. One controller slot per form (aging
    /// order); a form with an empty slot falls back to the plain static look.
    /// Temporarily all four slots can share one controller until each form's
    /// own frames arrive.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PP_PlayerController))]
    [RequireComponent(typeof(PP_AgeLoop))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PP_PlayerAnimator : MonoBehaviour
    {
        [Tooltip("One animator controller per form, in aging order (old man, adult, teen, baby). Empty = static look.")]
        public RuntimeAnimatorController[] formControllers = new RuntimeAnimatorController[4];

        Animator _animator;
        PP_PlayerController _ctrl;
        PP_AgeLoop _loop;
        SpriteRenderer _sprite;
        Sprite _defaultSprite;
        int _lastForm = -1;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _ctrl = GetComponent<PP_PlayerController>();
            _loop = GetComponent<PP_AgeLoop>();
            _sprite = GetComponent<SpriteRenderer>();
            _defaultSprite = _sprite.sprite;
        }

        void Update()
        {
            int form = _loop.FormIndex;
            if (form != _lastForm)
            {
                _lastForm = form;
                RuntimeAnimatorController ctrl =
                    (formControllers != null && form >= 0 && form < formControllers.Length)
                        ? formControllers[form] : null;

                _animator.runtimeAnimatorController = ctrl;
                _animator.enabled = ctrl != null;
                if (ctrl == null) _sprite.sprite = _defaultSprite;
            }

            if (!_animator.enabled) return;
            _animator.SetFloat("Speed", Mathf.Abs(_ctrl.MoveInput));
            _animator.SetBool("Grounded", _ctrl.IsGrounded);
        }
    }
}
