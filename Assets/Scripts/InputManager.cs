using UnityEngine;

public class InputManager : MonoBehaviour
{
    private readonly RaycastHit[] _raycastHits = new RaycastHit[4];

    private Vector3 MouseWorldPosition
    {
        get
        {
            Vector3 screenCoordinates;
#if MOBILE_INPUT
            if (Input.touchCount > 0)
            {
                var touchPosition = Input.GetTouch(0).position;
                screenCoordinates = new Vector3(touchPosition.x, touchPosition.y);
            }
            else
#endif
            {
                screenCoordinates = Input.mousePosition;
            } 
            return _mainCamera.ScreenToWorldPoint(screenCoordinates);
        }
    }

    [SerializeField] private UIManager _uiManager;

    private Camera _mainCamera;
    private IEntity _selectedEntity;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        var input = GetInput();
        switch (input)
        {
            case InputPhase.Started:
                HandleInputStarted();
                break;
            case InputPhase.Dragging:
                HandleDragging();
                break;
            case InputPhase.Ended:
                HandleInputEnded();
                break;
            case InputPhase.None:
                break;
        }

        HandleZoom();
    }

    private enum InputPhase
    {
        None,
        Started,
        Dragging,
        Ended
    }

    private InputPhase GetInput()
    {
        const int LeftMouseButton = 0;

        if (Input.GetMouseButtonDown(LeftMouseButton))
        {
            return InputPhase.Started;
        }
        if (Input.GetMouseButton(LeftMouseButton))
        {
            return InputPhase.Dragging;
        }
        if (Input.GetMouseButtonUp(LeftMouseButton))
        {
            return InputPhase.Ended;
        }

#if MOBILE_INPUT
        if (Input.touchCount == 0)
        {
            return InputPhase.None;
        }

        var touch = Input.GetTouch(0);
        switch (touch.phase)
        {
            case TouchPhase.Began:
                return InputPhase.Started;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                return InputPhase.Dragging;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                return InputPhase.Ended;
            default:
                return InputPhase.None;
        }
#endif
        return InputPhase.None;
    }

    private void HandleInputStarted()
    {
        ClearRaycastHits();
        Physics.RaycastNonAlloc(new Ray(MouseWorldPosition, Vector3.forward), _raycastHits);

        for (int i = 0; i < _raycastHits.Length; i++)
        {
            var hit = _raycastHits[i];
            if (hit.collider == null)
            {
                continue;
            }

            var parentObject = hit.collider.gameObject;
            var entity = parentObject.GetComponent<IEntity>();
            if (entity == null)
            {
                continue;
            }

            if (_selectedEntity != null)
            {
                _selectedEntity.Deselect();
            }

            _selectedEntity = entity;
            _selectedEntity.Select();
            return;
        }

        if (_selectedEntity != null)
        {
            _selectedEntity.Deselect();
            _selectedEntity = null;
        }
    }

    private void ClearRaycastHits()
    {
        for (int i = 0; i < _raycastHits.Length; i++)
        {
            _raycastHits[i] = default;
        }
    }

    private void HandleDragging()
    {
        if (_selectedEntity == null)
        {
            return;
        }

        var mousePosition = MouseWorldPosition;
        var entityPosition = new Vector3(mousePosition.x, mousePosition.y);
        _selectedEntity.DragTowards(Time.deltaTime, entityPosition);
    }

    private void HandleInputEnded()
    {
        if (_selectedEntity != null)
        {
            _selectedEntity.StopDragging();
        }
    }

    private void HandleZoom()
    {
        var scoll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(0f, scoll))
        {
            return;
        }

        var mousePositionBefore = MouseWorldPosition;
        _uiManager.ZoomCamera(-scoll);
        var mousePositionAfter = MouseWorldPosition;
        var diff = mousePositionBefore - mousePositionAfter;
        _uiManager.MoveCameraImmediate(diff);
    }
}
