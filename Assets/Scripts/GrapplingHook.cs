using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum HookState
{
    None,
    Off,
    Pull,
    Hold,
    Loose
}

public class GrapplingHook
{

    // How strong the spring will feel
    private const float SpringTightness = 0.5f;

    // The higher this number is, the quicker the spring will come to rest
    private const float DampingCoeff = 0.01f;

    public HookState State { get; private set; }

    private readonly Transform _hookVisual;
    private readonly Vector3 _screenMidPoint;
    private readonly Camera _mainCamera;
    private readonly LayerMask _excludedLayers;
    private readonly Transform _hookSlot;

    private Vector3 _springEnd;
    private float _hookLength;

    public GrapplingHook(GameObject hookPrefab, Transform hookSlot, Vector3 screenMidPoint, Camera camera, LayerMask excludedLayers)
    {
        _screenMidPoint = screenMidPoint;
        _hookSlot = hookSlot;
        _hookVisual = GameObject.Instantiate(hookPrefab).transform;
        _hookVisual.gameObject.SetActive(false);
        State = HookState.Off;
        _mainCamera = camera;
        _excludedLayers = excludedLayers;
    }

    public void Update(float dt, Vector3 playerPosition)
    {
        if (State == HookState.Off)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                if (Physics.Raycast(_mainCamera.ScreenPointToRay(_screenMidPoint), out hit, float.MaxValue, ~_excludedLayers))
                {
                    _springEnd = hit.point;
                    _hookVisual.gameObject.SetActive(true);
                    State = HookState.Pull;
                }
            }
        }
        else if (State == HookState.Pull)
        {
            if (Input.GetMouseButtonUp(0))
            {
                State = HookState.Hold;
                _hookLength = Vector3.Distance(playerPosition, _springEnd);
            }
        }
        else if (State == HookState.Hold)
        {
            if (Input.GetMouseButtonDown(0))
            {
                State = HookState.Pull;
            }
            if (Input.GetMouseButtonDown(1))
            {
                State = HookState.Loose;
            }
        }
        else if (State == HookState.Loose)
        {
            if (Input.GetMouseButtonUp(1))
            {
                State = HookState.Hold;
                _hookLength = Vector3.Distance(playerPosition, _springEnd);
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            State = HookState.Off;
            _hookVisual.gameObject.SetActive(false);
        }
    }

    public void ApplyHookAcceleration(ref Vector3 playerVelocity, Vector3 playerPosition)
    {
        if (State != HookState.Pull)
        {
            return;
        }

        var springDir = (_springEnd - playerPosition).normalized;
        var damping = playerVelocity * DampingCoeff;

        var springLength = Vector3.Distance(playerPosition, _springEnd);
        playerVelocity += Mathf.Sqrt(Mathf.Sqrt(springLength)) * SpringTightness * springDir;
        playerVelocity -= damping;
    }

    public bool ApplyHookDisplacement(ref Vector3 playerVelocity, ref Vector3 displacement, Vector3 playerPosition)
    {
        if (State != HookState.Hold)
        {
            return false;
        }

        var distance = Vector3.Distance(playerPosition, _springEnd);
        if (distance > _hookLength)
        {
            var playerToEndDir = (_springEnd - playerPosition).normalized;
            playerVelocity -= Vector3.Project(playerVelocity, playerToEndDir);

            displacement = playerToEndDir * (distance - _hookLength);
            return true;
        }

        return false;
    }

    public void Draw()
    {
        _hookVisual.transform.position = (_springEnd + _hookSlot.position) / 2f;
        _hookVisual.transform.rotation = Quaternion.LookRotation(_springEnd - _hookSlot.position);
        _hookVisual.transform.localScale = new Vector3(0.1f, 0.1f, Vector3.Distance(_springEnd, _hookSlot.position));
    }

    public void Reset()
    {
        State = HookState.Off;
        _hookVisual.gameObject.SetActive(false);
    }
}
