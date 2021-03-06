﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSphere : RTObject {
    public float radius;

    private Vector3 _cachedPos;
    private float _cachedRadiusSq;

    private void Start() {
        _cachedPos = transform.position;
        _cachedRadiusSq = (radius + .01f) * (radius + .01f);
    }

    public override HitResult TestHit(Ray ray, RTObject insideObject) {
        var pos = _cachedPos;
        var ap = pos - ray.origin;
        var ax = Vector3.Dot(ray.direction, ap);
        var isInside = insideObject == this;

        if (ax > 0.0f) {
            var sqrMag = (ap - ax * ray.direction).sqrMagnitude;
            if (sqrMag < radius * radius) {
                var a = 1;
                var b = 2 * Vector3.Dot(ray.origin - pos, ray.direction);
                var c = (ray.origin - pos).sqrMagnitude - radius * radius;

                var det = b * b - 4 * a * c;
                if (det >= 0.0f) {
                     var hitPos = ray.origin + ray.direction * (-b + (isInside ? 1 : -1) * Mathf.Sqrt(det)) / (2.0f * a);
                     var normal = (hitPos - pos).normalized;

                     return new HitResult {
                         normal = isInside ? -normal : normal,
                         pos = hitPos,
                         isHit = true,
                         material = material,
                         obj = this,
                         isExit = isInside
                     };
                }
            }
        }

        return new HitResult { isHit = false };
    }
}
