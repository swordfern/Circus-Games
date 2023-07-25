using UnityEngine;

public class FriendsAndEnemiesEntity : MonoBehaviour, IEntity
{
    public Vector3 Position => _movement.Position;
    public float PersonalSpace => 1f;

    private float CombinedPersonalSpace => PersonalSpace + _friend.PersonalSpace;

    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private EntityMovement _movement;
    [SerializeField] private Rigidbody _rigidbody;

    private float _maxDistanceFromOrigin;
    private IEntity _friend;
    private IEntity _enemy;
    private LineController _lineController;

    public void Initialize(EntityArgs args)
    {
        _movement.Initialize(args);
        _maxDistanceFromOrigin = args.MaxDistanceFromOrgin;
        _meshRenderer.sharedMaterial = args.Material;
        var (friend, enemy) = Utils.GetTwoDifferentIndices(args.Entities.Count, args.MyIndex);
        _friend = args.Entities[friend].entity;
        _enemy = args.Entities[enemy].entity;
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
        public readonly Vector3 MeToFriend;

        public Calculations(Vector3 desiredPosition, Vector3 meToFriend)
        {
            DesiredPosition = desiredPosition;
            MeToFriend = meToFriend;
        }
    }

    private Calculations Calculate()
    {
        // The formula for projecting a vector v onto a line s is (v·s)/(s·s) * s
        // Source: https://en.wikibooks.org/wiki/Linear_Algebra/Orthogonal_Projection_Onto_a_Line

        var myPosition = Position;
        var friendPosition = _friend.Position;
        var enemyPosition = _enemy.Position;

        var vectorFromMeToFriend = myPosition - friendPosition;
        var vectorFromFriendToEnemy = friendPosition - enemyPosition;

        var dotMeToLine = Vector3.Dot(vectorFromMeToFriend, vectorFromFriendToEnemy);
        var dotLineToLine = Vector3.Dot(vectorFromFriendToEnemy, vectorFromFriendToEnemy);
        Vector3 projectedPosition;
        if (Mathf.Approximately(0f, dotLineToLine))
        {
            projectedPosition = PositionNearFriend(friendPosition, enemyPosition);
        }
        else
        {
            var projectedPoint = dotMeToLine / dotLineToLine;

            // Because the calcualtions are relative to the friend's position rather than the origin,
            // add friend's position to our projection
            projectedPosition = friendPosition + (projectedPoint * vectorFromFriendToEnemy);
        }

        projectedPosition = ProtectedPosition(friendPosition, enemyPosition, projectedPosition);
        projectedPosition = Vector3.ClampMagnitude(projectedPosition, _maxDistanceFromOrigin);

        return new Calculations(projectedPosition, vectorFromMeToFriend);
    }

    private Vector3 ProtectedPosition(Vector3 friendPosition, Vector3 enemyPosition, Vector3 projectedPosition)
    {
        // Position projected on the line must be on the side of the friend away from the enemy
        var friendXIsLargerThanEnemyX = friendPosition.x > enemyPosition.x;
        var friendYIsLargerThanEnemyY = friendPosition.y > enemyPosition.y;

        var xProtected = friendXIsLargerThanEnemyX ? projectedPosition.x > friendPosition.x : projectedPosition.x < friendPosition.x;
        var yProtected = friendYIsLargerThanEnemyY ? projectedPosition.y > friendPosition.y : projectedPosition.y < friendPosition.y;

        var farEnoughFromFriend = Vector3.Distance(projectedPosition, friendPosition) >= CombinedPersonalSpace;

        if (xProtected && yProtected && farEnoughFromFriend)
        {
            return projectedPosition;
        }

        return PositionNearFriend(friendPosition, enemyPosition, friendXIsLargerThanEnemyX, friendYIsLargerThanEnemyY);
    }

    private Vector3 PositionNearFriend(Vector3 friendPosition, Vector3 enemyPosition)
    {
        var friendXIsLargerThanEnemyX = friendPosition.x > enemyPosition.x;
        var friendYIsLargerThanEnemyY = friendPosition.y > enemyPosition.y;
        return PositionNearFriend(friendPosition, enemyPosition, friendXIsLargerThanEnemyX, friendYIsLargerThanEnemyY);
    }

    private Vector3 PositionNearFriend(Vector3 friendPosition, Vector3 enemyPosition, bool friendXIsLargerThanEnemyX, bool friendYIsLargerThanEnemyY)
    {
        // Slope-Intercept
        // y = slope * x + intercept
        // slope = (y1 - y2) / (x1 - x2)
        // intercept = y - slope * x
        // x = (y - intercept) / slope;

        var changeInY = friendPosition.y - enemyPosition.y;
        var changeInX = friendPosition.x - enemyPosition.x;
        if (Mathf.Approximately(0f, changeInX))
        {
            var newY = friendYIsLargerThanEnemyY ? friendPosition.y + CombinedPersonalSpace : friendPosition.y - CombinedPersonalSpace;
            var newX = friendPosition.x;
            return new Vector3(newX, newY);
        }

        var slope = changeInY / changeInX;
        var intercept = friendPosition.y - (slope * friendPosition.x);

        // Point-Slope
        // y = slope * (x - x1) + y1
        // Circle Equation
        // (x - x1)^2 + (y - y1)^2 = radius^2

        // (x - x1)^2 + (slope * (x - x1) + y1 - y1)^2 = radius^2
        // (x - x1)^2 + slope^2 * (x - x1)^2 = radius^2
        // (slope^2 + 1) * (x - x1)^2 = radius^2
        // (x - x1)^2 = radius^2 / (slope^2 + 1)
        // x - x1 = radius / (+/-sqrt(slope^2 + 1))
        // x = x1 +/- radius / sqrt(slope^2 + 1)

        var sign = friendXIsLargerThanEnemyX ? 1 : -1;

        var newXAtProperDistance = friendPosition.x + (sign * CombinedPersonalSpace / Mathf.Sqrt(slope * slope + 1));
        var newYAtProperDistance = slope * newXAtProperDistance + intercept;
        var newPosition = new Vector3(newXAtProperDistance, newYAtProperDistance);
        return newPosition;
    }

    public void SetPaused(bool isPaused) => _movement.SetPaused(isPaused);

    public void Select()
    {
        _lineController.ShowFriendAndEnemy(this, _friend, _enemy);
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
        Gizmos.DrawLine(Position, _friend.Position);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_friend.Position, _enemy.Position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(calculations.DesiredPosition, 1f);
    }
}
