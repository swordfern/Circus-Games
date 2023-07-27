using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public interface IEntity
{
    Vector3 Position { get; }
    float PersonalSpace { get; }

    void Initialize(EntityArgs args);
    void UpdatePosition(float deltaTime);
    void SetPaused(bool isPaused);

    void Select();
    void Deselect();
    void DragTowards(float deltaTime, Vector3 position);
    void StopDragging();
}

public enum GameMode
{
    FriendsAndEnemies,
    Triangle,
    Mixed
}

public enum NavigationType
{
    Default,
    NavMesh
}

public struct EntityArgs
{
    public readonly Vector3 StartingPosition;
    public readonly Material Material;
    public readonly float Speed;
    public readonly IReadOnlyList<(GameObject go, IEntity entity)> Entities;
    public readonly int MyIndex;
    public readonly float MaxDistanceFromOrgin;
    public readonly NavigationType NavigationType;
    public readonly LineController LineController;

    public EntityArgs(Vector3 startingPosition, Material material, float speed, IReadOnlyList<(GameObject go, IEntity entity)> entities, int myIndex, float maxDistanceFromOrgin, NavigationType navigationType, LineController lineController)
    {
        StartingPosition = startingPosition;
        Material = material;
        Speed = speed;
        Entities = entities;
        MyIndex = myIndex;
        MaxDistanceFromOrgin = maxDistanceFromOrgin;
        NavigationType = navigationType;
        LineController = lineController;
    }
}

public static class Utils
{
    public static (int, int) GetTwoDifferentIndices(int count, int notI)
    {
        int firstIndex;
        int secondIndex;

        do
        {
            firstIndex = Random.Range(0, count);
        } while (firstIndex == notI);

        do
        {
            secondIndex = Random.Range(0, count);
        } while (secondIndex == firstIndex || secondIndex == notI);

        return (firstIndex, secondIndex);
    }
}

public class Controller : MonoBehaviour
{
    public GameMode Mode { get => _mode;  set => _mode = value; }
    public NavigationType NavigationType { get => _navigationType; set => _navigationType = value; }
    public int EntityCount { get => _entityCount; set => _entityCount = value; }
    public int StaticCount { get => _staticEntityCount; set => _staticEntityCount = value; }
    public int TotalEntityCount => EntityCount + StaticCount;
    public int SpawnArea { set => _spawnAreaRadius = value; }
    public int MaxDistance { get => (int)_maxDistanceFromOrigin; set => _maxDistanceFromOrigin = value; }
    public bool IsRunning => _started;
    public LineController LineController => _lineController;

    [SerializeField] private GameMode _mode;
    [SerializeField] private NavigationType _navigationType;
    [SerializeField] private int _entityCount;
    [SerializeField] private int _staticEntityCount;
    [SerializeField] private float _spawnAreaRadius;
    [SerializeField] private float _maxDistanceFromOrigin;
    [SerializeField] private float _entitySpeed;
    [SerializeField] private FriendsAndEnemiesEntity _friendsAndEnemiesPrefab;
    [SerializeField] private TriangleEntity _trianglePrefab;
    [SerializeField] private StaticEntity _staticEntityPrefab;
    [SerializeField] private LineController _lineController;
    [SerializeField] private List<Material> _materials;

    private readonly List<(GameObject go, IEntity entity)> _entities = new List<(GameObject go, IEntity entity)>();
    private bool _started;

    public void StartSimulation()
    {
        _lineController.HideLines();
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            Destroy(_entities[i].go);
        }
        _entities.Clear();

        for (int i = 0; i < _entityCount; i++)
        {
            (GameObject go, IEntity entity) newInstance;
            if (_mode == GameMode.FriendsAndEnemies || (_mode == GameMode.Mixed && (i % 2 == 0)))
            {
                var friendsAndEnemies = GameObject.Instantiate(_friendsAndEnemiesPrefab);
                newInstance = (friendsAndEnemies.gameObject, friendsAndEnemies);
            }
            else
            {
                var triangle = GameObject.Instantiate(_trianglePrefab);
                newInstance = (triangle.gameObject, triangle);
            }

            newInstance.go.SetActive(true);
            _entities.Add(newInstance);
        }

        for (int i = 0; i < _staticEntityCount; i++)
        {
            var staticEntity = GameObject.Instantiate(_staticEntityPrefab);
            staticEntity.gameObject.SetActive(true);
            _entities.Add((staticEntity.gameObject, staticEntity));
        }

        for (int i = 0; i < _entities.Count; i++)
        {
            var (_, entity) = _entities[i];

            var vector2Position = _spawnAreaRadius * Random.insideUnitCircle;
            var position = new Vector3(vector2Position.x, vector2Position.y);

            var material = _materials[i % _materials.Count];

            var args = new EntityArgs(position, material, _entitySpeed, _entities, i, _maxDistanceFromOrigin, _navigationType, _lineController);

            entity.Initialize(args);
        }

        _started = true;
    }

    public void TogglePause()
    {
        SetSimulationPaused(IsRunning);
    }

    public void PauseSimulation()
    {
        SetSimulationPaused(true);
    }

    public void ResumeSimulation()
    {
        SetSimulationPaused(false);
    }

    private void SetSimulationPaused(bool paused)
    {
        _started = !paused;
        for (int i = 0; i < _entities.Count; i++)
        {
            _entities[i].entity.SetPaused(paused);
        }
    }

    private void FixedUpdate()
    {
        UpdateEntities();
    }

    private void UpdateEntities()
    {
        if (!_started)
        {
            return;
        }

        foreach (var (_, entity) in _entities)
        {
            entity.UpdatePosition(Time.fixedDeltaTime);
        }
    }

    public void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(Vector3.zero, _maxDistanceFromOrigin);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Controller))]
public class ControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (!Application.isPlaying)
        {
            DrawDefaultInspector();
            return;
        }

        var controller = (Controller)target;
        if (GUILayout.Button("Start"))
        {
            controller.StartSimulation();
        }
        if (GUILayout.Button("Pause"))
        {
            controller.PauseSimulation();
        }
        if (GUILayout.Button("Resume"))
        {
            controller.ResumeSimulation();
        }
        DrawDefaultInspector();
    }
}
#endif
