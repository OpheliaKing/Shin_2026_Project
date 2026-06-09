using UnityEngine;

namespace Shin
{
    public partial class CharacterBase
    {
        private const string ANIM_PARAM_MOVE_X = "MoveX";
        private const string ANIM_PARAM_MOVE_Y = "MoveY";
        protected const string ANIM_PARAM_UPPER_Y = "UpperY";

        [SerializeField, Min(0f)]
        private float _moveAnimationLerpSpeed = 12f;

        private Animator _animator;

        private Animator Animator
        {
            get
            {
                if (_animator == null)
                {
                    _animator = GetComponentInChildren<Animator>();
                }
                return _animator;
            }
        }

        public void SetAnimatorBool(string name, bool value)
        {
            Animator.SetBool(name, value);
        }

        public void SetAnimatorFloat(string name, float value)
        {
            Animator.SetFloat(name, value);
        }

        public void SetMoveAnimationByInputDirection()
        {
            bool isAi = _characterAIState == CHARACTER_AI_STATE.AI;
            Vector2 inputDirection = isAi ? GetAIMoveAnimationInput() : IntendedMoveDirection;

            if (!CharacterState.IsMoveAble())
            {
                if (!isAi)
                {
                    return;
                }

                inputDirection = Vector2.zero;
            }

            float movementMaxValue = GetMovementAnimationMaxValue();
            float targetMoveX = Mathf.Clamp(inputDirection.x, -1f, 1f) * movementMaxValue;
            float targetMoveY = Mathf.Clamp(inputDirection.y, -1f, 1f) * movementMaxValue;

            float currentMoveX = Animator.GetFloat(ANIM_PARAM_MOVE_X);
            float currentMoveY = Animator.GetFloat(ANIM_PARAM_MOVE_Y);

            float t = Mathf.Clamp01(Time.deltaTime * _moveAnimationLerpSpeed);
            float nextMoveX = Mathf.Lerp(currentMoveX, targetMoveX, t);
            float nextMoveY = Mathf.Lerp(currentMoveY, targetMoveY, t);

            if (Animator.runtimeAnimatorController == null)
            {
                return;
            }

            Animator.SetFloat(ANIM_PARAM_MOVE_X, nextMoveX);
            Animator.SetFloat(ANIM_PARAM_MOVE_Y, nextMoveY);
        }

        private float GetMovementAnimationMaxValue()
        {
            return _movementState switch
            {
                MOVEMENT_STATE.WALK => 1f,
                MOVEMENT_STATE.RUN => 2f,
                MOVEMENT_STATE.DASH => 3f,
                _ => 1f,
            };
        }

        public void SetWeight(string name, float weight)
        {
            Animator.SetLayerWeight(Animator.GetLayerIndex(name), weight);
        }
    }
}
