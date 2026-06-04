using UnityEngine;
using UnityEngine.AI;

namespace Shin
{
    public partial class CharacterBase
    {
        [Header("AI")]
        [SerializeField]
        private CHARACTER_AI_STATE _characterAIState = CHARACTER_AI_STATE.NONE;

        [SerializeField, Min(0f)]
        private float _enemyChaseStopDistance = 1.5f;

        [SerializeField, Min(0.1f)]
        [Tooltip("NavMesh 경로 계산 시 시작/목표 위치를 메시 위로 맞출 검색 반경입니다.")]
        private float _navMeshPathSampleRadius = 4f;

        [SerializeField, Min(0.05f)]
        [Tooltip("현재 코너를 지나쳤다고 볼 거리. 작을수록 Bake 코너를 더 정확히 따릅니다.")]
        private float _pathCornerReachDistance = 0.3f;

        [SerializeField, Min(0.1f)]
        [Tooltip("경로 선분 위를 따라 바라볼 거리. 커브에서 코너를 깎지 않도록 경로 위 점을 향합니다.")]
        private float _pathLookAheadDistance = 1.2f;

        [SerializeField, Min(0.05f)]
        private float _navPathRecalculateInterval = 0.25f;

        public CHARACTER_AI_STATE CharacterAIState => _characterAIState;

        protected void EnsureDefaultPlayerAIState()
        {
            if (_characterAIState == CHARACTER_AI_STATE.NONE)
            {
                _characterAIState = CHARACTER_AI_STATE.PLAYER;
            }
        }

        private NavMeshPath _navMeshPath;
        private int _navPathCornerIndex;
        private float _nextNavPathRecalculateTime;
        private CharacterBase _cachedPlayerTarget;

        partial void InitAI()
        {
            _navMeshPath = new NavMeshPath();

            if (_characterAIState == CHARACTER_AI_STATE.AI)
            {
                EnsureAIMovementState();
            }
        }

        partial void UpdateCharacterAI()
        {
            if (_characterAIState != CHARACTER_AI_STATE.AI)
            {
                return;
            }

            switch (_friendlyType)
            {
                case CHARACTER_FRIENDLY_TYPE.ENEMY:
                    UpdateEnemyChaseAI();
                    break;
            }
        }

        private void UpdateEnemyChaseAI()
        {
            EnsureAIMovementState();

            CharacterBase player = FindPlayerTarget();
            if (player == null)
            {
                Move(Vector2.zero);
                return;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= _enemyChaseStopDistance * _enemyChaseStopDistance)
            {
                Move(Vector2.zero);
                return;
            }

            if (!TryGetNavMeshSteeringDirection(player.transform.position, out Vector3 steeringDirection))
            {
                Move(Vector2.zero);
                return;
            }

            Move(new Vector2(steeringDirection.x, steeringDirection.z));
        }

        private void EnsureAIMovementState()
        {
            if (_characterState == CHARACTER_STATE.NONE || _characterState == CHARACTER_STATE.DIE)
            {
                ChangeCharacterState(CHARACTER_STATE.IDLE, true);
            }
        }

        private CharacterBase FindPlayerTarget()
        {
            if (_cachedPlayerTarget != null && _cachedPlayerTarget.gameObject.activeInHierarchy)
            {
                return _cachedPlayerTarget;
            }

            PlayerCharacterBase playerCharacter = FindAnyObjectByType<PlayerCharacterBase>();
            if (playerCharacter != null && playerCharacter != this)
            {
                _cachedPlayerTarget = playerCharacter;
                return _cachedPlayerTarget;
            }

            CharacterBase[] characters = FindObjectsByType<CharacterBase>(FindObjectsInactive.Exclude);
            CharacterBase player = null;
            CharacterBase playerFriendly = null;

            for (int i = 0; i < characters.Length; i++)
            {
                CharacterBase candidate = characters[i];
                if (candidate == this || candidate.CharacterAIState == CHARACTER_AI_STATE.AI)
                {
                    continue;
                }

                if (candidate.FriendlyType == CHARACTER_FRIENDLY_TYPE.PLAYER)
                {
                    player = candidate;
                    break;
                }

                if (candidate.FriendlyType == CHARACTER_FRIENDLY_TYPE.PLAYER_FRIENDLY && playerFriendly == null)
                {
                    playerFriendly = candidate;
                }
            }

            _cachedPlayerTarget = player != null ? player : playerFriendly;
            return _cachedPlayerTarget;
        }

