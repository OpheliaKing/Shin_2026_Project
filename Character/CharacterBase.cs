using UnityEngine;
using System;

namespace Shin
{
    public partial class CharacterBase : MonoBehaviour
    {
        private int _health;
        private int _maxHealth;

        private CHARACTER_STATE _previousCharacterState = CHARACTER_STATE.NONE;
        [SerializeField]
        private CHARACTER_STATE _characterState = CHARACTER_STATE.NONE;

        public event Action<CHARACTER_STATE, CHARACTER_STATE> OnCharacterStateChanged;

        public CHARACTER_STATE CharacterState
        {
            get
            {
                return _characterState;
            }
        }

        public CHARACTER_STATE PreviousCharacterState
        {
            get
            {
                return _previousCharacterState;
            }
        }
        
        protected virtual void Awake()
        {
            Init();
        }

        protected virtual void Init()
        {

        }

        protected bool ChangeCharacterState(CHARACTER_STATE nextState, bool forceChange = false)
        {
            if (!forceChange && _characterState == nextState)
            {
                return false;
            }

            if (!forceChange && !CanChangeState(_characterState, nextState))
            {
                return false;
            }

            CHARACTER_STATE prevState = _characterState;

            OnExitState(prevState, nextState);

            _previousCharacterState = prevState;
            _characterState = nextState;

            OnEnterState(prevState, nextState);
            OnCharacterStateChanged?.Invoke(prevState, nextState);
            return true;
        }

        protected virtual bool CanChangeState(CHARACTER_STATE currentState, CHARACTER_STATE nextState)
        {
            return true;
        }

        protected virtual void OnEnterState(CHARACTER_STATE previousState, CHARACTER_STATE currentState)
        {
        }

        protected virtual void OnExitState(CHARACTER_STATE currentState, CHARACTER_STATE nextState)
        {
        }

        protected virtual void Update()
        {
            SetMoveAnimationByInputDirection();
        }
    }


    public enum CHARACTER_STATE
    {
        NONE,
        IDLE,
        MOVE,
        MOVE_RUN,
        ATTACK,
        ATTACK_MOVEABLE,
        HIT,
        DIE,
    }

}