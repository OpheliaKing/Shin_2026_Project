using UnityEngine;

namespace Shin
{
    public static class ExtensionMethod
    {
        public static bool IsMoveAble(this CHARACTER_STATE state)
        {
            switch (state)
            {
                case CHARACTER_STATE.NONE:
                case CHARACTER_STATE.DIE:
                case CHARACTER_STATE.HIT:
                case CHARACTER_STATE.ATTACK:
                case CHARACTER_STATE.ATTACK_MOVEABLE:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsAttackAble(this CHARACTER_STATE state)
        {
            switch (state)
            {
                case CHARACTER_STATE.NONE:
                case CHARACTER_STATE.DIE:
                case CHARACTER_STATE.HIT:
                case CHARACTER_STATE.MOVE:
                case CHARACTER_STATE.MOVE_RUN:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsPlayerInputAllowed(this CHARACTER_STATE state)
        {
            return state != CHARACTER_STATE.DIE;
        }
        
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }
    }
}
