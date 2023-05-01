using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour {
    [Range(-2.0f, 2.0f)]
    public float r1 = 0.0f;
    
    [Range(-2.0f, 2.0f)]
    public float r2 = 0.0f;

    [Range(0.0f, 30.0f)]
    public float depth = 15.0f;

    private float radius = 0.0f;
    private Vector3 bulletOrigin = new Vector3(0, 0, 0);
    private Vector3 bulletForward = new Vector3(0, 0, 0);
    private Camera cam;

    public float GetRadius1() {
        return Mathf.Lerp(-2.0f, r1, Easing(radius));
    }

    public float GetRadius2() {
        return Mathf.Lerp(-2.0f, r2, Easing(radius));
    }

    public float GetDepth() {
        return depth;
    }

    public Vector3 GetBulletOrigin() {
        return bulletOrigin;
    }

    public Vector3 GetBulletForward() {
        return bulletForward;
    }

    float Easing(float x) {
        return 1 - x * x * x;
    }

    void OnEnable() {
        cam = Camera.main;
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            radius = 0.0f;
            bulletOrigin = cam.transform.position;
            bulletForward = cam.transform.forward;
        }

        if (radius <= 1)
            radius += Time.deltaTime;
    }
}
