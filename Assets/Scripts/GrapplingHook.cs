using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GrapplingHook
{
    private enum HookState
    {
        None,
        Off,
        Pull,
        Hold,
        Loose
    }

    // How strong the spring will feel
    private const float SpringTightness = 0.5f;

    // The higher this number is, the quicker the spring will come to rest
    private const float DampingCoeff = 0.01f;

    private readonly Transform _hookVisual;
    private readonly Vector3 _screenMidPoint;
    private readonly Camera _mainCamera;
    private readonly LayerMask _excludedLayers;

    private Vector3 _springEnd;
    private HookState _state;
    private float _hookLength;

    public GrapplingHook(GameObject hookPrefab, Vector3 screenMidPoint, Camera camera, LayerMask excludedLayers)
    {
        _screenMidPoint = screenMidPoint;
        _hookVisual = GameObject.Instantiate(hookPrefab).transform;
        _hookVisual.gameObject.SetActive(false);
        _state = HookState.Off;
        _mainCamera = camera;
        _excludedLayers = excludedLayers;
    }

    public void Update(float dt, Vector3 playerPosition)
    {
        if (_state == HookState.Off)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                if (Physics.Raycast(_mainCamera.ScreenPointToRay(_screenMidPoint), out hit, float.MaxValue, ~_excludedLayers))
                {
                    _springEnd = hit.point;
                    _hookVisual.gameObject.SetActive(true);
                    _state = HookState.Pull;
                }
            }
        }
        else if (_state == HookState.Pull)
        {
            if (Input.GetMouseButtonUp(0))
            {
                _state = HookState.Hold;
                _hookLength = Vector3.Distance(playerPosition, _springEnd);
            }
        }
        else if (_state == HookState.Hold)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _state = HookState.Pull;
            }
            if (Input.GetMouseButtonDown(1))
            {
                _state = HookState.Loose;
            }
        }
        else if (_state == HookState.Loose)
        {
            if (Input.GetMouseButtonUp(1))
            {
                _state = HookState.Hold;
                _hookLength = Vector3.Distance(playerPosition, _springEnd);
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            _state = HookState.Off;
            _hookVisual.gameObject.SetActive(false);
        }
    }

    public void ApplyHookAcceleration(ref Vector3 playerVelocity, Vector3 playerPosition)
    {
        if (_state != HookState.Pull)
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
        if (_state != HookState.Hold)
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

    public void Draw(Vector3 playerPosition)
    {
        _hookVisual.transform.position = (_springEnd + playerPosition) / 2f;
        _hookVisual.transform.rotation = Quaternion.LookRotation(_springEnd - playerPosition);
        _hookVisual.transform.localScale = new Vector3(0.05f, 0.05f, Vector3.Distance(_springEnd, playerPosition));
    }

    public void Reset()
    {
        _state = HookState.Off;
        _hookVisual.gameObject.SetActive(false);
    }
}
