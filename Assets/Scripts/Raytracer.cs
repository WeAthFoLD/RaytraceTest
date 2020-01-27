using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using URandom = UnityEngine.Random;

public struct HitResult {
    public bool isHit;
    public bool isExit; // 光线是否从当前物体内部退出 
    public Vector3 pos;
    public Vector3 normal;

    public RTMaterial material;
    // public Color color;

    public static HitResult Empty = new HitResult {isHit = false};
}

public class Raytracer : MonoBehaviour {
    public static Raytracer Instance { get; private set; }

    public Camera unityCamera;
    public new RTCamera camera;

    public GameObject sceneObject;

    public Color backgroundColor = Color.grey;
    public Color backgroundColor2 = Color.blue;

    public int samplesPerPixel = 25;

    public bool enableDebugUI = true;

    private MeshRenderer _meshRenderer;

    private List<RTObject> _objects;

    private Texture2D _texture;

    private bool _realTime;

    private float _renderTime;

    private void Awake() {
        Instance = this;
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.sharedMaterial = Instantiate(_meshRenderer.sharedMaterial);
        _objects = sceneObject.GetComponentsInChildren<RTObject>().ToList();
    }

    private IEnumerator Start() {
        yield return null;
        RenderOnce();
    }

    public void Register(RTObject obj) {
        Debug.Log("Register " + obj);
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
        if (!enableDebugUI)
            return;
        
        if (GUILayout.Button("Render")) {
            RenderOnce();
        }

        _realTime = GUILayout.Toggle(_realTime, "Real Time");
        GUILayout.Label("Render Time: " + _renderTime.ToString(CultureInfo.InvariantCulture) + " ms");
    }

    private void DoRaytrace() {
        float zNear = 1.0f / Mathf.Tan(Mathf.Deg2Rad * camera.fov / 2);

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

                    accumulated += CalcPixel(ray, 1.0f);
                }

                var finalColor = accumulated / samplesPerPixel;
                finalColor.r = GammaCorrection(finalColor.r);
                finalColor.g = GammaCorrection(finalColor.g);
                finalColor.b = GammaCorrection(finalColor.b);

                _texture.SetPixel(x, y, finalColor);
            }
        }
    }

    private const int MAX_ITERATION = 25;

    private float GammaCorrection(float x) {
        return Mathf.Sqrt(x);
    }

    private Color CalcPixel(Ray ray, float refractIndex, int iteration = 0) {
        if (iteration <= MAX_ITERATION) {
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

            if (hitResult.isHit) {
                // 漫反射
                Vector3 diffuseFinalPos = hitResult.pos + hitResult.normal + URandom.insideUnitSphere;
                Vector3 diffuseDir = (diffuseFinalPos - hitResult.pos).normalized;

                float cosTheta = Vector3.Dot(ray.direction, -hitResult.normal);
                
                // 计算折射，如果需要的话
                bool hasRefracted = false;
                Vector3 refractedDir = Vector3.zero;
                if (hitResult.material.isDieletric) {
                    var n = refractIndex;
                    var n1 = hitResult.isExit ? 1 : hitResult.material.refractIndex;
                    var refractRatio = n / n1;

                    var cosTheta1Sq = 1 - (refractRatio * refractRatio) * (1 - cosTheta * cosTheta);
                    // if (false) {
                    if (cosTheta1Sq > 0) {
                        hasRefracted = true;
                        var sinTheta1 = Mathf.Sqrt(1 - cosTheta1Sq);
                        // var sinTheta1 = Mathf.Sqrt(1 - cosTheta * cosTheta);
                        refractedDir =
                            -hitResult.normal * cosTheta + sinTheta1 * (ray.direction + hitResult.normal * cosTheta).normalized;
                        // refractedDir = ray.direction;
                        refractedDir = refractedDir.normalized;
                    }
                }

                if (hasRefracted) {
                    var nextColor = CalcPixel(new Ray(hitResult.pos + refractedDir * .0001f, 
                            refractedDir),
                        hitResult.isExit ? 1 : hitResult.material.refractIndex,
                        iteration + 1);
                    return ScaleColor(hitResult.material.albedoo, nextColor);
                }
                else {
                    var reflectedDir =
                        ray.direction + 2 * cosTheta * hitResult.normal;
                    Vector3 finalDir = Vector3.Slerp(reflectedDir, diffuseDir, hitResult.material.roughness).normalized;
                    var nextColor = CalcPixel(
                               new Ray(hitResult.pos + 0.001f * hitResult.normal, finalDir),
                               refractIndex,
                               iteration + 1);
                    return ScaleColor(hitResult.material.albedoo, nextColor);
                }
            }
            // return NormalToColor(hitResult.normal);
        }

        // 天空，写死的渐变
        var t = 0.5f * (ray.direction.y + 1.0f);
        return Color.Lerp(backgroundColor, backgroundColor2, t);
    }

    private Color ScaleColor(Color lhs, Color rhs) {
        return new Color(lhs.r * rhs.r, lhs.g * rhs.g, lhs.b * rhs.b, 1);
    }

    private Color NormalToColor(Vector3 normal) {
         return new Color(
             MathUtils.NDC2Unit(normal.x),
             MathUtils.NDC2Unit(normal.y),
             MathUtils.NDC2Unit(normal.z));
    }
}
