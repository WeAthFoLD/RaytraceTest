using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSphere : RTObject {
    public float radius;

    public override HitResult TestHit(Ray ray) {
        var pos = transform.position;
        var ap = pos - ray.origin;
        var ax = Vector3.Dot(ray.direction, ap);
        var mag = (ap - ax * ray.direction).magnitude;

        if (ax > 0.0f && mag * mag < radius * radius) {
            var a = 1;
            var b = 2 * Vector3.Dot(ray.origin - pos, ray.direction);
            var c = (ray.origin - pos).sqrMagnitude - radius * radius;

            var det = b * b - 4 * a * c;
            if (det >= 0.0f) {
                 var hitPos = ray.origin + ray.direction * (-b - Mathf.Sqrt(det)) / (2.0f * a);
                 var normal = (hitPos - pos).normalized;
                 var color = new Color(
                     MathUtils.NDC2Unit(normal.x),
                     MathUtils.NDC2Unit(normal.y),
                     MathUtils.NDC2Unit(normal.z));

                 return new HitResult {
                     color = color,
                     pos = hitPos
                 };
            }
        }

        return null;
    }
}
