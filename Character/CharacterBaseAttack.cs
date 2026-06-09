using UnityEngine;
using AYellowpaper.SerializedCollections;
using System;


namespace Shin
{
    public partial class CharacterBase
    {
        partial void InitCombatHealth();
        partial void OnExitState_AttackComboHook(CHARACTER_STATE currentState, CHARACTER_STATE nextState);
        partial void OnAIAttackAnimationEnded(AttackData endedAttack);

        [Header("Combat")]
        [SerializeField]
        private int _maxHealth = 100;

        private int _health;

        public int Health => _health;
        public int MaxHealth => _maxHealth;

        public bool IsCombatAlive()
        {
            return CharacterState != CHARACTER_STATE.DIE && _health > 0;
        }

        partial void InitCombatHealth()
        {
            _maxHealth = Mathf.Max(1, _maxHealth);
            _health = _maxHealth;
        }

        /// <summary><see cref="CombatManager.ApplyDamage"/>에서만 호출합니다.</summary>
        internal void ReceiveCombatDamage(CharacterBase attacker, AttackInfoData attackInfo, int damageAmount)
        {
            if (damageAmount <= 0)
            {
                return;
            }

            _health = Mathf.Max(0, _health - damageAmount);
            OnCombatDamaged(attacker, attackInfo, damageAmount);

            Debug.Log($"ReceiveCombatDamage: {attacker.name} -> {name} | damageAmount={damageAmount} | health={_health}");

            if (_health <= 0 && CharacterState != CHARACTER_STATE.DIE)
            {
                ChangeCharacterState(CHARACTER_STATE.DIE, true);
            }
        }

        protected virtual void OnCombatDamaged(CharacterBase attacker, AttackInfoData attackInfo, int damageAmount)
        {
        }

        [SerializeField]
        private CHARACTER_FRIENDLY_TYPE _friendlyType = CHARACTER_FRIENDLY_TYPE.NONE;

        public CHARACTER_FRIENDLY_TYPE FriendlyType => _friendlyType;

        public bool IsAlly(CharacterBase other)
        {
            if (other == null || other == this)
            {
                return false;
            }

            if (_friendlyType == CHARACTER_FRIENDLY_TYPE.NEUTRAL || other._friendlyType == CHARACTER_FRIENDLY_TYPE.NEUTRAL)
            {
                return false;
            }

            if (IsPlayerSideFaction(_friendlyType) && IsPlayerSideFaction(other._friendlyType))
            {
                return true;
            }

            return _friendlyType == CHARACTER_FRIENDLY_TYPE.ENEMY && other._friendlyType == CHARACTER_FRIENDLY_TYPE.ENEMY;
        }

        public bool IsEnemy(CharacterBase other)
        {
            if (other == null || other == this)
            {
                return false;
            }

            if (_friendlyType == CHARACTER_FRIENDLY_TYPE.NEUTRAL || other._friendlyType == CHARACTER_FRIENDLY_TYPE.NEUTRAL)
            {
                return false;
            }

            bool selfPlayerSide = IsPlayerSideFaction(_friendlyType);
            bool otherPlayerSide = IsPlayerSideFaction(other._friendlyType);
            bool selfEnemySide = _friendlyType == CHARACTER_FRIENDLY_TYPE.ENEMY;
            bool otherEnemySide = other._friendlyType == CHARACTER_FRIENDLY_TYPE.ENEMY;

            return (selfPlayerSide && otherEnemySide) || (selfEnemySide && otherPlayerSide);
        }

