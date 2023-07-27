using UnityEngine;
using UnityEngine.AI;

public class StaticEntity : MonoBehaviour, IEntity
{
    public Vector3 Position
    {
        get => _navigationType == NavigationType.Default ? _rigidbody.position : _transform.position;
        private set
        { 
            if (_navigationType == NavigationType.Default)
            {
                _rigidbody.position = value;
            }
            else
            {
                _transform.position = value;
            } 
        }
    }
    public float PersonalSpace => 1f;

    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private NavMeshObstacle _navMeshObstacle;
    [SerializeField] private Rigidbody _rigidbody;

    private Transform _transform;
    private NavigationType _navigationType;
    private float _speed;
    private float _maxDistanceFromOrigin;

    public void Initialize(EntityArgs args)
    {
        _transform = transform;
        _rigidbody.position = args.StartingPosition;
        _meshRenderer.sharedMaterial = args.Material;
        _speed = args.Speed;
        _maxDistanceFromOrigin = args.MaxDistanceFromOrgin;
        _navigationType = args.NavigationType;
        if (_navigationType != NavigationType.NavMesh)
        {
            Destroy(_navMeshObstacle);
        }
    }

    public void UpdatePosition(float deltaTime)
    {
        // no-op
    }

    public void SetPaused(bool isPaused)
    {
        // no-op
    }

    public void Select()
    {
        // no-op
    }

    public void Deselect()
    {
        // no-op
    }

    public void DragTowards(float deltaTime, Vector3 position)
    {
        position = Vector3.ClampMagnitude(position, _maxDistanceFromOrigin);
        var newPosition = Vector3.MoveTowards(Position, position, deltaTime * _speed);

        Position = newPosition;
    }

    public void StopDragging()
    {
        // no-op
    }
}
