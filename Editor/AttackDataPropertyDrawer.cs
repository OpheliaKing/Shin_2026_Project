using UnityEditor;
using UnityEngine;

namespace Shin.Editor
{
    [CustomPropertyDrawer(typeof(AttackData))]
    public class AttackDataPropertyDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                float y = position.y + EditorGUIUtility.singleLineHeight + VerticalSpacing;
                float x = position.x;
                float width = position.width;

                y = DrawRelativeField(property, "Tid", x, y, width);
                y = DrawRelativeField(property, "AttackType", x, y, width);
                y = DrawRelativeField(property, "AnimationName", x, y, width);
                y = DrawRelativeField(property, "AttackInputType", x, y, width);

                ATTACK_INPUT_TYPE inputType = GetAttackInputType(property);
                if (inputType == ATTACK_INPUT_TYPE.INPUT || inputType == ATTACK_INPUT_TYPE.NONE)
                {
                    y = DrawRelativeField(property, "NextAttackChainUnlockNormalizedTime", x, y, width);
                }
                else if (inputType == ATTACK_INPUT_TYPE.AI)
                {
                    y = DrawRelativeField(property, "AttackPriority", x, y, width);
                    y = DrawRelativeField(property, "AIAttackDistance", x, y, width);
                }

                DrawRelativeField(property, "LinkedAttack", x, y, width);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            height += SumFieldHeight(property, "Tid");
            height += SumFieldHeight(property, "AttackType");
            height += SumFieldHeight(property, "AnimationName");
            height += SumFieldHeight(property, "AttackInputType");

            ATTACK_INPUT_TYPE inputType = GetAttackInputType(property);
            if (inputType == ATTACK_INPUT_TYPE.INPUT || inputType == ATTACK_INPUT_TYPE.NONE)
            {
                height += SumFieldHeight(property, "NextAttackChainUnlockNormalizedTime");
            }
            else if (inputType == ATTACK_INPUT_TYPE.AI)
            {
                height += SumFieldHeight(property, "AttackPriority");
                height += SumFieldHeight(property, "AIAttackDistance");
            }

            height += SumFieldHeight(property, "LinkedAttack");
            return height;
        }

        private static float DrawRelativeField(
            SerializedProperty parent,
            string relativeName,
            float x,
            float y,
            float width)
        {
            SerializedProperty child = parent.FindPropertyRelative(relativeName);
            if (child == null)
            {
                return y;
            }

            float height = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(new Rect(x, y, width, height), child, true);
            return y + height + VerticalSpacing;
        }

        private static float SumFieldHeight(SerializedProperty parent, string relativeName)
        {
            SerializedProperty child = parent.FindPropertyRelative(relativeName);
            if (child == null)
            {
                return 0f;
            }

            return EditorGUI.GetPropertyHeight(child, true) + VerticalSpacing;
        }

        private static ATTACK_INPUT_TYPE GetAttackInputType(SerializedProperty property)
        {
            SerializedProperty inputTypeProperty = property.FindPropertyRelative("AttackInputType");
            if (inputTypeProperty == null)
            {
                return ATTACK_INPUT_TYPE.NONE;
            }

            return (ATTACK_INPUT_TYPE)inputTypeProperty.enumValueIndex;
        }
    }
}
