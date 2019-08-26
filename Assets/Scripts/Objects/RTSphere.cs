using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSphere : RTObject {
    public float radius;

    public override HitResult TestHit(Ray ray) {
        return null;
    }
}
