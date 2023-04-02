using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Raymarcher : MonoBehaviour {
    private Material raymarchMat;

    [Range(0.0f, 10.0f)]
    public float maxRadius = 3.0f;

    [Range(0.0f, 10.0f)]
    public float growthSpeed = 1.0f;

    public bool restartAnimation = false;


    private float radius = 0.0f;
    
    float Easing(float x) {
        return Mathf.Sin((radius * Mathf.PI) / 2);
    }

    void Start() {
        raymarchMat = new Material(Shader.Find("Hidden/Raymarch"));
        Debug.Log(this.transform.position.ToString());
    }

    void Update() {
        if (radius < 1.0f) {
            radius += growthSpeed * Time.deltaTime;
        }

        if (restartAnimation) {
            radius = 0.0f;
            restartAnimation = false;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        raymarchMat.SetFloat("_Radius", Mathf.Lerp(0.0f, maxRadius, Easing(radius)));
        Graphics.Blit(source, destination, raymarchMat);
    }
}
