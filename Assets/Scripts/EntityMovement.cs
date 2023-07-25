using UnityEngine;
using UnityEngine.AI;

public class EntityMovement : MonoBehaviour
{
    private interface IMovement
    {
        Vector3 Position { get; }
        void UpdatePosition(float deltaTime, Vector3 desiredPosition);
        void SetPaused(bool isPaused);
        void DragTowards(float deltaTime, Vector3 position);
        void StopDragging();
    }

    private class DefaultMovement : IMovement
    {
        public Vector3 Position => _rigidbody.position;

        private readonly Rigidbody _rigidbody;
        private readonly float _speed;

        public DefaultMovement(Rigidbody rigidbody, NavMeshAgent navMeshAgent, EntityArgs args)
        {
            _rigidbody = rigidbody;
            _speed = args.Speed;

            Destroy(navMeshAgent);
            _rigidbody.position = args.StartingPosition;
        }

        public void UpdatePosition(float deltaTime, Vector3 desiredPosition)
        {
            var newPosition = Vector3.MoveTowards(_rigidbody.position, desiredPosition, deltaTime * _speed);
            _rigidbody.position = newPosition;
        }

        public void SetPaused(bool isPaused)
        {
            // no-op
        }

        public void DragTowards(float deltaTime, Vector3 position)
        {
            var newPosition = Vector3.MoveTowards(_rigidbody.position, position, deltaTime * _speed);
            _rigidbody.position = newPosition;
        }

        public void StopDragging()
        {
            // no-op
        }
    }

    private class NavMeshMovement : IMovement
    {
        public Vector3 Position => _transform.position;

        private readonly Transform _transform;
        private readonly NavMeshAgent _navMeshAgent;
        private readonly float _speed;

        private bool _isPaused;

        public NavMeshMovement(Transform transform, NavMeshAgent navMeshAgent, EntityArgs args)
        {
            _transform = transform;
            _navMeshAgent = navMeshAgent;
            _speed = args.Speed;

            _navMeshAgent.speed = _speed;
            _transform.position = args.StartingPosition;
        }

        public void UpdatePosition(float deltaTime, Vector3 desiredPosition)
        {
            _navMeshAgent.destination = desiredPosition;
        }

        public void SetPaused(bool isPaused)
        {
            _isPaused = isPaused;
            _navMeshAgent.isStopped = isPaused;
        }

        public void DragTowards(float deltaTime, Vector3 position)
        {
            _navMeshAgent.isStopped = true;
            var newPosition = Vector3.MoveTowards(_transform.position, position, deltaTime * _speed);
            _transform.position = newPosition;
        }

        public void StopDragging()
        {
            if (!_navMeshAgent.isActiveAndEnabled)
            {
                return;
            }

            _navMeshAgent.isStopped = _isPaused;
        }
    }

    public Vector3 Position => _movement.Position;

    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private Rigidbody _rigidbody;

    private IMovement _movement;
    private float _maxDistanceFromOrigin;

    private bool _isBeingDragged;

    public void Initialize(EntityArgs args)
    {
        _maxDistanceFromOrigin = args.MaxDistanceFromOrgin;
        if (args.NavigationType == NavigationType.NavMesh)
        {
            _movement = new NavMeshMovement(transform, _navMeshAgent, args);
        }
        else
        {
            _movement = new DefaultMovement(_rigidbody, _navMeshAgent, args);
        }
    }

    public void UpdatePosition(float deltaTime, Vector3 desiredPosition)
    {
        if (_isBeingDragged)
        {
            return;
        }

        _movement.UpdatePosition(deltaTime, desiredPosition);
    }

    public void SetPaused(bool isPaused)
    {
        _movement.SetPaused(isPaused);
    }

    public void DragTowards(float deltaTime, Vector3 position)
    {
        _isBeingDragged = true;

        position = Vector3.ClampMagnitude(position, _maxDistanceFromOrigin);
        _movement.DragTowards(deltaTime, position);
    }

    public void StopDragging()
    {
        _isBeingDragged = false;
        _movement.StopDragging();
    }
}
