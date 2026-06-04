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
        private float _navMeshSampleRadius = 4f;

        [SerializeField, Min(0.05f)]
        private float _pathCornerReachDistance = 0.4f;

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
                SnapPositionToNavMesh();
            }
        }

        /// <summary>AI 이동량을 NavMesh 위로만 허용합니다. 벽 슬라이드로 메시 밖으로 나가는 것을 막습니다.</summary>
        internal Vector3 ResolveAIDelta(Vector3 delta)
        {
            if (delta.sqrMagnitude < 1e-8f || _rigidbody == null)
            {
                return delta;
            }

            Vector3 from = _rigidbody.position;
            Vector3 target = from + delta;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas))
            {
                Vector3 onMesh = hit.position;
                onMesh.y = from.y;
                return onMesh - from;
            }

            return Vector3.zero;
        }

        partial void ApplyAINavMeshPositionConstraint()
        {
            if (_characterAIState != CHARACTER_AI_STATE.AI)
            {
                return;
            }

            SnapPositionToNavMesh();
        }

        private void SnapPositionToNavMesh()
        {
            if (_rigidbody == null)
            {
                return;
            }

            Vector3 position = _rigidbody.position;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas))
            {
                Vector3 onMesh = hit.position;
                onMesh.y = position.y;
                _rigidbody.MovePosition(onMesh);
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

            return TryGetDirectionToNextPathCorner(out direction);
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

            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit fromHit, _navMeshSampleRadius, NavMesh.AllAreas))
            {
                return false;
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit toHit, _navMeshSampleRadius, NavMesh.AllAreas))
            {
                return false;
            }

            if (!NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, _navMeshPath))
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

        private bool TryGetDirectionToNextPathCorner(out Vector3 direction)
        {
            direction = Vector3.zero;

            Vector3 currentPosition = transform.position;
            if (NavMesh.SamplePosition(currentPosition, out NavMeshHit currentHit, _navMeshSampleRadius, NavMesh.AllAreas))
            {
                currentPosition = currentHit.position;
            }

            while (_navPathCornerIndex < _navMeshPath.corners.Length)
            {
                Vector3 corner = _navMeshPath.corners[_navPathCornerIndex];
                Vector3 toCorner = corner - currentPosition;
                toCorner.y = 0f;

                if (toCorner.sqrMagnitude <= _pathCornerReachDistance * _pathCornerReachDistance)
                {
                    _navPathCornerIndex++;
                    continue;
                }

                direction = toCorner.normalized;
                return true;
            }

            return false;
        }
    }

    public enum CHARACTER_AI_STATE
    {
        NONE,
        PLAYER,
        AI,
    }
}
