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


        public void AttackInput(INPUT_TYPE inputType)
        {
            if (!CharacterState.IsAttackAble())
            {
                return;
            }

            if (_currentAttackTid.IsNullOrEmpty())
            {
                if (_inputType.TryGetValue(inputType, out string attackTid))
                {
                    Attack(attackTid);
                }
            }
            else
            {
                AttackData attackData = Array.Find(_attackData, data => data.Tid == _currentAttackTid);

                attackData.LinkedAttack.TryGetValue(inputType, out string linkedAttackTid);
                if (!linkedAttackTid.IsNullOrEmpty())
                {
                    Attack(linkedAttackTid);
                }
            }


        }

        public void Attack(string attackTid)
        {
            if (string.IsNullOrEmpty(attackTid))
            {
                return;
            }

            AttackData attackData = Array.Find(_attackData, data => data.Tid == attackTid);
            if (attackData == null)
            {
                return;
            }

            ChangeCharacterState(CHARACTER_STATE.ATTACK);

            attackData.AttackStartEvent?.Invoke();
            _animator.Play(attackData.AnimationName);
            _currentAttackTid = attackTid;
        }

        /// <summary>공격 상태 애니메이션이 재생되기 시작했을 때(StateMachineBehaviour OnStateEnter).</summary>
        public virtual void OnCombatAnimationPlayStarted(string animatorStateDisplayName)
        {
            _currentPlayAttackAnim = animatorStateDisplayName;
        }

        /// <summary>공격 상태 애니메이션이 끝났을 때(StateMachineBehaviour OnStateExit).</summary>
        public virtual void OnCombatAnimationPlayEnded(string animatorStateDisplayName)
        {
            if (_currentPlayAttackAnim == animatorStateDisplayName)
            {
                _currentPlayAttackAnim = null;
            }
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