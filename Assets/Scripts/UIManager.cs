using System.Collections;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Controller _controller;
    [SerializeField] private TMP_Dropdown _gameMode;
    [SerializeField] private TMP_Dropdown _navigationType;

    [SerializeField] private RectTransform _uiPanel;
    [SerializeField] private GameObject _slideOut;
    [SerializeField] private GameObject _slideIn;

    [SerializeField] private TMP_InputField _entityCount;
    [SerializeField] private TMP_InputField _staticCount;
    [SerializeField] private TextMeshProUGUI _startText;
    [SerializeField] private GameObject _pauseButton;
    [SerializeField] private GameObject _resumeButton;
    [SerializeField] private GameObject _resetCameraButton;

    private Camera _mainCamera;
    private Transform _cameraTransform;
    private bool _uiVisible = true;

    private Coroutine _slideUICoroutine;
    private Coroutine _adjustCameraSizeCoroutine;

    private int _lastEntityCount;
    private int _lastMaxDistance;
    private int _lastCameraSize;

    private int LastEntityCount
    { 
        get
        {
            if (_lastEntityCount == 0)
            {
                _lastEntityCount = _controller.TotalEntityCount;
            }
            return _lastEntityCount; 
        } 
    }

    private int LastMaxDistance
    {
        get
        {
            if (_lastMaxDistance == 0)
            {
                _lastMaxDistance = _controller.MaxDistance;
            }
            return _lastMaxDistance;
        }
    }
    private int LastCameraSize
    {
        get
        {
            if (_lastCameraSize == 0)
            {
                var (_, _, cameraSize, _) = CalculateParameters(LastEntityCount);
                _lastCameraSize = cameraSize;
            }
            return _lastCameraSize;
        }
    }


    private void Start()
    {
        _gameMode.value = (int)_controller.Mode;
        _navigationType.value = (int)_controller.NavigationType;
        _entityCount.text = _controller.EntityCount.ToString();
        _staticCount.text = _controller.StaticCount.ToString();
        _mainCamera = Camera.main;
        _cameraTransform = _mainCamera.transform;
    }

    public void ToggleUI()
    {
        if (_slideUICoroutine != null)
        {
            StopCoroutine(_slideUICoroutine);
        }

        _uiVisible = !_uiVisible;
        _slideOut.SetActive(!_uiVisible);
        _slideIn.SetActive(_uiVisible);
        _slideUICoroutine = ToggleUIVisibilityCoroutine();
    }

    private Coroutine ToggleUIVisibilityCoroutine()
    {
        const float Speed = 750f;

        var startPosition = _uiPanel.anchoredPosition.x;
        var goal = _uiVisible ? 0f : -1 * _uiPanel.sizeDelta.x;

        return StartCoroutine(Lerp(_uiPanel, startPosition, goal, Speed,
            (uiPanel, val, _) =>
            {
                var oldPosition = uiPanel.anchoredPosition;
                uiPanel.anchoredPosition = new Vector2(val, oldPosition.y);
            },
            (uiPanel) => uiPanel.anchoredPosition.x));
    }

    public void SetMode(int mode)
    {
        GameMode gameMode = (GameMode)mode;
        _controller.Mode = gameMode;
    }

    public void SetNavigation(int navigation)
    {
        NavigationType navigationType = (NavigationType)navigation;
        _controller.NavigationType = navigationType;
    }

    private const int AbsoluteMin = 3;
    private const int AbsoluteMax = 2000;

    public void SetEntityCount(string entities)
    {
        const int NonStaticMin = 1;

        var staticCount = _controller.StaticCount;
        var min = staticCount < AbsoluteMin ? AbsoluteMin - staticCount : NonStaticMin;
        var max = AbsoluteMax - staticCount;

        if (string.IsNullOrEmpty(entities) || !int.TryParse(entities, out var count))
        {
            count = min;
        }

        count = Mathf.Clamp(count, min, max);
        _entityCount.text = count.ToString();

        _controller.EntityCount = count;
    }

    public void SetStaticCount(string entities)
    {
        const int StaticMin = 0;

        var entityCount = _controller.EntityCount;
        var min = entityCount < AbsoluteMin ? AbsoluteMin - entityCount : StaticMin;
        var max = AbsoluteMax - entityCount;

        if (string.IsNullOrEmpty(entities) || !int.TryParse(entities, out var count))
        {
            count = min;
        }

        count = Mathf.Clamp(count, min, max);
        _staticCount.text = count.ToString();

        _controller.StaticCount = count;
    }

    public void StartPressed()
    {
        _startText.text = "Restart";
        _pauseButton.SetActive(true);
        _resumeButton.SetActive(false);

        _lastEntityCount = _controller.TotalEntityCount;
        var (spawnArea, maxDistance, cameraSize, lineWidth) = CalculateParameters(_lastEntityCount);
        _lastMaxDistance = maxDistance;
        _lastCameraSize = cameraSize;
        _controller.SpawnArea = spawnArea;
        _controller.MaxDistance = maxDistance;
        _controller.LineController.SetLineWidth(lineWidth);
        AdjustCameraSize(cameraSize);
        if (_uiVisible)
        {
            ToggleUI();
        }

        _controller.StartSimulation();
    }

    public void TogglePause()
    {
        _controller.TogglePause();
        var isRunning = _controller.IsRunning;
        _pauseButton.SetActive(isRunning);
        _resumeButton.SetActive(!isRunning);
    }

    public void ResetCamera()
    {
        AdjustCameraSize(LastCameraSize);
    }

    private (int spawnArea, int maxDistance, int cameraSize, float lineWidth) CalculateParameters(int entitiesCount)
    {
        var spawnArea = CalculateSpawnArea(entitiesCount);
        var distance = CalculateMaxDistance(spawnArea);
        var maxDistance = Mathf.CeilToInt(distance);
        var cameraSize = CalculateCameraSize(maxDistance);
        var lineWidth = CalculateLineWidthFromCameraSize(cameraSize);

        return (Mathf.CeilToInt(spawnArea), maxDistance, cameraSize, lineWidth);
    }

    private static float CalculateSpawnArea(int entitiesCount)
    {
        // Formula derived from graphing good-feeling points and finding best fit formula
        const float Scaler = 2f;
        return Scaler * Mathf.Sqrt(entitiesCount);
    }

    private static float CalculateMaxDistance(float spawnArea)
    {
        const float Buffer = 5f;
        return spawnArea + Buffer;
    }

    private static int CalculateCameraSize(int maxDistance)
    {
        const int CameraBuffer = 5;
        return maxDistance + CameraBuffer;
    }

    private static float CalculateLineWidthFromCameraSize(float cameraSize)
    {
        // Formula derived from graphing good-feeling points and finding best fit formula
        const float Scaler = .1f;
        const float Offset = 10f;

        var square = Mathf.Max(1, cameraSize - Offset);
        return Scaler * Mathf.Sqrt(square); 
    }

    public void ZoomCamera(float zoomDelta)
    {
        const float MinCameraSize = 5f;
        const float MaxCameraSizeScaler = 1.5f;

        _resetCameraButton.SetActive(true);

        if (_adjustCameraSizeCoroutine != null)
        {
            StopCoroutine(_adjustCameraSizeCoroutine);
            _adjustCameraSizeCoroutine = null;
        }

        var currentSize = _mainCamera.orthographicSize;
        var newSize = currentSize + zoomDelta;
        newSize = Mathf.Clamp(newSize, MinCameraSize, LastCameraSize * MaxCameraSizeScaler);

        _mainCamera.orthographicSize = newSize;

        var lineWidth = CalculateLineWidthFromCameraSize(newSize);
        _controller.LineController.SetLineWidth(lineWidth);
    }

    public void MoveCameraImmediate(Vector3 moveDelta)
    {
        _resetCameraButton.SetActive(true);
        var newPosition = _cameraTransform.position + moveDelta;
        var maxPosition = LastMaxDistance;
        var x = Mathf.Clamp(newPosition.x, -maxPosition, maxPosition);
        var y = Mathf.Clamp(newPosition.y, -maxPosition, maxPosition);

        _cameraTransform.position = new Vector3(x, y, newPosition.z);
    }

    private void AdjustCameraSize(int desiredSize)
    {
        const float ChangeInSizePerSecond = 30f;

        _resetCameraButton.SetActive(false);
        if (_adjustCameraSizeCoroutine != null)
        {
            StopCoroutine(_adjustCameraSizeCoroutine);
        }

        var startPosition = _cameraTransform.position;
        _adjustCameraSizeCoroutine = StartCoroutine(Lerp((_mainCamera, _cameraTransform, startPosition), _mainCamera.orthographicSize, desiredSize, ChangeInSizePerSecond,
            (tuple, val, percentComplete) =>
            {
                const float CameraZ = -10f;

                tuple._mainCamera.orthographicSize = val;
                tuple._cameraTransform.position = Vector3.Lerp(tuple.startPosition, new Vector3(0f, 0f, CameraZ), percentComplete);
            }
            ,
            (tuple) => tuple._mainCamera.orthographicSize));
    }

    private delegate void SetValue<T>(T obj, float val, float percentComplete);
    private delegate float GetValue<T>(T obj);

    private IEnumerator Lerp<T>(T obj, float start, float goal, float speed, SetValue<T> setVal, GetValue<T> getVal)
    {
        var difference = Mathf.Abs(start - goal);
        var totalChange = 0f;
        while (!Mathf.Approximately(goal, getVal(obj)))
        {
            var t = totalChange / difference;
            setVal(obj, Mathf.Lerp(start, goal, t), t);
            totalChange += speed * Time.deltaTime;
            yield return null;
        }
    }
}
