using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Raymarcher : MonoBehaviour {
    public Voxelizer smokeVoxelData = null;
    public Gun gun = null;

    public enum Res {
        FullResolution = 0,
        HalfResolution,
        QuarterResolution
    } public Res resolutionScale;

    [Header("Noise Settings")]
    [Space(5)]
    [Range(0, 100000)]
    public int seed = 0;

    [Range(1, 16)]
    public int octaves = 1;

    [Range(1, 128)]
    public int cellSize = 16;

    [Range(1, 64)]
    public int axisCellCount = 4;

    [Range(0.1f, 16.0f)]
    public float amplitude = 1.0f;
    
    [Range(0.0f, 5.0f)]
    public float warp = 0.0f;

    [Range(-5.0f, 5.0f)]
    public float add = 0.0f;

    public bool invertNoise = false;

    public bool updateNoise = false;

    public bool debugNoise = false;

    public bool debugTiledNoise = false;

    public enum DebugAxis {
        X = 0,
        Y,
        Z
    } public DebugAxis debugNoiseAxis;
    
    [Range(0, 128)]
    public int debugNoiseSlice = 0;

    [Header("SDF Settings")]
    [Space(5)]
    public Vector4 cubeParams = new Vector4(0, 0, 0, 1);

    [Header("Smoke Settings")]
    [Space(5)]
    [ColorUsageAttribute(false, true)]
    public Color lightColor;

    public Color smokeColor;

    [Range(1, 256)]
    public int stepCount = 64;

    [Range(0.01f, 0.1f)]
    public float stepSize = 0.05f;

    [Range(1, 32)]
    public int lightStepCount = 8;

    [Range(0.01f, 1.0f)]
    public float lightStepSize = 0.25f;

    [Range(0.01f, 64.0f)]
    public float smokeSize = 32.0f;

    [Range(0.0f, 10.0f)]
    public float volumeDensity = 1.0f;

    [Range(0.0f, 3.0f)]
    public float absorptionCoefficient = 0.5f;

    [Range(0.0f, 3.0f)]
    public float scatteringCoefficient = 0.5f;

    public Color extinctionColor = new Color(1, 1, 1);

    [Range(0.0f, 10.0f)]
    public float shadowDensity = 1.0f;

    public enum PhaseFunction {
        HenyeyGreenstein = 0,
        Mie,
        Rayleigh
    } public PhaseFunction phaseFunction;

    [Range(-1.0f, 1.0f)]
    public float scatteringAnisotropy = 0.0f;
    
    [Range(0.0f, 1.0f)]
    public float densityFalloff = 0.25f;

    [Range(0.0f, 1.0f)]
    public float alphaThreshold = 0.1f;

    [Header("Animation Settings")]
    [Space(5)]
    public Vector3 animationDirection = new Vector3(0, -0.1f, 0);

    [Header("Composite Settings")]
    [Space(5)]
    public bool bicubicUpscale = true;

    [Range(-1.0f, 1.0f)]
    public float sharpness = 0.0f;

    public enum ViewTexture {
        Composite = 0,
        SmokeAlbedo,
        SmokeMask,
        PolygonalDepth
    } public ViewTexture debugView;

    private GameObject sun;
    private Camera cam;

    private Material compositeMaterial;
    private ComputeShader raymarchCompute;

    private int generateNoisePass, debugNoisePass, raymarchSmokePass;
    
    private RenderTexture noiseTex, depthTex;
    private RenderTexture smokeAlbedoFullTex, smokeAlbedoHalfTex, smokeAlbedoQuarterTex;
    private RenderTexture smokeMaskFullTex, smokeMaskHalfTex, smokeMaskQuarterTex;

    private ComputeBuffer smokeVoxelBuffer;

    void UpdateNoise() {
        raymarchCompute.SetTexture(generateNoisePass, "_RWNoiseTex", noiseTex);
        raymarchCompute.SetInt("_Octaves", octaves);
        raymarchCompute.SetInt("_CellSize", cellSize);
        raymarchCompute.SetInt("_AxisCellCount", axisCellCount);
        raymarchCompute.SetFloat("_Amplitude", amplitude);
        raymarchCompute.SetFloat("_Warp", warp);
        raymarchCompute.SetFloat("_Add", add);
        raymarchCompute.SetInt("_InvertNoise", invertNoise ? 1 : 0);
        raymarchCompute.SetInt("_Seed", seed);
        raymarchCompute.SetVector("_NoiseRes", new Vector4(128, 128, 128, 0));

        // 128 / 8
        raymarchCompute.Dispatch(generateNoisePass, 16, 16, 16);

        raymarchCompute.SetTexture(raymarchSmokePass, "_NoiseTex", noiseTex);
    }

    void InitializeNoise() {
        if (noiseTex != null) {
            UpdateNoise();
            return;
        }
        
        noiseTex = new RenderTexture(128, 128, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
        noiseTex.enableRandomWrite = true;
        noiseTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        noiseTex.volumeDepth = 128;
        noiseTex.Create();

        UpdateNoise();
    }

    void InitializeVariables() {
        compositeMaterial = new Material(Shader.Find("Hidden/CompositeEffects"));
        raymarchCompute = (ComputeShader)Resources.Load("RenderSmoke");

        generateNoisePass = raymarchCompute.FindKernel("CS_GenerateNoise");
        debugNoisePass = raymarchCompute.FindKernel("CS_DebugNoise");
        raymarchSmokePass = raymarchCompute.FindKernel("CS_RayMarchSmoke");

        InitializeNoise();

        smokeAlbedoFullTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        smokeAlbedoFullTex.enableRandomWrite = true;
        smokeAlbedoFullTex.Create();

        smokeAlbedoHalfTex = new RenderTexture(Mathf.CeilToInt(Screen.width / 2), Mathf.CeilToInt(Screen.height / 2), 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        smokeAlbedoHalfTex.enableRandomWrite = true;
        smokeAlbedoHalfTex.Create();

        smokeAlbedoQuarterTex = new RenderTexture(Mathf.CeilToInt(Screen.width / 4), Mathf.CeilToInt(Screen.height / 4), 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        smokeAlbedoQuarterTex.enableRandomWrite = true;
        smokeAlbedoQuarterTex.Create();

        smokeMaskFullTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        smokeMaskFullTex.enableRandomWrite = true;
        smokeMaskFullTex.Create();

        smokeMaskHalfTex = new RenderTexture(Mathf.CeilToInt(Screen.width / 2), Mathf.CeilToInt(Screen.height / 2), 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        smokeMaskHalfTex.enableRandomWrite = true;
        smokeMaskHalfTex.Create();

        smokeMaskQuarterTex = new RenderTexture(Mathf.CeilToInt(Screen.width / 4), Mathf.CeilToInt(Screen.height / 4), 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        smokeMaskQuarterTex.enableRandomWrite = true;
        smokeMaskQuarterTex.Create();
        
        depthTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
        depthTex.enableRandomWrite = true;
        depthTex.Create();

        cam = GetComponent<Camera>();
        sun = GameObject.Find("Directional Light");
    }

    void OnEnable() {
        InitializeVariables();
    }

    void Update() {
        if (updateNoise) {
            UpdateNoise();
        }

        if (smokeVoxelData != null) {
            smokeVoxelBuffer = smokeVoxelData.GetSmokeVoxelBuffer(); 
            raymarchCompute.SetBuffer(2, "_SmokeVoxels", smokeVoxelBuffer);
            raymarchCompute.SetVector("_BoundsExtent", smokeVoxelData.GetBoundsExtent());
            raymarchCompute.SetVector("_VoxelResolution", smokeVoxelData.GetVoxelResolution());
        }

        if (gun != null) {
            raymarchCompute.SetBuffer(2, "_BulletHoles", gun.GetBulletHoles());
            raymarchCompute.SetFloat("_BulletDepth", gun.GetDepth());
            raymarchCompute.SetInt("_BulletHoleCount", gun.GetActiveBulletHoleCount());
        }
    }

    private RenderTexture GetSmokeAlbedoTex() {
        switch((int)resolutionScale) {
            case 0:
                return smokeAlbedoFullTex;
            case 1:
                return smokeAlbedoHalfTex;
            case 2:
                return smokeAlbedoQuarterTex;
        }

        return smokeAlbedoFullTex;
    }

    private RenderTexture GetSmokeMaskTex() {
        switch((int)resolutionScale) {
            case 0:
                return smokeMaskFullTex;
            case 1:
                return smokeMaskHalfTex;
            case 2:
                return smokeMaskQuarterTex;
        }

        return smokeMaskFullTex;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        RenderTexture smokeTex = GetSmokeAlbedoTex();
        RenderTexture smokeMaskTex = GetSmokeMaskTex();

        //Create depth tex for compute shader
        Graphics.Blit(source, depthTex, compositeMaterial, 0);

        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
        Matrix4x4 viewProjMatrix = projMatrix * cam.worldToCameraMatrix;
        raymarchCompute.SetVector("_CameraForward", cam.transform.forward);


        raymarchCompute.SetVector("_CameraWorldPos", this.transform.position);
        raymarchCompute.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);
        raymarchCompute.SetMatrix("_CameraInvProjection", projMatrix.inverse);
        raymarchCompute.SetMatrix("_CameraInvViewProjection", viewProjMatrix.inverse);
        raymarchCompute.SetInt("_BufferWidth", smokeTex.width);
        raymarchCompute.SetInt("_BufferHeight", smokeTex.height);
        raymarchCompute.SetInt("_StepCount", stepCount);
        raymarchCompute.SetInt("_LightStepCount", lightStepCount);
        raymarchCompute.SetFloat("_SmokeSize", smokeSize);
        raymarchCompute.SetFloat("_FrameTime", Time.time);
        raymarchCompute.SetFloat("_AbsorptionCoefficient", absorptionCoefficient);
        raymarchCompute.SetFloat("_ScatteringCoefficient", scatteringCoefficient);
        raymarchCompute.SetFloat("_DensityFalloff", 1 - densityFalloff);
        raymarchCompute.SetFloat("_VolumeDensity", volumeDensity * stepSize);
        raymarchCompute.SetFloat("_StepSize", stepSize);
        raymarchCompute.SetFloat("_ShadowDensity", shadowDensity * lightStepSize);
        raymarchCompute.SetFloat("_LightStepSize", lightStepSize);
        raymarchCompute.SetFloat("_G", scatteringAnisotropy);
        raymarchCompute.SetVector("_SunDirection", sun.transform.forward);
        raymarchCompute.SetVector("_AnimationDirection", animationDirection);
        raymarchCompute.SetInt("_PhaseFunction", (int)phaseFunction);
        raymarchCompute.SetVector("_CubeParams", cubeParams);
        raymarchCompute.SetVector("_LightColor", lightColor);
        raymarchCompute.SetVector("_SmokeColor", smokeColor);
        raymarchCompute.SetVector("_ExtinctionColor", extinctionColor);
        raymarchCompute.SetVector("_Radius", smokeVoxelData.GetSmokeRadius());
        raymarchCompute.SetVector("_SmokeOrigin", smokeVoxelData.GetSmokeOrigin());
        raymarchCompute.SetFloat("_AlphaThreshold", alphaThreshold);

        if (debugNoise) {
            raymarchCompute.SetTexture(debugNoisePass, "_NoiseTex", noiseTex);
            raymarchCompute.SetTexture(debugNoisePass, "_SmokeTex", smokeTex);
            raymarchCompute.SetInt("_DebugNoiseSlice", debugNoiseSlice);
            raymarchCompute.SetInt("_DebugAxis", (int)debugNoiseAxis);
            raymarchCompute.SetInt("_DebugTiledNoise", debugTiledNoise ? 1 : 0);
            raymarchCompute.SetVector("_NoiseRes", new Vector4(128, 128, 128, 0));

            raymarchCompute.Dispatch(debugNoisePass, Mathf.CeilToInt(Screen.width / 8.0f), Mathf.CeilToInt(Screen.height / 8.0f), 1);

            Graphics.Blit(smokeTex, destination);
            return;
        }
        
        // Render volumes
        raymarchCompute.SetTexture(raymarchSmokePass, "_SmokeTex", smokeTex);
        raymarchCompute.SetTexture(raymarchSmokePass, "_SmokeMaskTex", smokeMaskTex);
        raymarchCompute.SetTexture(raymarchSmokePass, "_NoiseTex", noiseTex);
        raymarchCompute.SetTexture(raymarchSmokePass, "_DepthTex", depthTex);
        raymarchCompute.Dispatch(raymarchSmokePass, Mathf.CeilToInt(smokeTex.width / 8.0f), Mathf.CeilToInt(smokeTex.height / 8.0f), 1);
        
        if (resolutionScale == Res.HalfResolution) {
            Graphics.Blit(smokeMaskHalfTex, smokeMaskFullTex);
            Graphics.Blit(smokeMaskFullTex, smokeMaskHalfTex);

            if (bicubicUpscale) {
                Graphics.Blit(smokeAlbedoHalfTex, smokeAlbedoFullTex, compositeMaterial, 1);
            } else {
                Graphics.Blit(smokeAlbedoHalfTex, smokeAlbedoFullTex);
            }
        }

        if (resolutionScale == Res.QuarterResolution) {
            Graphics.Blit(smokeMaskQuarterTex, smokeMaskHalfTex);
            Graphics.Blit(smokeMaskHalfTex, smokeMaskFullTex);
            Graphics.Blit(smokeMaskFullTex, smokeMaskHalfTex);
            Graphics.Blit(smokeMaskHalfTex, smokeMaskQuarterTex);

            if (bicubicUpscale) {
                Graphics.Blit(smokeAlbedoQuarterTex, smokeAlbedoHalfTex, compositeMaterial, 1);
                Graphics.Blit(smokeAlbedoHalfTex, smokeAlbedoFullTex, compositeMaterial, 1);
            } else {
                Graphics.Blit(smokeAlbedoQuarterTex, smokeAlbedoHalfTex);
                Graphics.Blit(smokeAlbedoHalfTex, smokeAlbedoFullTex);
            }
        }

        // Composite volumes with source buffer
        compositeMaterial.SetTexture("_SmokeTex", smokeAlbedoFullTex);
        compositeMaterial.SetTexture("_SmokeMaskTex", smokeMaskTex);
        compositeMaterial.SetTexture("_DepthTex", depthTex);
        compositeMaterial.SetFloat("_Sharpness", sharpness);
        compositeMaterial.SetFloat("_DebugView", (int)debugView);

        Graphics.Blit(source, destination, compositeMaterial, 2);
    }
}
