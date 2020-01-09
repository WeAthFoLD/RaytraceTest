using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using URandom = UnityEngine.Random;

public struct HitResult {
    public bool isHit;
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

    private bool _realTime;

    private float _renderTime;

    private void Awake() {
        Instance = this;
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.sharedMaterial = Instantiate(_meshRenderer.sharedMaterial);

        Debug.Log("Awake!");
    }

    private IEnumerator Start() {
        yield return null;
        RenderOnce();
    }

    public void Register(RTObject obj) {
        _objects.Add(obj);
    }

    public void Unregister(RTObject obj) {
        _objects.Remove(obj);
    }

    private void RenderOnce() {
        var now = DateTime.Now;

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

        var elapsed = DateTime.Now - now;
        _renderTime = (float) elapsed.TotalMilliseconds;
    }

    private void Update() {
        if (_realTime)
            RenderOnce();
    }

    private void OnGUI() {
        if (GUILayout.Button("Render")) {
            RenderOnce();
        }

        _realTime = GUILayout.Toggle(_realTime, "Real Time");
        GUILayout.Label("Render Time: " + _renderTime.ToString(CultureInfo.InvariantCulture) + " ms");
    }

    private void DoRaytrace() {
        float zNear = 1.0f / Mathf.Tan(Mathf.Deg2Rad * camera.fov / 2);
        const int samplesPerPixel = 5;

        var camTrans = camera.transform;
        var camPos = camTrans.position;
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;
        var aspect = unityCamera.aspect;
        for (int x = 0; x < screenWidth; ++x) {
            for (int y = 0; y < screenHeight; ++y) {
                Color accumulated = Color.black;
                for (int s = 0; s < samplesPerPixel; ++s) {
                    float xNDC = 2 * (-0.5f + ((x + URandom.value) / screenWidth));
                    float yNDC = 2 * (-0.5f + ((y + URandom.value) / screenHeight));

                    var dir = new Vector3(xNDC * aspect, yNDC, zNear).normalized;
                    var worldDir = camTrans.TransformDirection(dir);
                    var ray = new Ray(camPos, worldDir);

                    accumulated += CalcPixel(ray);
                }

                _texture.SetPixel(x, y, accumulated / samplesPerPixel);
            }
        }
    }

    private Color CalcPixel(Ray ray) {
        float minDist = float.MaxValue;
        HitResult hitResult = new HitResult();
        foreach (var obj in _objects) {
            var result = obj.TestHit(ray);
            if (result.isHit) {
                var distance = Vector3.Distance(result.pos, ray.origin);
                if (distance < minDist) {
                    minDist = distance;
                    hitResult = result;
                }
            }
        }

        Color color = hitResult.isHit ? hitResult.color : backgroundColor;
        return color;
    }
}
