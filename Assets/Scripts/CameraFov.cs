using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CameraFov : MonoBehaviour
{
    private const float MinFloat = 90f;
    private const float MaxFloat = 110f;
    private const float MaxSpeed = 50f;

    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private AnimationCurve _curve;

    public void ExternalUpdate(Vector3 velocity)
    {
        if (!enabled)
        {
            return;
        }

        var t = velocity.magnitude / MaxSpeed;
        var targetFov = Mathf.Lerp(MinFloat, MaxFloat, _curve.Evaluate(t));
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, Time.deltaTime * 10);

    }
}