        /// <summary>
        /// <paramref name="attacker"/>의 공격 진영 설정(<see cref="ATTACK_FRIENDLY_TYPE"/>)에 따라 피격 가능한지 판별합니다.
        /// <see cref="CHARACTER_FRIENDLY_TYPE.NEUTRAL"/>은 아군·적군 공격 모두에 맞습니다.
        /// </summary>
        public bool CanBeDamagedBy(CharacterBase attacker, ATTACK_FRIENDLY_TYPE attackFriendlyType)
        {
            if (attacker == null)
            {
                return false;
            }

            if (_friendlyType == CHARACTER_FRIENDLY_TYPE.NEUTRAL)
            {
                return true;
            }

            switch (attackFriendlyType)
            {
                case ATTACK_FRIENDLY_TYPE.FRIENDLY:
                    return attacker.IsAlly(this);
                case ATTACK_FRIENDLY_TYPE.ENEMY:
                    return attacker.IsEnemy(this);
                case ATTACK_FRIENDLY_TYPE.ALL:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsPlayerSideFaction(CHARACTER_FRIENDLY_TYPE type)
        {
            return type == CHARACTER_FRIENDLY_TYPE.PLAYER || type == CHARACTER_FRIENDLY_TYPE.PLAYER_FRIENDLY;
        }

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
            if (!IsPlayerInputAllowed || !CharacterState.IsAttackAble())
            {
                return;
            }

            bool isAttackTid = _inputType.TryGetValue(inputType, out string attackTid);

            var inputAttackData = FindAttackData(attackTid);
            if (inputAttackData == null)
            {
                return;
            }

            if (inputAttackData.AttackInputType == ATTACK_INPUT_TYPE.AI)
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

                        if (currentAttack == null || currentAttack.AttackInputType == ATTACK_INPUT_TYPE.AI)
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
                        _currentAttackTid = attackTid;
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
                        _currentAttackTid = "";
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

            StopMovementRequest();
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
            if (currentAttack == null || currentAttack.AttackInputType == ATTACK_INPUT_TYPE.AI)
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

        public AttackInfoData FindAttackInfoData(string attackInfoDataTid)
        {
            if (_attackInfoData == null || string.IsNullOrEmpty(attackInfoDataTid))
            {
                return null;
            }

            return Array.Find(_attackInfoData, data => data.Tid == attackInfoDataTid);
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

            if (_characterAIState == CHARACTER_AI_STATE.AI && currentAttack.AttackInputType == ATTACK_INPUT_TYPE.AI)
            {
                if (TryGetFirstLinkedAttackTid(currentAttack, out string linkedTid))
                {
                    currentAttack.AttackEndEvent?.Invoke();
                    Attack(linkedTid);
                    return;
                }

                currentAttack.AttackEndEvent?.Invoke();
                ClearAttackComboBufferAndCurrentTid();
                ChangeCharacterState(CHARACTER_STATE.IDLE);
                OnAIAttackAnimationEnded(currentAttack);
                return;
            }

            currentAttack.AttackEndEvent?.Invoke();
            ClearAttackComboBufferAndCurrentTid();
            ChangeCharacterState(CHARACTER_STATE.IDLE);
        }

        private bool TryGetSelectedAIAttackData(out AttackData selected)
        {
            selected = null;
            if (_attackData == null || _attackData.Length == 0)
            {
                return false;
            }

            float bestPriority = float.MinValue;
            for (int i = 0; i < _attackData.Length; i++)
            {
                AttackData data = _attackData[i];
                if (data == null || data.AttackInputType != ATTACK_INPUT_TYPE.AI)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(data.Tid))
                {
                    continue;
                }

                if (data.AttackPriority > bestPriority)
                {
                    bestPriority = data.AttackPriority;
                    selected = data;
                }
            }

            return selected != null;
        }

        private static bool TryGetFirstLinkedAttackTid(AttackData attackData, out string linkedTid)
        {
            linkedTid = null;
            if (attackData?.LinkedAttack == null || attackData.LinkedAttack.Count == 0)
            {
                return false;
            }

            foreach (System.Collections.Generic.KeyValuePair<INPUT_TYPE, string> pair in attackData.LinkedAttack)
            {
                if (!string.IsNullOrEmpty(pair.Value))
                {
                    linkedTid = pair.Value;
                    return true;
                }
            }

            return false;
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
            AttackInfoData attackInfo = FindAttackInfoData(attackInfoDataTid);
            if (attackInfo == null)
            {
                Debug.LogWarning($"{name}: AttackInfoData not found for tid '{attackInfoDataTid}'");
                return;
            }

            CombatManager combatManager = GameManager.Instance != null ? GameManager.Instance.CombatManager : null;
            if (combatManager == null)
            {
                Debug.LogWarning($"{name}: CombatManager is not available.");
                return;
            }

            combatManager.ProcessMeleeHitFromAttackInfo(this, attackInfo);
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

    public enum ATTACK_INPUT_TYPE
    {
        NONE,
        INPUT,
        AI,
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

        public ATTACK_INPUT_TYPE AttackInputType;

        //Input

        [Range(0f, 1f)]
        [Tooltip("현재 공격 애니메이션(레이어 0) 정규화 시간이 이 값 이상일 때, 버퍼에 쌓인 LinkedAttack 입력으로 다음 공격이 실행됩니다.")]
        public float NextAttackChainUnlockNormalizedTime = 0.35f;

        //AI
        public float AttackPriority;
        public float AIAttackDistance;
        //AI 끝

        public SerializedDictionary<INPUT_TYPE, string> LinkedAttack = new SerializedDictionary<INPUT_TYPE, string>();

        public Action AttackStartEvent;
        public Action AttackEndEvent;
    }
    [Serializable]
    public class AttackInfoData
    {
        public string Tid;
        public ATTACK_FRIENDLY_TYPE AttackFriendlyType = ATTACK_FRIENDLY_TYPE.ENEMY;
        public float DamageValue;
        public float CameraShakeGain = 0.3f;

        //MELEE
        public Vector3 HitBoxSize;
        public Vector3 HitBoxOffset;
        public Vector3 HitBoxRotation;
        public float HitBoxDuration;

        //PROJECTILE
        public GameObject Projectile;
    }

    public enum ATTACK_FRIENDLY_TYPE
    {
        NONE,
        FRIENDLY,
        ENEMY,
        ALL,
    }

    public enum CHARACTER_FRIENDLY_TYPE
    {
        NONE,
        PLAYER,
        PLAYER_FRIENDLY,
        ENEMY,
        NEUTRAL,
    }
}