﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Util
{
    public static Vector3 WithX(this Vector3 v, float x)
    {
        return new Vector3(x, v.y, v.z);
    }

    public static Vector3 WithY(this Vector3 v, float y)
    {
        return new Vector3(v.x, y, v.z);
    }

    public static Vector3 WithZ(this Vector3 v, float z)
    {
        return new Vector3(v.x, v.y, z);
    }

    public static Vector3 ToHorizontal(this Vector3 v)
    {
        return Vector3.ProjectOnPlane(v, Vector3.up);
    }

    public static Vector3 TransformDirectionHorizontal(this Transform t, Vector3 v)
    {
        return t.TransformDirection(v).ToHorizontal().normalized;
    }

    public static Vector3 InverseTransformDirectionHorizontal(this Transform t, Vector3 v)
    {
        return t.InverseTransformDirection(v).ToHorizontal().normalized;
    }
}
