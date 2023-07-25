using UnityEngine;

public class TriangleEntity : MonoBehaviour, IEntity
{
    public Vector3 Position => _movement.Position;
    public float PersonalSpace => 1f;

    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private EntityMovement _movement;
    [SerializeField] private Rigidbody _rigidbody;

    private float _maxDistanceFromOrigin;
    private IEntity _corner1;
    private IEntity _corner2;
    private LineController _lineController;

    public void Initialize(EntityArgs args)
    {
        _movement.Initialize(args);
        _maxDistanceFromOrigin = args.MaxDistanceFromOrgin;
        _meshRenderer.sharedMaterial = args.Material;
        var (corner1, corner2) = Utils.GetTwoDifferentIndices(args.Entities.Count, args.MyIndex);
        _corner1 = args.Entities[corner1].entity;
        _corner2 = args.Entities[corner2].entity;
        _lineController = args.LineController;
    }

    public void UpdatePosition(float deltaTime)
    {
        var calculations = Calculate();
        _movement.UpdatePosition(deltaTime, calculations.DesiredPosition);
    }

    private readonly struct Calculations
    {
        public readonly Vector3 DesiredPosition;
        public readonly Vector3 AlternatePosition;

        public Calculations(Vector3 desiredPosition, Vector3 alternatePosition)
        {
            DesiredPosition = desiredPosition;
            AlternatePosition = alternatePosition;
        }
    }

    private Calculations Calculate()
    {
        var myPosition = Position;
        var corner1Position = _corner1.Position;
        var corner2Position = _corner2.Position;

        // This is calculating the intersection of two circles centered on the two corners entities
        // with a radius of the distance between those entities.
        // The two places where the circles intersect are the two points that form an 
        // equilateral triangle.
        // Math based on this source: https://planetcalc.com/8098/

        var distance = Vector3.Distance(corner1Position, corner2Position);
        if (Mathf.Approximately(0f, distance))
        {
            return new Calculations(corner1Position, corner1Position);
        }
        var midpoint = (corner1Position + corner2Position) / 2;
        var height = Mathf.Sqrt(.75f * (distance * distance));
        var x1 = midpoint.x + (height / distance) * (corner1Position.y - corner2Position.y);
        var x2 = midpoint.x - (height / distance) * (corner1Position.y - corner2Position.y);

        var y1 = midpoint.y - (height / distance) * (corner1Position.x - corner2Position.x);
        var y2 = midpoint.y + (height / distance) * (corner1Position.x - corner2Position.x);

        var position1 = new Vector3(x1, y1);
        var position2 = new Vector3(x2, y2);

        var position1OutOfBounds = Vector3.Magnitude(position1) > _maxDistanceFromOrigin;
        var position2OutOfBounds = Vector3.Magnitude(position2) > _maxDistanceFromOrigin;

        if (position1OutOfBounds && !position2OutOfBounds)
        {
            return new Calculations(position2, position1);
        }

        if (position2OutOfBounds && !position1OutOfBounds)
        {
            return new Calculations(position1, position2);
        }

        if (position1OutOfBounds && position2OutOfBounds)
        {
            position1 = Vector3.ClampMagnitude(position1, _maxDistanceFromOrigin);
            position2 = Vector3.ClampMagnitude(position2, _maxDistanceFromOrigin);
        }

        var distanceTo1 = Vector3.Distance(myPosition, position1);
        var distanceTo2 = Vector3.Distance(myPosition, position2);

        Vector3 desiredPosition;
        Vector3 alternatePosition;
        if (distanceTo1 < distanceTo2)
        {
            desiredPosition = position1;
            alternatePosition = position2;
        } 
        else
        {
            desiredPosition = position2;
            alternatePosition = position1;
        }

        return new Calculations(desiredPosition, alternatePosition);
    }

    public void SetPaused(bool isPaused) => _movement.SetPaused(isPaused);

    public void Select()
    {
        _lineController.ShowTriangle(this, _corner1, _corner2);
    }

    public void Deselect()
    {
        _lineController.HideLines();
    }

    public void DragTowards(float deltaTime, Vector3 position) => _movement.DragTowards(deltaTime, position);

    public void StopDragging() => _movement.StopDragging();

    public void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        var calculations = Calculate();

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(Position, _corner1.Position);
        Gizmos.DrawLine(Position, _corner2.Position);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_corner1.Position, _corner2.Position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(calculations.DesiredPosition, 1f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(calculations.AlternatePosition, 1f);
    }
}