        private bool TryGetNavMeshSteeringDirection(Vector3 destination, out Vector3 direction)
        {
            direction = Vector3.zero;

            if (Time.time >= _nextNavPathRecalculateTime || !IsNavPathUsable(destination))
            {
                if (!TryCalculateNavMeshPath(destination))
                {
                    return false;
                }

                _nextNavPathRecalculateTime = Time.time + _navPathRecalculateInterval;
            }

            return TryGetDirectionAlongNavMeshPath(out direction);
        }

        private bool IsNavPathUsable(Vector3 destination)
        {
            if (_navMeshPath == null || _navMeshPath.corners == null || _navMeshPath.corners.Length < 2)
            {
                return false;
            }

            Vector3 pathEnd = _navMeshPath.corners[_navMeshPath.corners.Length - 1];
            pathEnd.y = 0f;
            Vector3 destFlat = destination;
            destFlat.y = 0f;
            return (pathEnd - destFlat).sqrMagnitude <= 1f;
        }

        private bool TryCalculateNavMeshPath(Vector3 destination)
        {
            if (_navMeshPath == null)
            {
                _navMeshPath = new NavMeshPath();
            }

            if (!TrySampleNavMeshNear(transform.position, _navMeshPathSampleRadius, out Vector3 fromOnMesh))
            {
                return false;
            }

            if (!TrySampleNavMeshNear(destination, _navMeshPathSampleRadius, out Vector3 toOnMesh))
            {
                return false;
            }

            if (!NavMesh.CalculatePath(fromOnMesh, toOnMesh, NavMesh.AllAreas, _navMeshPath))
            {
                return false;
            }

            if (_navMeshPath.status != NavMeshPathStatus.PathComplete
                && _navMeshPath.status != NavMeshPathStatus.PathPartial)
            {
                return false;
            }

            if (_navMeshPath.corners.Length < 2)
            {
                return false;
            }

            _navPathCornerIndex = 1;
            return true;
        }

        /// <summary>
        /// Bake 경로(코너 폴리라인) 위에 가장 가까운 점을 기준으로, 선분을 따라 look-ahead 지점을 향해 이동합니다.
        /// </summary>
        private bool TryGetDirectionAlongNavMeshPath(out Vector3 direction)
        {
            direction = Vector3.zero;
            Vector3[] corners = _navMeshPath.corners;
            if (corners == null || corners.Length < 2)
            {
                return false;
            }

            float pathY = corners[0].y;
            Vector3 flatPosition = transform.position;
            flatPosition.y = pathY;

            if (!TryGetClosestPointOnNavPath(flatPosition, corners, out Vector3 closestOnPath, out int segmentIndex))
            {
                return false;
            }

            AdvanceNavPathCornerIndex(flatPosition, corners, segmentIndex);

            if (!TryGetPointAheadOnNavPath(closestOnPath, segmentIndex, corners, _pathLookAheadDistance, out Vector3 lookAheadPoint))
            {
                lookAheadPoint = corners[corners.Length - 1];
                lookAheadPoint.y = pathY;
            }

            Vector3 toLookAhead = lookAheadPoint - flatPosition;
            toLookAhead.y = 0f;
            if (toLookAhead.sqrMagnitude < 1e-8f)
            {
                return false;
            }

            direction = toLookAhead.normalized;
            return true;
        }

