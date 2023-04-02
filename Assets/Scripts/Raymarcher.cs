using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Raymarcher : MonoBehaviour {
    private ComputeShader raymarchCompute;

    [Range(0.0f, 10.0f)]
    public float maxRadius = 3.0f;

    [Range(0.0f, 10.0f)]
    public float growthSpeed = 1.0f;

    public bool restartAnimation = false;

    private GameObject sun;
    private Camera cam;

    private float radius = 0.0f;

    private int raymarchSmokePass, compositePass;
    
    private RenderTexture smokeTex, outputTex;

    float Easing(float x) {
        return Mathf.Sin((radius * Mathf.PI) / 2);
    }

    void OnEnable() {
        raymarchCompute = (ComputeShader)Resources.Load("RenderSmoke");

        raymarchSmokePass = raymarchCompute.FindKernel("CS_RayMarchSmoke");
        compositePass = raymarchCompute.FindKernel("CS_Composite");

        smokeTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        smokeTex.enableRandomWrite = true;
        smokeTex.Create();

        outputTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        outputTex.enableRandomWrite = true;
        outputTex.Create();

        cam = GetComponent<Camera>();
        sun = GameObject.Find("Directional Light");
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
        raymarchCompute.SetVector("_CameraWorldPos", this.transform.position);
        raymarchCompute.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);
        raymarchCompute.SetMatrix("_CameraInvProjection", cam.projectionMatrix.inverse);
        raymarchCompute.SetFloat("_BufferWidth", Screen.width);
        raymarchCompute.SetFloat("_BufferHeight", Screen.height);
        raymarchCompute.SetVector("_SunDirection", sun.transform.forward);

        raymarchCompute.SetFloat("_Radius", Mathf.Lerp(0.0f, maxRadius, Easing(radius)));

        raymarchCompute.SetTexture(raymarchSmokePass, "_Smoke", smokeTex);

        raymarchCompute.Dispatch(raymarchSmokePass, Mathf.CeilToInt(Screen.width / 8.0f), Mathf.CeilToInt(Screen.height / 8.0f), 1);

        Graphics.Blit(source, outputTex);
        raymarchCompute.SetTexture(compositePass, "_ColorBuffer", outputTex);
        raymarchCompute.SetTexture(compositePass, "_Smoke", smokeTex);
        raymarchCompute.Dispatch(compositePass, Mathf.CeilToInt(Screen.width / 8.0f), Mathf.CeilToInt(Screen.height / 8.0f), 1);

        Graphics.Blit(outputTex, destination);
    }
}
