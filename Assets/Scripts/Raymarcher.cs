using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Raymarcher : MonoBehaviour {

    public Vector4 cubeParams = new Vector4(0, 0, 0, 1);

    [Range(0.0f, 10.0f)]
    public float maxRadius = 3.0f;

    [Range(0.0f, 10.0f)]
    public float growthSpeed = 1.0f;

    public bool restartAnimation = false;

    public enum ViewTexture {
        Composite = 0,
        SmokeAlbedo,
        SmokeMask,
        SmokeDepth,
        PolygonalDepth
    } public ViewTexture debugView;

    private GameObject sun;
    private Camera cam;

    private float radius = 0.0f;

    private Material compositeMaterial;
    private ComputeShader raymarchCompute;

    private int raymarchSmokePass;
    
    private RenderTexture smokeMaskTex, smokeTex, smokeDepthTex;

    float Easing(float x) {
        return Mathf.Sin((radius * Mathf.PI) / 2);
    }

    void OnEnable() {
        compositeMaterial = new Material(Shader.Find("Hidden/CompositeEffects"));
        raymarchCompute = (ComputeShader)Resources.Load("RenderSmoke");

        raymarchSmokePass = raymarchCompute.FindKernel("CS_RayMarchSmoke");

        smokeTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        smokeTex.enableRandomWrite = true;
        smokeTex.Create();

        smokeDepthTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
        smokeDepthTex.enableRandomWrite = true;
        smokeDepthTex.Create();

        smokeMaskTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        smokeMaskTex.enableRandomWrite = true;
        smokeMaskTex.Create();

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
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);

        raymarchCompute.SetVector("_CameraWorldPos", this.transform.position);
        raymarchCompute.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);
        raymarchCompute.SetMatrix("_CameraInvProjection", projMatrix.inverse);
        raymarchCompute.SetMatrix("_CameraViewProjection", projMatrix * cam.worldToCameraMatrix);
        raymarchCompute.SetFloat("_BufferWidth", Screen.width);
        raymarchCompute.SetFloat("_BufferHeight", Screen.height);
        raymarchCompute.SetVector("_SunDirection", sun.transform.forward);
        raymarchCompute.SetFloat("_Radius", Mathf.Lerp(0.0f, maxRadius, Easing(radius)));
        raymarchCompute.SetVector("_CubeParams", cubeParams);
        
        // Render volumes
        raymarchCompute.SetTexture(raymarchSmokePass, "_SmokeTex", smokeTex);
        raymarchCompute.SetTexture(raymarchSmokePass, "_SmokeDepthTex", smokeDepthTex);
        raymarchCompute.SetTexture(raymarchSmokePass, "_SmokeMaskTex", smokeMaskTex);
        raymarchCompute.Dispatch(raymarchSmokePass, Mathf.CeilToInt(Screen.width / 8.0f), Mathf.CeilToInt(Screen.height / 8.0f), 1);

        // Composite volumes with source buffer
        compositeMaterial.SetTexture("_SmokeTex", smokeTex);
        compositeMaterial.SetTexture("_SmokeDepthTex", smokeDepthTex);
        compositeMaterial.SetTexture("_SmokeMaskTex", smokeMaskTex);
        compositeMaterial.SetFloat("_DebugView", (int)debugView);

        Graphics.Blit(source, destination, compositeMaterial);
    }
}
