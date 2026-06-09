using Shin;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Shin.Editor
{
    /// <summary>
    /// Hierarchy의 Map2/Floors/NavMeshSurface와 Enemy AI 값을 한 번에 맞춥니다.
    /// 씬이 디스크에 없거나 이름이 다르면 메뉴 실행 시 로그로 알려줍니다.
    /// </summary>
    public static class ShinNavMeshEnemySetup
    {
        private const float AgentRadius = 0.62f;
        private const float PathSampleRadius = 4f;
        private const float PathCornerReach = 0.3f;
        private const float PathLookAhead = 1.2f;
        private const float ChaseStopDistance = 1.5f;

        [MenuItem("Tools/Shin/Fix Enemy AI & NavMesh (Map2)")]
        public static void ApplyFromMenu()
        {
            if (!ApplyCoreSettings(logDetails: true))
            {
                Debug.LogWarning(
                    "[Shin] Map2/Floors 또는 Enemy를 찾지 못했습니다. Hierarchy 이름을 확인하거나 씬을 저장한 뒤 다시 실행하세요.");
            }
        }

        public static bool ApplyCoreSettings(bool logDetails)
        {
            bool changed = false;
            changed |= ApplyProjectNavMeshAgentRadius(AgentRadius);
            changed |= TryConfigureFloorsSurface(logDetails);
            changed |= TryConfigureEnemy(logDetails);
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            return changed;
        }

        private static bool ApplyProjectNavMeshAgentRadius(float radius)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset");
            if (assets == null || assets.Length == 0)
            {
                return false;
            }

            SerializedObject projectSettings = new SerializedObject(assets[0]);
            SerializedProperty settingsArray = projectSettings.FindProperty("m_Settings");
            if (settingsArray == null || !settingsArray.isArray || settingsArray.arraySize == 0)
            {
                return false;
            }

            SerializedProperty agentRadius = settingsArray.GetArrayElementAtIndex(0).FindPropertyRelative("agentRadius");
            if (agentRadius == null)
            {
                return false;
            }

            if (Mathf.Approximately(agentRadius.floatValue, radius))
            {
                return false;
            }

            agentRadius.floatValue = radius;
            projectSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Shin] Project NavMesh Agent Radius → {radius}");
            return true;
        }

        private static bool TryConfigureFloorsSurface(bool logDetails)
        {
            Transform floors = FindTransformByNames("Map2", "Floors") ?? FindTransformByName("Floors");
            if (floors == null)
            {
                if (logDetails)
                {
                    Debug.LogWarning("[Shin] Floors 오브젝트를 찾지 못했습니다.");
                }

                return false;
            }

            NavMeshSurface surface = floors.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = floors.gameObject.AddComponent<NavMeshSurface>();
            }

            bool changed = false;
            if (surface.collectObjects != CollectObjects.Children)
            {
                surface.collectObjects = CollectObjects.Children;
                changed = true;
            }

            if (surface.useGeometry != NavMeshCollectGeometry.RenderMeshes)
            {
                surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                changed = true;
            }

            int wallLayer = LayerMask.NameToLayer("Wall");
            LayerMask bakeMask = wallLayer >= 0
                ? ~((1 << wallLayer) | (1 << LayerMask.NameToLayer("Unit")))
                : surface.layerMask;

            if (surface.layerMask.value != bakeMask.value)
            {
                surface.layerMask = bakeMask;
                changed = true;
            }

            EditorUtility.SetDirty(surface);
            surface.BuildNavMesh();
            Debug.Log($"[Shin] NavMeshSurface Bake 완료 ({GetPath(floors)}) — Agent Radius {AgentRadius} (Project Settings)");
            return true;
        }

        private static bool TryConfigureEnemy(bool logDetails)
        {
            GameObject enemyGo = GameObject.Find("Enemy");
            CharacterBase enemy = enemyGo != null ? enemyGo.GetComponent<CharacterBase>() : null;
            if (enemy == null)
            {
                enemy = Object.FindFirstObjectByType<CharacterBase>(FindObjectsInactive.Exclude);
                if (enemy != null && enemy.CharacterAIState != CHARACTER_AI_STATE.AI)
                {
                    enemy = null;
                }
            }

            if (enemy == null)
            {
                if (logDetails)
                {
                    Debug.LogWarning("[Shin] Enemy 오브젝트(CharacterBase, AI)를 찾지 못했습니다.");
                }

                return false;
            }

            SerializedObject so = new SerializedObject(enemy);
            bool changed = SetEnum(so, "_characterAIState", (int)CHARACTER_AI_STATE.AI);
            changed |= SetEnum(so, "_friendlyType", (int)CHARACTER_FRIENDLY_TYPE.ENEMY);
            changed |= SetFloat(so, "_navMeshPathSampleRadius", PathSampleRadius);
            changed |= SetFloat(so, "_pathCornerReachDistance", PathCornerReach);
            changed |= SetFloat(so, "_pathLookAheadDistance", PathLookAhead);
            changed |= SetFloat(so, "_enemyChaseStopDistance", ChaseStopDistance);

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
            }

            CapsuleCollider capsule = enemy.GetComponent<CapsuleCollider>();
            float capsuleRadius = capsule != null ? capsule.radius : 0.5f;
            Debug.Log(
                $"[Shin] Enemy 설정 ({enemy.name}): AI/ENEMY, PathSample={PathSampleRadius}, Corner={PathCornerReach}, CapsuleR={capsuleRadius:F2}");
            return true;
        }

        private static bool SetEnum(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null || prop.enumValueIndex == value)
            {
                return false;
            }

            prop.enumValueIndex = value;
            return true;
        }

        private static bool SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null || Mathf.Approximately(prop.floatValue, value))
            {
                return false;
            }

            prop.floatValue = value;
            return true;
        }

        private static bool SetLayerMask(SerializedObject so, string propertyName, int mask)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null || prop.intValue == mask)
            {
                return false;
            }

            prop.intValue = mask;
            return true;
        }

        private static Transform FindTransformByName(string name)
        {
            GameObject go = GameObject.Find(name);
            return go != null ? go.transform : null;
        }

        private static Transform FindTransformByNames(string rootName, string childName)
        {
            Transform root = FindTransformByName(rootName);
            if (root == null)
            {
                return null;
            }

            Transform child = root.Find(childName);
            return child != null ? child : FindChildRecursive(root, childName);
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static string GetPath(Transform t)
        {
            if (t.parent == null)
            {
                return t.name;
            }

            return GetPath(t.parent) + "/" + t.name;
        }
    }
}
