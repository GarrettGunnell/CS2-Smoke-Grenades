using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine;

public class Gun : MonoBehaviour {
    [Range(-2.0f, 2.0f)]
    public float r1 = 0.0f;
    
    [Range(-2.0f, 2.0f)]
    public float r2 = 0.0f;

    [Range(0.0f, 30.0f)]
    public float depth = 15.0f;

    private Camera cam;

    private struct BulletHole {
        public bool active;
        public float t;
        public Vector3 origin;
        public Vector3 forward;
        public Vector2 radius;
    }

    private BulletHole[] bulletHoles;

    private struct GPUBulletHole {
        public Vector3 origin;
        public Vector3 forward;
        public Vector2 radius;
    }
    private List<GPUBulletHole> activeBulletHoles;

    private int maxBulletHoles = 256;
    private int activeBulletHoleCount = 0;
    private ComputeBuffer bulletHoleBuffer;

    public float GetRadius(float r, float t) {
        return Mathf.Lerp(-2.0f, r, Easing(t));
    }

    public float GetDepth() {
        return depth;
    }

    public ComputeBuffer GetBulletHoles() {
        return bulletHoleBuffer;
    }

    public int GetActiveBulletHoleCount() {
        return activeBulletHoleCount;
    }

    float Easing(float x) {
        if (x < 0.25f) return 1.0f - Mathf.Pow(1.0f - 2.0f * x, 15);
        else return 1.0f - Mathf.Pow(1.25f * (x - 0.25f), 2);
    }

    void OnEnable() {
        cam = Camera.main;

        bulletHoleBuffer = new ComputeBuffer(maxBulletHoles, SizeOf(typeof(GPUBulletHole)));

        bulletHoles = new BulletHole[maxBulletHoles];

        for (int i = 0; i < maxBulletHoles; ++i) {
            bulletHoles[i].active = false;
            bulletHoles[i].t = 0;
            bulletHoles[i].origin = new Vector3(0, 0, 0);
            bulletHoles[i].forward = new Vector3(0, 0, 0);
            bulletHoles[i].radius = new Vector2(0, 0);
        }

        activeBulletHoles = new List<GPUBulletHole>();

        for (int i = 0; i < maxBulletHoles; ++i) {
            GPUBulletHole gpuBulletHole = new GPUBulletHole();
            gpuBulletHole.origin = new Vector3(0, 0, 0);
            gpuBulletHole.forward = new Vector3(0, 0, 0);
            gpuBulletHole.radius = new Vector2(0, 0);
        }

        bulletHoleBuffer.SetData(activeBulletHoles.ToArray());
        activeBulletHoles.Clear();
    }

    void ActivateBulletHole() {
        for (int i = 0; i < maxBulletHoles; ++i) {
            if (bulletHoles[i].active) continue;

            bulletHoles[i].active = true;
            bulletHoles[i].t = 0.0f;
            bulletHoles[i].origin = cam.transform.position;
            Vector3 offset = new Vector3(Random.value, Random.value, Random.value);
            offset = offset * 2 - new Vector3(1, 1, 1);
            offset *= 0.05f;
            bulletHoles[i].forward = cam.transform.forward + offset;
            bulletHoles[i].radius = new Vector2(GetRadius(r1, 0), GetRadius(r2, 0));

            return;
        }
    }

    void UpdateBulletHoles() {
        activeBulletHoles.Clear();

        for (int i = 0; i < maxBulletHoles; ++i) {
            if (!bulletHoles[i].active) continue;

            bulletHoles[i].t += Time.deltaTime * (Random.value * 0.8f + 0.1f);
            if (bulletHoles[i].t > 1) {
                bulletHoles[i].active = false;
                continue;
            }

            bulletHoles[i].radius = new Vector2(GetRadius(r1, bulletHoles[i].t), GetRadius(r2, bulletHoles[i].t));

            GPUBulletHole gpuBulletHole = new GPUBulletHole();
            gpuBulletHole.origin = bulletHoles[i].origin;
            gpuBulletHole.forward = bulletHoles[i].forward;
            gpuBulletHole.radius = bulletHoles[i].radius;

            activeBulletHoles.Add(gpuBulletHole);
        }

        activeBulletHoleCount = activeBulletHoles.Count;
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            ActivateBulletHole();
        }

        UpdateBulletHoles();

        if (activeBulletHoleCount > 0) bulletHoleBuffer.SetData(activeBulletHoles.ToArray(), 0, 0, activeBulletHoleCount);
    }

    void OnDisable() {
        bulletHoleBuffer.Release();
    }
}
