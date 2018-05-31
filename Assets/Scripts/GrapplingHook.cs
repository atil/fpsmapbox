using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum HookState
{
    None,
    Off,
    Shooting,
    Pull,
    Hold,
    Loose
}

public class GrapplingHook : MonoBehaviour
{
    // How strong the spring will feel
    private const float HookPullForce = 1f;

    // How fast the hook travels when we launch it
    private const float HookShootSpeed = 320f;

    // The higher this number is, the quicker the spring will come to rest
    private const float DampingCoeff = 0.01f;

    private const float FuelTankCapacity = 3f;

    private const float FuelBurnRate = 1f;

    public HookState State { get; private set; }

    [SerializeField]
    private Camera _mainCamera;
    [SerializeField]
    private LayerMask _excludedLayers;
    [SerializeField]
    private Transform _hookSlot;
    [SerializeField]
    private GameObject _hookPrefab;
    [SerializeField]
    private GameObject _hookEndPrefab;

    private Transform _hookVisual;
    private Transform _hookEnd;
    private Vector3 _hookTargetOnTheWall;
    private float _hookLength;
    private float _remainingFuel;

    private void Start()
    {
        _hookVisual = Instantiate(_hookPrefab).transform;
        _hookVisual.gameObject.SetActive(false);

        _hookEnd = Instantiate(_hookEndPrefab).transform;
        _hookEnd.gameObject.SetActive(false);

        State = HookState.Off;
    }

    public void ExternalUpdate(float dt, Vector3 playerPosition)
    {
        if (State == HookState.Off)
        {
            // Transition: Off -> Shooting
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                if (Physics.Raycast(_mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f)), out hit, float.MaxValue, ~_excludedLayers))
                {
                    _hookEnd.position = _hookSlot.position;
                    _hookTargetOnTheWall = hit.point;
                    _hookVisual.gameObject.SetActive(true);
                    _hookEnd.gameObject.SetActive(true);
                    State = HookState.Shooting;
                }
            }

            AdjustFuel(dt);
        }
        else if (State == HookState.Shooting)
        {
            // Keep going until the target on the wall
            // unless we're gonna reach it in the next frame
            var dx = HookShootSpeed * dt;
            if (Vector3.Distance(_hookEnd.position, _hookTargetOnTheWall) > dx)
            {
                _hookEnd.position = _hookEnd.position + (_hookTargetOnTheWall - _hookEnd.position).normalized * dx;
            }
            else
            {
                // Transition: Shooting -> Pull
                _hookEnd.position = _hookTargetOnTheWall;
                State = HookState.Pull;
            }

        }
        else if (State == HookState.Pull)
        {
            // Transition: Pull -> Hold
            if (!Input.GetMouseButton(0))
            {
                State = HookState.Hold;
                _hookLength = Vector3.Distance(playerPosition, _hookEnd.position);
            }

            AdjustFuel(-dt);
        }
        else if (State == HookState.Hold)
        {
            // Transition: Hold -> Pull
            if (Input.GetMouseButtonDown(0))
            {
                State = HookState.Pull;
            }

            // Transition: Hold -> Loose
            if (Input.GetMouseButtonDown(1))
            {
                State = HookState.Loose;
            }

            AdjustFuel(dt);
        }
        else if (State == HookState.Loose)
        {
            // Transition: Loose -> Hold
            if (Input.GetMouseButtonUp(1))
            {
                State = HookState.Hold;
                _hookLength = Vector3.Distance(playerPosition, _hookEnd.position);
            }

            AdjustFuel(dt);
        }

        // Pressing F or running out of fuel force breaks the hook
        if (Input.GetKeyDown(KeyCode.F) || _remainingFuel <= 0)
        {
            State = HookState.Off;
            _hookVisual.gameObject.SetActive(false);
            _hookEnd.gameObject.SetActive(false);
        }
    }

    // Apply simple spring physics
    public void ApplyHookAcceleration(ref Vector3 playerVelocity, Transform playerTransform, Vector3 moveInput)
    {
        if (State != HookState.Pull)
        {
            return;
        }

        var springDir = (_hookEnd.position - playerTransform.position).normalized;
        var damping = playerVelocity * DampingCoeff;

        playerVelocity += HookPullForce * springDir;

        var sideAccel = moveInput.x * 0.8f;
        playerVelocity += sideAccel * playerTransform.right;

        playerVelocity -= damping;
    }

    // Prevent it going further than the length of the hook
    public bool ApplyHookDisplacement(ref Vector3 playerVelocity, ref Vector3 displacement, Vector3 playerPosition)
    {
        if (State != HookState.Hold)
        {
            return false;
        }

        var distance = Vector3.Distance(playerPosition, _hookEnd.position);
        if (distance > _hookLength)
        {
            // The player will have no velocity component in the hook's direction
            var playerToEndDir = (_hookEnd.position - playerPosition).normalized;
            playerVelocity -= Vector3.Project(playerVelocity, playerToEndDir);

            displacement = playerToEndDir * (distance - _hookLength);
            return true;
        }

        return false;
    }

    // Fuel is burned only when the hook is pulling
    // Otherwise it's always charging
    private void AdjustFuel(float amount)
    {
        _remainingFuel += amount * FuelBurnRate;
        _remainingFuel = Mathf.Clamp(_remainingFuel, 0f, FuelTankCapacity);
    }

    // For UI
    public float GetRemainingFuel()
    {
        return _remainingFuel / FuelTankCapacity;
    }

    // Mess with the poor cube's transform to make it look good
    public void Draw()
    {
        _hookVisual.transform.position = (_hookEnd.position + _hookSlot.position) / 2f;
        _hookVisual.transform.localScale = new Vector3(0.1f, 0.1f, Vector3.Distance(_hookEnd.position, _hookSlot.position));

        if (Vector3.Distance(_hookEnd.position, _hookSlot.position) > 0.1f)
        {
            _hookVisual.transform.rotation = Quaternion.LookRotation(_hookEnd.position - _hookSlot.position);
        }
    }

    public void ResetHook()
    {
        State = HookState.Off;
        _hookVisual.gameObject.SetActive(false);
    }
}
