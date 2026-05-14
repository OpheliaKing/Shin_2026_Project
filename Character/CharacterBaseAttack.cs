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

    [System.Serializable]
    public class AttackData
    {
        public string Tid;
        public ATTACK_TYPE AttackType;
        public string AnimationName;

        //MELEE
        public Vector3 HitBoxSize;
        public Vector3 HitBoxOffset;
        public Vector3 HitBoxRotation;
        public float HitBoxDuration;

        //PROJECTILE
        public GameObject Projectile;

        public SerializedDictionary<INPUT_TYPE, string> LinkedAttack = new SerializedDictionary<INPUT_TYPE, string>();
        
        public Action AttackStartEvent;
        public Action AttackEndEvent;
    }
}