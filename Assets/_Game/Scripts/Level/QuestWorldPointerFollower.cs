using UnityEngine;

public class QuestWorldPointerFollower : MonoBehaviour
{
    [SerializeField] GameObject _groundDecal;

    Transform _anchor;
    Vector3 _offset;
    bool _offsetIsWorld;

    public void Configure(Transform anchor, Vector3 offset, bool offsetIsWorld, bool showGroundDecal)
    {
        _anchor = anchor;
        _offset = offset;
        _offsetIsWorld = offsetIsWorld;

        if (_groundDecal != null)
            _groundDecal.SetActive(showGroundDecal);
    }

    void LateUpdate()
    {
        if (_anchor == null)
            return;

        transform.position = _offsetIsWorld
            ? _anchor.position + _offset
            : _anchor.position + _anchor.rotation * _offset;
    }
}
