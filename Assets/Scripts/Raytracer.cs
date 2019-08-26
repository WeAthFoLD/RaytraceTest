using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitResult {
    public Vector3 pos;
    public Color color;
}

public class Raytracer : MonoBehaviour {
    public static Raytracer Instance { get; private set; }

    public Camera unityCamera;
    public new RTCamera camera;

    public Color backgroundColor = Color.grey;

    private MeshRenderer _meshRenderer;

    private readonly List<RTObject> _objects = new List<RTObject>();

    private Texture2D _texture;

    private void Awake() {
        Instance = this;
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.sharedMaterial = Instantiate(_meshRenderer.sharedMaterial);

        Debug.Log("Awake!");
    }

    public void Register(RTObject obj) {
        _objects.Add(obj);
    }

    public void Unregister(RTObject obj) {
        _objects.Remove(obj);
    }

    void Update() {
        // Adjust mesh scale
        var aspect = unityCamera.aspect;
        var nscl = transform.localScale;
        nscl.x = nscl.y * aspect;
        transform.localScale = nscl;

        var material = _meshRenderer.sharedMaterial;

        // Update texture to match screen
        if (!_texture || _texture.width != Screen.width || _texture.height != Screen.height) {
            material.SetTexture("_MainTex", null);
            if (_texture)
                Destroy(_texture);
            _texture = new Texture2D(Screen.width, Screen.height);
            material.SetTexture("_MainTex", _texture);

            Debug.Log("Set Texture!" + _texture);
//            _meshRenderer.sharedMaterial = material;
            _meshRenderer.sharedMaterials = new[] {material};
        }

        // Do raytracing
        DoRaytrace();

        _texture.Apply();
//        _meshRenderer.material = material;
    }

    private void DoRaytrace() {
        float zNear = 1.0f / Mathf.Tan(Mathf.Deg2Rad * camera.fov / 2);

        for (int x = 0; x < Screen.width; ++x) {
            for (int y = 0; y < Screen.height; ++y) {
                float xNDC = 2 * (-0.5f + ((float) x / Screen.width));
                float yNDC = 2 * (-0.5f + ((float) y / Screen.height));

                var dir = new Vector3(xNDC * unityCamera.aspect, yNDC, zNear).normalized;
                var worldDir = camera.transform.TransformDirection(dir);
                var ray = new Ray(camera.transform.position, worldDir);

                var pixel = CalcPixel(ray);
                _texture.SetPixel(x, y, pixel);
            }
        }
    }

    private Color CalcPixel(Ray ray) {
        float minDist = float.MaxValue;
        HitResult hitResult = null;
        foreach (var obj in _objects) {
            var result = obj.TestHit(ray);
            if (result != null) {
                var distance = Vector3.Distance(result.pos, ray.origin);
                if (distance < minDist) {
                    minDist = distance;
                    hitResult = result;
                }
            }
        }

        Color color = hitResult?.color ?? backgroundColor;
        return color;
    }
}
