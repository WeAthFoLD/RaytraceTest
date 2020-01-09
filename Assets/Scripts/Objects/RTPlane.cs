using System;
using UnityEngine;

// 目前仅是一个 normal = (0, 1, 0), y = transform.position.y 的无限大平面
public class RTPlane : RTObject {
	private float _cachedY = 0;

	private void Start() {
		_cachedY = transform.position.y;
	}

	public override HitResult TestHit(Ray ray) {
		bool isOriginBelow = ray.origin.y < _cachedY;
		bool isDirectionUp = ray.direction.y > 0;
		if (isOriginBelow != isDirectionUp)
			return HitResult.Empty;

		if (Mathf.Abs(ray.direction.y) < 0.01f)
			return HitResult.Empty;

		var deltaY = _cachedY - ray.origin.y;
		var t = deltaY / ray.direction.y;
		return new HitResult {
			pos = ray.origin + t * ray.direction,
			isHit = true,
			normal = new Vector3(0, 1, 0),
			material = material
		};
	}
}
