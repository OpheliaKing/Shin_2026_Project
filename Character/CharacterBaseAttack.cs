using UnityEngine;
using AYellowpaper.SerializedCollections;
using System;


namespace Shin
{
    public partial class CharacterBase
    {
        [Header("Attack")]
        [SerializeField]
        protected SerializedDictionary<INPUT_TYPE, string> _inputType = new SerializedDictionary<INPUT_TYPE, string>();
        [SerializeField]
        protected AttackData[] _attackData;
        public AttackData[] AttackData
        {
            get
            {
                return _attackData;
            }
        }

        [SerializeField]
        protected AttackInfoData[] _attackInfoData;
        public AttackInfoData[] AttackInfoData
        {
            get
            {
                return _attackInfoData;
            }
        }

        protected string _currentAttackTid;
        public string CurrentAttackTid
        {
            get
            {
                return _currentAttackTid;
            }
        }

        protected string _currentPlayAttackAnim;

        /// <summary>Animator/클립 기준으로 보이는 현재 공격 애니메이션 식별 문자열.</summary>
        public string CurrentPlayAttackAnim => _currentPlayAttackAnim;

        private bool _hasQueuedComboInput;
        private INPUT_TYPE _queuedComboInput;

        private bool _isZoomState;

        protected bool IsZoomState
        {
            get => _isZoomState;
            set
            {
                if (_isZoomState == value)
                {
                    return;
                }

                _isZoomState = value;
                ZoomStateChange();
            }
        }

        public void AttackInput(INPUT_TYPE inputType, bool isPressed)
        {
            if (!CharacterState.IsAttackAble())
            {
                return;
            }

            bool isAttackTid = _inputType.TryGetValue(inputType, out string attackTid);

            var inputAttackData = FindAttackData(attackTid);
            if (inputAttackData == null)
            {
                return;
            }

            if (isPressed)
            {
                switch (inputAttackData.AttackType)
                {
                    case ATTACK_TYPE.MELEE:
                        if (_currentAttackTid.IsNullOrEmpty())
                        {
                            if (isAttackTid)
                            {
                                Attack(attackTid);
                            }

                            return;
                        }

                        if (!IsInComboBufferableState(CharacterState))
                        {
                            return;
                        }

                        AttackData currentAttack = FindAttackData(_currentAttackTid);

                        if (currentAttack == null)
                        {
                            return;
                        }


                        if (currentAttack == null)
                        {
                            return;
                        }

                        if (!currentAttack.LinkedAttack.TryGetValue(inputType, out string nextTid) || string.IsNullOrEmpty(nextTid))
                        {
                            return;
                        }
                        _hasQueuedComboInput = true;
                        break;
                    case ATTACK_TYPE.PROJECTILE:
                        break;
                    case ATTACK_TYPE.HITSCAN:
                        break;
                    case ATTACK_TYPE.ZOOM:
                        ActiveZoom(true);
                        break;
                }

                _queuedComboInput = inputType;
            }
            else
            {
                switch (inputAttackData.AttackType)
                {
                    case ATTACK_TYPE.ZOOM:
                        ActiveZoom(false);
                        break;
                }
            }
        }

        public void Attack(string attackTid)
        {
            if (string.IsNullOrEmpty(attackTid))
            {
                return;
            }

            AttackData attackData = FindAttackData(attackTid);
            if (attackData == null)
            {
                return;
            }

            _hasQueuedComboInput = false;

            ChangeCharacterState(CHARACTER_STATE.ATTACK);

            attackData.AttackStartEvent?.Invoke();
            _animator.CrossFade(attackData.AnimationName, 0.2f);
            _currentAttackTid = attackTid;
        }

        private void LateUpdate()
        {
            TryConsumeQueuedComboAttack();
        }

        private static bool IsInComboBufferableState(CHARACTER_STATE state)
        {
            return state == CHARACTER_STATE.ATTACK || state == CHARACTER_STATE.ATTACK_MOVEABLE;
        }

        private void TryConsumeQueuedComboAttack()
        {
            if (!_hasQueuedComboInput)
            {
                return;
            }

            if (!IsInComboBufferableState(CharacterState))
            {
                _hasQueuedComboInput = false;
                return;
            }

            if (_currentAttackTid.IsNullOrEmpty())
            {
                _hasQueuedComboInput = false;
                return;
            }

            AttackData currentAttack = FindAttackData(_currentAttackTid);
            if (currentAttack == null)
            {
                _hasQueuedComboInput = false;
                return;
            }

            if (GetPrimaryLayerNormalizedTime() < currentAttack.NextAttackChainUnlockNormalizedTime)
            {
                return;
            }

            if (!currentAttack.LinkedAttack.TryGetValue(_queuedComboInput, out string nextTid) || string.IsNullOrEmpty(nextTid))
            {
                _hasQueuedComboInput = false;
                return;
            }

            _hasQueuedComboInput = false;
            Attack(nextTid);
        }

        private float GetPrimaryLayerNormalizedTime()
        {
            if (_animator == null)
            {
                return 0f;
            }

            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            float nt = info.normalizedTime;
            if (info.loop)
            {
                nt -= Mathf.Floor(nt);
            }
            else
            {
                nt = Mathf.Clamp01(nt);
            }

            return nt;
        }

