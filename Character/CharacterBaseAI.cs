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

        [SerializeField, Min(0f)]
        [Tooltip("공격 사거리 밖으로 이 거리만큼 더 벌어져야 다시 추적을 시작합니다. 경계에서 멈칫거림을 줄입니다.")]
        private float _aiAttackDistanceHysteresis = 0.35f;

        [SerializeField, Min(1f)]
        [Tooltip("NavMesh 조향 방향을 부드럽게 바꿉니다. 높을수록 코너에서 덜 튑니다.")]
        private float _aiSteeringSmoothSpeed = 10f;

        [SerializeField, Min(0.05f)]
        [Tooltip("경로 조향이 잠깐 실패해도 이 시간 동안은 마지막 방향으로 이동합니다.")]
        private float _aiSteeringFailGraceTime = 0.2f;

        private bool _aiWithinAttackRange;
        private Vector3 _smoothedSteeringDirection;
        private Vector3 _lastValidSteeringDirection;
        private float _lastValidSteeringTime;

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

            if (IsInComboBufferableState(CharacterState))
            {
                StopAIMovement();
                if (TryGetAlivePlayerTarget(out CharacterBase attackTarget))
                {
                    FaceTarget(attackTarget.transform.position);
                }

                return;
            }

            if (!TryGetAlivePlayerTarget(out CharacterBase player))
            {
                _aiWithinAttackRange = false;
                StopAIMovement();
                return;
            }

            if (CharacterState == CHARACTER_STATE.HIT || CharacterState == CHARACTER_STATE.DIE)
            {
                StopAIMovement();
                return;
            }

            if (!TryGetSelectedAIAttackData(out AttackData aiAttack))
            {
                UpdateEnemyChaseMovementFallback(player);
                return;
            }

            UpdateAIAttackRangeState(player, aiAttack);

            if (TryStartAIAttack(player, aiAttack))
            {
                return;
            }

            if (!_aiWithinAttackRange)
            {
                if (!TryResolveChaseSteeringDirection(player.transform.position, out Vector3 steeringDirection))
                {
                    StopAIMovement();
                    return;
                }

                ApplyChaseMove(steeringDirection);
                return;
            }

            StopAIMovement();
            FaceTarget(player.transform.position);
        }

        partial void OnAIAttackAnimationEnded(AttackData endedAttack)
        {
            ReevaluateAIAttackRangeAfterAttack(endedAttack);
        }

        private void StopAIMovement()
        {
            StopMovementRequest();
            _smoothedSteeringDirection = Vector3.zero;
            Move(Vector2.zero);
        }

        private void FaceTarget(Vector3 targetWorldPosition)
        {
            Vector3 toTarget = targetWorldPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 1e-8f)
            {
                SetIntendedLookDirection(toTarget.normalized);
            }
        }

        private void UpdateAIAttackRangeState(CharacterBase player, AttackData aiAttack)
        {
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distanceToPlayer = toPlayer.magnitude;
            float attackDistance = aiAttack.AIAttackDistance;
            float resumeChaseDistance = attackDistance + _aiAttackDistanceHysteresis;

            if (_aiWithinAttackRange)
            {
                if (distanceToPlayer > resumeChaseDistance)
                {
                    _aiWithinAttackRange = false;
                }
            }
            else if (distanceToPlayer <= attackDistance)
            {
                _aiWithinAttackRange = true;
            }
        }

        private void ReevaluateAIAttackRangeAfterAttack(AttackData endedAttack)
        {
            StopAIMovement();

            if (!TryGetAlivePlayerTarget(out CharacterBase player) || endedAttack == null)
            {
                _aiWithinAttackRange = false;
                return;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distanceToPlayer = toPlayer.magnitude;
            _aiWithinAttackRange = distanceToPlayer <= endedAttack.AIAttackDistance;
        }

        private bool TryStartAIAttack(CharacterBase player, AttackData aiAttack)
        {
            if (!_aiWithinAttackRange)
            {
                return false;
            }

            if (!CanStartAIAttack(player, aiAttack))
            {
                return false;
            }

            StopAIMovement();
            FaceTarget(player.transform.position);
            Attack(aiAttack.Tid);
            return true;
        }

        private bool CanStartAIAttack(CharacterBase player, AttackData aiAttack)
        {
            if (!IsValidAITarget(player) || aiAttack == null)
            {
                return false;
            }

            if (!CharacterState.IsAttackAble())
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_currentAttackTid))
            {
                return false;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            return toPlayer.magnitude <= aiAttack.AIAttackDistance;
        }

        private void UpdateEnemyChaseMovementFallback(CharacterBase player)
        {
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= _enemyChaseStopDistance * _enemyChaseStopDistance)
            {
                Move(Vector2.zero);
                return;
            }

            if (!TryResolveChaseSteeringDirection(player.transform.position, out Vector3 steeringDirection))
            {
                Move(Vector2.zero);
                return;
            }

            ApplyChaseMove(steeringDirection);
        }

        private bool TryResolveChaseSteeringDirection(Vector3 destination, out Vector3 steeringDirection)
        {
            if (TryGetNavMeshSteeringDirection(destination, out steeringDirection))
            {
                _lastValidSteeringDirection = steeringDirection;
                _lastValidSteeringTime = Time.time;
                return true;
            }

            if (_lastValidSteeringDirection.sqrMagnitude >= 1e-8f
                && Time.time - _lastValidSteeringTime <= _aiSteeringFailGraceTime)
            {
                steeringDirection = _lastValidSteeringDirection;
                return true;
            }

            steeringDirection = Vector3.zero;
            return false;
        }

        private void ApplyChaseMove(Vector3 steeringDirection)
        {
            steeringDirection.y = 0f;
            if (steeringDirection.sqrMagnitude < 1e-8f)
            {
                Move(Vector2.zero);
                return;
            }

            steeringDirection.Normalize();

            if (_smoothedSteeringDirection.sqrMagnitude < 1e-8f)
            {
                _smoothedSteeringDirection = steeringDirection;
            }
            else
            {
                _smoothedSteeringDirection = Vector3.Slerp(
                    _smoothedSteeringDirection,
                    steeringDirection,
                    Time.deltaTime * _aiSteeringSmoothSpeed).normalized;
            }

            Move(new Vector2(_smoothedSteeringDirection.x, _smoothedSteeringDirection.z));
        }

        private void EnsureAIMovementState()
        {
            if (_characterState == CHARACTER_STATE.NONE || _characterState == CHARACTER_STATE.DIE)
            {
                ChangeCharacterState(CHARACTER_STATE.IDLE, true);
            }
        }

        private bool TryGetAlivePlayerTarget(out CharacterBase player)
        {
            player = FindPlayerTarget();
            if (!IsValidAITarget(player))
            {
                player = null;
                return false;
            }

            return true;
        }

        private static bool IsValidAITarget(CharacterBase target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            return target.IsCombatAlive();
        }

        private CharacterBase FindPlayerTarget()
        {
            if (IsValidAITarget(_cachedPlayerTarget))
            {
                return _cachedPlayerTarget;
            }

            _cachedPlayerTarget = null;

            PlayerCharacterBase playerCharacter = FindAnyObjectByType<PlayerCharacterBase>();
            if (IsValidAITarget(playerCharacter) && playerCharacter != this)
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

                if (!IsValidAITarget(candidate))
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
                Vector3 segmentDirection = corners[segmentIndex + 1] - corners[segmentIndex];
                segmentDirection.y = 0f;
                if (segmentDirection.sqrMagnitude < 1e-8f)
                {
                    return false;
                }

                direction = segmentDirection.normalized;
                return true;
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
