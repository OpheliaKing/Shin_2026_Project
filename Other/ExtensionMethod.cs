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
                    return false;
                default:
                    return true;
            }
        }
    }
}