        private void AdvanceNavPathCornerIndex(Vector3 flatPosition, Vector3[] corners, int currentSegmentIndex)
        {
            _navPathCornerIndex = Mathf.Max(_navPathCornerIndex, currentSegmentIndex + 1);

            float reachSqr = _pathCornerReachDistance * _pathCornerReachDistance;
            while (_navPathCornerIndex < corners.Length)
            {
                Vector3 corner = corners[_navPathCornerIndex];
                corner.y = flatPosition.y;
                if ((corner - flatPosition).sqrMagnitude > reachSqr)
                {
                    break;
                }

                _navPathCornerIndex++;
            }
        }

        private static bool TryGetClosestPointOnNavPath(
            Vector3 flatPosition,
            Vector3[] corners,
            out Vector3 closestOnPath,
            out int segmentIndex)
        {
            closestOnPath = flatPosition;
            segmentIndex = 0;
            float bestDistanceSqr = float.MaxValue;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 segmentStart = corners[i];
                Vector3 segmentEnd = corners[i + 1];
                segmentStart.y = flatPosition.y;
                segmentEnd.y = flatPosition.y;

                Vector3 closestOnSegment = GetClosestPointOnSegment(segmentStart, segmentEnd, flatPosition, out _);
                float distanceSqr = (closestOnSegment - flatPosition).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    closestOnPath = closestOnSegment;
                    segmentIndex = i;
                }
            }

            return bestDistanceSqr < float.MaxValue;
        }

        private static Vector3 GetClosestPointOnSegment(Vector3 segmentStart, Vector3 segmentEnd, Vector3 point, out float t)
        {
            Vector3 segment = segmentEnd - segmentStart;
            float segmentLengthSqr = segment.sqrMagnitude;
            if (segmentLengthSqr < 1e-8f)
            {
                t = 0f;
                return segmentStart;
            }

            t = Mathf.Clamp01(Vector3.Dot(point - segmentStart, segment) / segmentLengthSqr);
            return segmentStart + segment * t;
        }

        private static bool TryGetPointAheadOnNavPath(
            Vector3 startOnPath,
            int startSegmentIndex,
            Vector3[] corners,
            float lookAheadDistance,
            out Vector3 pointAhead)
        {
            pointAhead = startOnPath;
            if (lookAheadDistance <= 0f)
            {
                return true;
            }

            float pathY = startOnPath.y;
            float remaining = lookAheadDistance;

            Vector3 segmentEnd = corners[startSegmentIndex + 1];
            segmentEnd.y = pathY;
            Vector3 segmentVector = segmentEnd - startOnPath;
            float segmentLength = segmentVector.magnitude;

            if (segmentLength >= remaining)
            {
                pointAhead = segmentLength < 1e-8f
                    ? segmentEnd
                    : startOnPath + segmentVector * (remaining / segmentLength);
                return true;
            }

            remaining -= segmentLength;
            for (int i = startSegmentIndex + 1; i < corners.Length - 1; i++)
            {
                Vector3 segStart = corners[i];
                Vector3 segEnd = corners[i + 1];
                segStart.y = pathY;
                segEnd.y = pathY;
                segmentVector = segEnd - segStart;
                segmentLength = segmentVector.magnitude;

                if (segmentLength >= remaining)
                {
                    pointAhead = segmentLength < 1e-8f
                        ? segEnd
                        : segStart + segmentVector * (remaining / segmentLength);
                    return true;
                }

                remaining -= segmentLength;
            }

            pointAhead = corners[corners.Length - 1];
            pointAhead.y = pathY;
            return true;
        }

        private static bool TrySampleNavMeshNear(Vector3 worldPosition, float maxDistance, out Vector3 onMesh)
        {
            onMesh = worldPosition;
            if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            {
                return false;
            }

            onMesh = hit.position;
            return true;
        }
    }

    public enum CHARACTER_AI_STATE
    {
        NONE,
        PLAYER,
        AI,
    }
}
