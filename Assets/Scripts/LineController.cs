using UnityEngine;

public class LineController : MonoBehaviour
{
    [SerializeField] private LineRenderer _triangle1;
    [SerializeField] private LineRenderer _triangle2;
    [SerializeField] private LineRenderer _friend;
    [SerializeField] private LineRenderer _enemy;

    private readonly LineRenderer[] _allLines = new LineRenderer[4];

    private readonly (LineRenderer line, IEntity start, IEntity end)[] _activeLines = new (LineRenderer line, IEntity start, IEntity end)[2];
    private bool _linesShown;

    private void Start()
    {
        _allLines[0] = _triangle1;
        _allLines[1] = _triangle2;
        _allLines[2] = _friend;
        _allLines[3] = _enemy;
    }

    public void SetLineWidth(float width)
    {
        for (int i = 0; i < _allLines.Length; i++)
        {
            _allLines[i].startWidth = width;
            _allLines[i].endWidth = width;
        }
    }

    public void HideLines()
    {
        _linesShown = false;

        for (int i = 0; i < _allLines.Length; i++)
        {
            _allLines[i].gameObject.SetActive(false);
        }
    }

    public void ShowTriangle(IEntity selected, IEntity side1, IEntity side2)
    {
        _linesShown = true;
        _activeLines[0] = (_triangle1, selected, side1);
        _activeLines[1] = (_triangle2, selected, side2);
    }

    public void ShowFriendAndEnemy(IEntity selected, IEntity friend, IEntity enemy)
    {
        _linesShown = true;
        _activeLines[0] = (_friend, selected, friend);
        _activeLines[1] = (_enemy, friend, enemy);
    }

    private void Update()
    {
        if (!_linesShown)
        {
            return;
        }

        for (int i = 0; i < _activeLines.Length; i++)
        {
            var (line, start, end) = _activeLines[i];
            line.gameObject.SetActive(true);

            line.SetPosition(0, start.Position);
            line.SetPosition(1, end.Position);
        }
    }
}
