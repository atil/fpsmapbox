using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Ui : MonoBehaviour
{
    [SerializeField]
    private Text _speedText;

    [SerializeField]
    private RectTransform _hookFuelIndicator;

    private FpsController _controller;

    private void Start()
    {
        _controller = FindObjectOfType<FpsController>();
    }

    private void Update()
    {
        if (_controller != null)
        {
            _speedText.text = _controller.Velocity.ToString("F1");
            _hookFuelIndicator.localScale = Vector3.one.WithX(_controller.HookFuel);
        }
    }
}
