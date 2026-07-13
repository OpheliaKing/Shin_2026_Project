using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shin
{
    public partial class CharacterBase
    {
        private readonly List<InputBlockEntry> _inputBlocks = new List<InputBlockEntry>();
        private int _nextInputBlockId = 1;

        /// <summary>입력 차단 요인이 하나라도 있으면 true.</summary>
        public bool HasInputBlock
        {
            get
            {
                RemoveExpiredInputBlocks();
                return _inputBlocks.Count > 0;
            }
        }

        /// <summary>현재 적용 중인 입력 차단 목록(읽기 전용 스냅샷).</summary>
        public IReadOnlyList<InputBlockEntry> InputBlocks
        {
            get
            {
                RemoveExpiredInputBlocks();
                return _inputBlocks;
            }
        }

        /// <summary>
        /// 입력 차단을 추가합니다. durationSeconds가 0보다 크면 만료 시각이 설정되고, 아니면 수동 제거까지 유지됩니다.
        /// </summary>
        /// <returns>제거에 사용할 블록 Id.</returns>
        public int AddInputBlock(INPUT_BLOCK_REASON reason, float durationSeconds = -1f)
        {
            bool wasBlocked = _inputBlocks.Count > 0;

            int id = _nextInputBlockId++;
            float expireTime = durationSeconds > 0f
                ? Time.time + durationSeconds
                : float.PositiveInfinity;

            _inputBlocks.Add(new InputBlockEntry(id, reason, expireTime));

            if (!wasBlocked)
            {
                OnInputBlocked();
            }

            return id;
        }

        public bool RemoveInputBlock(int blockId)
        {
            for (int i = 0; i < _inputBlocks.Count; i++)
            {
                if (_inputBlocks[i].Id != blockId)
                {
                    continue;
                }

                INPUT_BLOCK_REASON reason = _inputBlocks[i].Reason;
                _inputBlocks.RemoveAt(i);
                if (reason == INPUT_BLOCK_REASON.HitStun)
                {
                    TryExitHitStateAfterHitStun();
                }

                return true;
            }

            return false;
        }

        public int RemoveInputBlocksByReason(INPUT_BLOCK_REASON reason)
        {
            return _inputBlocks.RemoveAll(entry => entry.Reason == reason);
        }

        public void ClearInputBlocks()
        {
            _inputBlocks.Clear();
        }

        public bool HasInputBlockReason(INPUT_BLOCK_REASON reason)
        {
            RemoveExpiredInputBlocks();
            for (int i = 0; i < _inputBlocks.Count; i++)
            {
                if (_inputBlocks[i].Reason == reason)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveExpiredInputBlocks()
        {
            float now = Time.time;
            bool removedHitStun = false;

            for (int i = _inputBlocks.Count - 1; i >= 0; i--)
            {
                if (_inputBlocks[i].ExpireTime > now)
                {
                    continue;
                }

                if (_inputBlocks[i].Reason == INPUT_BLOCK_REASON.HitStun)
                {
                    removedHitStun = true;
                }

                _inputBlocks.RemoveAt(i);
            }

            if (removedHitStun)
            {
                TryExitHitStateAfterHitStun();
            }
        }

        private void TryExitHitStateAfterHitStun()
        {
            if (CharacterState != CHARACTER_STATE.HIT)
            {
                return;
            }

            for (int i = 0; i < _inputBlocks.Count; i++)
            {
                if (_inputBlocks[i].Reason == INPUT_BLOCK_REASON.HitStun)
                {
                    return;
                }
            }

            ChangeCharacterState(CHARACTER_STATE.IDLE);
        }

        /// <summary>입력이 막히기 시작한 프레임에 호출됩니다. 플레이어는 잔여 입력을 클리어합니다.</summary>
        protected virtual void OnInputBlocked()
        {
        }
    }

    public enum INPUT_BLOCK_REASON
    {
        HitStun,
        Stun,
        Debuff,
    }

    [Serializable]
    public readonly struct InputBlockEntry
    {
        public readonly int Id;
        public readonly INPUT_BLOCK_REASON Reason;
        public readonly float ExpireTime;

        public InputBlockEntry(int id, INPUT_BLOCK_REASON reason, float expireTime)
        {
            Id = id;
            Reason = reason;
            ExpireTime = expireTime;
        }

        public bool IsTimed => !float.IsPositiveInfinity(ExpireTime);
    }
}