        private AttackData FindAttackData(string attackTid)
        {
            if (_attackData == null || string.IsNullOrEmpty(attackTid))
            {
                return null;
            }

            return Array.Find(_attackData, data => data.Tid == attackTid);
        }

        partial void OnExitState_AttackComboHook(CHARACTER_STATE currentState, CHARACTER_STATE nextState)
        {
            if (currentState == CHARACTER_STATE.ATTACK || currentState == CHARACTER_STATE.ATTACK_MOVEABLE)
            {
                if (nextState != CHARACTER_STATE.ATTACK && nextState != CHARACTER_STATE.ATTACK_MOVEABLE)
                {
                    if (!_currentAttackTid.IsNullOrEmpty())
                    {
                        FindAttackData(_currentAttackTid)?.AttackEndEvent?.Invoke();
                    }

                    ClearAttackComboBufferAndCurrentTid();
                }
            }
        }

        /// <summary>공격 상태 애니메이션이 재생되기 시작했을 때(StateMachineBehaviour OnStateEnter).</summary>
        public virtual void OnCombatAnimationPlayStarted(string animatorStateDisplayName)
        {
            _currentPlayAttackAnim = animatorStateDisplayName;
        }

        /// <summary>공격 상태 애니메이션이 끝났을 때(StateMachineBehaviour OnStateExit).</summary>
        public virtual void OnCombatAnimationPlayEnded(string animatorStateDisplayName)
        {
            Debug.Log($"OnCombatAnimationPlayEnded: {animatorStateDisplayName}");
            if (_currentPlayAttackAnim == animatorStateDisplayName)
            {
                _currentPlayAttackAnim = null;
            }

            if (!IsInComboBufferableState(CharacterState))
            {
                return;
            }

            AttackData currentAttack = FindAttackData(_currentAttackTid);
            if (currentAttack == null)
            {
                return;
            }

            if (!DoesCombatAnimDisplayMatchAttack(currentAttack, animatorStateDisplayName))
            {
                return;
            }

            currentAttack.AttackEndEvent?.Invoke();
            ClearAttackComboBufferAndCurrentTid();
            ChangeCharacterState(CHARACTER_STATE.IDLE);
        }

        private static bool DoesCombatAnimDisplayMatchAttack(AttackData attack, string animatorStateDisplayName)
        {
            if (attack == null || animatorStateDisplayName.IsNullOrEmpty())
            {
                return false;
            }

            return animatorStateDisplayName == attack.AnimationName;
        }

        private void ClearAttackComboBufferAndCurrentTid()
        {
            _hasQueuedComboInput = false;
            _currentAttackTid = null;
        }

        /// <summary>애니메이션 정규화 시간 구간에서 데미지 판정이 필요할 때 호출됩니다.</summary>
        /// <param name="animatorStateDisplayName">시작/종료 알림과 동일한 식별 문자열(클립 이름 또는 오버라이드).</param>
        /// <param name="hitWindowIndex">인스펙터에서 설정한 히트 윈도우 순번(0부터).</param>
        /// <param name="normalizedTime">현재 상태의 정규화 재생 시점(대략 0~1, 루프 시 반복).</param>
        /// <param name="attackInfoDataTid"><see cref="CombatHitPhaseWindow.AttackInfoDataTid"/>에 설정한 값.</param>
        public virtual void AttackToAnimation(string animatorStateDisplayName, int hitWindowIndex, float normalizedTime, string attackInfoDataTid)
        {
            Debug.Log($"AttackToAnimation: {animatorStateDisplayName}, {hitWindowIndex}, {normalizedTime}, {attackInfoDataTid}");
        }

        protected virtual void ActiveZoom(bool isActive)
        {
            IsZoomState = isActive;
        }

        protected virtual void ZoomStateChange()
        {
            Debug.Log($"ZoomStateChange: {IsZoomState}");
        }
    }

    public enum INPUT_TYPE
    {
        LEFT_CLICK,
        RIGHT_CLICK,
        LEFT_SHIFT,
        SPACE,
        Q,
        E,
    }

    public enum ATTACK_TYPE
    {
        MELEE,
        PROJECTILE,
        HITSCAN,
        ZOOM,
    }

    [Serializable]
    public class AttackData
    {
        public string Tid;
        public ATTACK_TYPE AttackType;
        public string AnimationName;

        [Range(0f, 1f)]
        [Tooltip("현재 공격 애니메이션(레이어 0) 정규화 시간이 이 값 이상일 때, 버퍼에 쌓인 LinkedAttack 입력으로 다음 공격이 실행됩니다.")]
        public float NextAttackChainUnlockNormalizedTime = 0.35f;

        public SerializedDictionary<INPUT_TYPE, string> LinkedAttack = new SerializedDictionary<INPUT_TYPE, string>();

        public Action AttackStartEvent;
        public Action AttackEndEvent;
    }
    [Serializable]
    public class AttackInfoData
    {
        //MELEE
        public Vector3 HitBoxSize;
        public Vector3 HitBoxOffset;
        public Vector3 HitBoxRotation;
        public float HitBoxDuration;

        //PROJECTILE
        public GameObject Projectile;
    }
}