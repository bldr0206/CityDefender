using System.Collections.Generic;
using UnityEngine;

public class BreakableTrigger : MonoBehaviour
{
    [SerializeField]
    private GameObject _breakableAimObject;

    private readonly List<Transform> _breakables = new();
    private Transform _target;

    private void Update()
    {
        UpdateTarget();
        UpdateAimObject();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Breakable") && !_breakables.Contains(other.transform))
        {
            _breakables.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Breakable"))
        {
            _breakables.Remove(other.transform);
        }
    }

    private void UpdateTarget()
    {
        _target = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = _breakables.Count - 1; i >= 0; i--)
        {
            Transform breakable = _breakables[i];
            if (breakable == null || !breakable.gameObject.activeInHierarchy)
            {
                _breakables.RemoveAt(i);
                continue;
            }

            float sqrDistance = (breakable.position - transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                _target = breakable;
            }
        }
    }

    private void UpdateAimObject()
    {
        bool hasTarget = _target != null;
        _breakableAimObject.SetActive(hasTarget);

        if (hasTarget)
        {
            _breakableAimObject.transform.position = _target.position;
        }
    }
}
