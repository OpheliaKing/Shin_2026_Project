using System;
using UnityEngine;

namespace Shin
{
    /// <summary>
    /// 애니메이터 상태에 부착합니다. 시작/종료 시 <see cref="CharacterBase"/>에 표시 이름을 알리고,
    /// 정규화 시간 구간마다 <see cref="CharacterBase.AttackToAnimation"/>을 한 번씩 호출합니다.
    /// </summary>
    public class CharacterCombatAnimation : StateMachineBehaviour
    {
        [Tooltip("비우면 현재 상태의 메인 모션 클립 이름을 사용합니다. Animator 그래프의 상태 이름과 맞추고 싶으면 여기에 동일한 문자열을 적습니다.")]
        [SerializeField]
        private string _displayNameOverride;

        [Tooltip("정규화 시간(대개 0~1) 구간 안에 들어온 프레임에서 한 번만 AttackToAnimation이 호출됩니다.")]
        [SerializeField]
        private CombatHitPhaseWindow[] _hitWindows = Array.Empty<CombatHitPhaseWindow>();

        [Range(0f, 1f)]
        [SerializeField]
        private float _attackCancelTime;

        private CharacterBase _owner;
        private string _resolvedDisplayName;
        private bool[] _hitWindowFired;
        private bool _playStartedNotified;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _owner = animator.GetComponentInParent<CharacterBase>();
            _resolvedDisplayName = null;
            _playStartedNotified = false;
            _hitWindowFired = new bool[_hitWindows != null ? _hitWindows.Length : 0];
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_owner == null)
            {
                return;
            }

            if (!_playStartedNotified)
            {
                _resolvedDisplayName = ResolveDisplayName(animator, layerIndex, stateInfo);
                _owner.OnCombatAnimationPlayStarted(_resolvedDisplayName);
                _playStartedNotified = true;
            }

            if (_hitWindows == null || _hitWindows.Length == 0)
            {
                return;
            }

            float nt = GetNormalizedPlaybackTime(stateInfo);

            for (int i = 0; i < _hitWindows.Length; i++)
            {
                if (_hitWindowFired[i])
                {
                    continue;
                }

                ref CombatHitPhaseWindow w = ref _hitWindows[i];
                float start = Mathf.Min(w.normalizedTimeStart, w.normalizedTimeEnd);
                float end = Mathf.Max(w.normalizedTimeStart, w.normalizedTimeEnd);

                if (nt >= start && nt <= end)
                {
                    _hitWindowFired[i] = true;
                    _owner.AttackToAnimation(_resolvedDisplayName, i, nt, w.AttackInfoDataTid);
                }
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_owner != null)
            {
                if (!_playStartedNotified)
                {
                    _resolvedDisplayName = ResolveDisplayName(animator, layerIndex, stateInfo);
                    _owner.OnCombatAnimationPlayStarted(_resolvedDisplayName);
                    _playStartedNotified = true;
                }
                else if (string.IsNullOrEmpty(_resolvedDisplayName))
                {
                    _resolvedDisplayName = ResolveDisplayName(animator, layerIndex, stateInfo);
                }

                _owner.OnCombatAnimationPlayEnded(_resolvedDisplayName);
            }

            _owner = null;
            _resolvedDisplayName = null;
            _hitWindowFired = null;
            _playStartedNotified = false;
        }

        private static float GetNormalizedPlaybackTime(AnimatorStateInfo stateInfo)
        {
            float nt = stateInfo.normalizedTime;
            if (stateInfo.loop)
            {
                nt -= Mathf.Floor(nt);
            }
            else
            {
                nt = Mathf.Clamp01(nt);
            }

            return nt;
        }

        private string ResolveDisplayName(Animator animator, int layerIndex, AnimatorStateInfo stateInfo)
        {
            if (!string.IsNullOrEmpty(_displayNameOverride))
            {
                return _displayNameOverride;
            }

            var clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);
            if (clipInfos != null && clipInfos.Length > 0 && clipInfos[0].clip != null)
            {
                return clipInfos[0].clip.name;
            }

            return $"FullPathHash_{stateInfo.fullPathHash}";
        }
    }

    [Serializable]
    public struct CombatHitPhaseWindow
    {
        [Range(0f, 1f)]
        public float normalizedTimeStart;

        [Range(0f, 1f)]
        public float normalizedTimeEnd;

        public string AttackInfoDataTid;
    }
}
