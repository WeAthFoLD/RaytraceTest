using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RTObject : MonoBehaviour
{
    protected virtual void OnEnable() {
        Raytracer.Instance.Register(this);
    }

    protected virtual void OnDisable() {
        Raytracer.Instance.Unregister(this);
    }

    public abstract HitResult TestHit(Ray ray);
}
