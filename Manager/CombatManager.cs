using System.Collections.Generic;
using UnityEngine;

namespace Shin
{
    public class CombatManager : ManagerBase
    {
        /// <summary>
        /// 데미지를 실제로 적용하는 유일한 진입점입니다. 범위·히트스캔·투사체 등 모든 경로는 최종적으로 이 함수만 호출해야 합니다.
        /// </summary>
        /// <param name="attacker">때린 유닛</param>
        /// <param name="victim">맞은 유닛</param>
        /// <param name="attackInfo">때린 유닛 기준의 전투 데이터</param>
        public void ApplyDamage(CharacterBase attacker, CharacterBase victim, AttackInfoData attackInfo)
        {
            if (!CanApplyDamage(attacker, victim, attackInfo))
            {
                return;
            }

            int damage = Mathf.Max(0, Mathf.RoundToInt(attackInfo.DamageValue));

            Debug.Log("DamageValue: " + attackInfo.DamageValue);
            if (damage <= 0)
            {
                return;
            }

            victim.ReceiveCombatDamage(attacker, attackInfo, damage);

            Debug.Log(
                $"[CombatManager.ApplyDamage] {attacker.name} -> {victim.name} | info={attackInfo.Tid} | damage={damage} | victimHp={victim.Health}");
        }

        /// <summary>
        /// 애니메이션 히트 윈도우 등에서 호출합니다. <see cref="AttackInfoData"/>의 근접 히트박스로 대상을 찾은 뒤 <see cref="ApplyDamage"/>를 호출합니다.
        /// </summary>
        public void ProcessMeleeHitFromAttackInfo(CharacterBase attacker, AttackInfoData attackInfo)
        {
            if (attacker == null || attackInfo == null)
            {
                return;
            }

            Vector3 center = attacker.transform.TransformPoint(attackInfo.HitBoxOffset);
            Quaternion rotation = attacker.transform.rotation * Quaternion.Euler(attackInfo.HitBoxRotation);
            Vector3 halfExtents = attackInfo.HitBoxSize * 0.5f;

            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            var damagedVictims = new HashSet<CharacterBase>();

            for (int i = 0; i < hits.Length; i++)
            {
                CharacterBase victim = hits[i].GetComponentInParent<CharacterBase>();
                if (victim == null || victim == attacker || !damagedVictims.Add(victim))
                {
                    continue;
                }
                Debug.Log("TakeDamage");
                ApplyDamage(attacker, victim, attackInfo);
            }
        }

        private static bool CanApplyDamage(CharacterBase attacker, CharacterBase victim, AttackInfoData attackInfo)
        {
            if (attacker == null || victim == null || attackInfo == null)
            {
                return false;
            }

            if (victim == attacker)
            {
                return false;
            }

            if (victim.CharacterState == CHARACTER_STATE.DIE)
            {
                return false;
            }

            if (!victim.CanBeDamagedBy(attacker, attackInfo.AttackFriendlyType))
            {
                return false;
            }

            return true;
        }
    }
}
