using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxelizer : MonoBehaviour {
    public Vector3 boundsExtent = new Vector3(3, 3, 3);

    public float voxelSize = 0.25f;

    public GameObject objectsToVoxelize = null;

    [Range(0.0f, 2.0f)]
    public float intersectionBias = 1.0f;

    public Mesh debugMesh;

    public bool debugStaticVoxels = false;
    public bool debugSmokeVoxels = false;

    public Vector4 maxRadius = new Vector4(1, 1, 1, 1);

    [Range(0.01f, 5.0f)]
    public float growthSpeed = 1.0f;

    [Range(0, 128)]
    public int maxFillSteps = 16;

    public bool iterateFill = false;
    public bool constantFill = false;

    private ComputeBuffer staticVoxelsBuffer, smokeVoxelsBuffer, smokePingVoxelsBuffer, argsBuffer;
    private ComputeShader voxelizeCompute;
    private Material debugVoxelMaterial;
    private Bounds debugBounds;
    private int voxelsX, voxelsY, voxelsZ, totalVoxels;
    private float radius;

    public ComputeBuffer GetSmokeVoxelBuffer() {
        return smokeVoxelsBuffer;
    }

    public Vector3 GetVoxelResolution() {
        return new Vector3(voxelsX, voxelsY, voxelsZ);
    }

    public Vector3 GetBoundsExtent() {
        return boundsExtent;
    }

    public float GetVoxelSize() {
        return voxelSize;
    }

    void OnEnable() {
        radius = 0.0f;
        debugVoxelMaterial = new Material(Shader.Find("Hidden/VisualizeVoxels"));
        voxelizeCompute = (ComputeShader)Resources.Load("Voxelize");

        Vector3 boundsSize = boundsExtent * 2;
        debugBounds = new Bounds(new Vector3(0, boundsExtent.y, 0), boundsSize);

        voxelsX = Mathf.CeilToInt(boundsSize.x / voxelSize);
        voxelsY = Mathf.CeilToInt(boundsSize.y / voxelSize);
        voxelsZ = Mathf.CeilToInt(boundsSize.z / voxelSize);
        totalVoxels = voxelsX * voxelsY * voxelsZ;

        staticVoxelsBuffer = new ComputeBuffer(totalVoxels, 4);

        // Clear buffer
        voxelizeCompute.SetBuffer(0, "_Voxels", staticVoxelsBuffer);
        voxelizeCompute.Dispatch(0, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);

        // Precompute voxelized representation of the scene
        ComputeBuffer verticesBuffer, trianglesBuffer;
        foreach (Transform child in objectsToVoxelize.GetComponentsInChildren<Transform>()) {
            MeshFilter meshFilter = child.gameObject.GetComponent<MeshFilter>();

            if (!meshFilter) continue;
            Mesh sharedMesh = meshFilter.sharedMesh;

            verticesBuffer = new ComputeBuffer(sharedMesh.vertexCount, 3 * sizeof(float));
            verticesBuffer.SetData(sharedMesh.vertices);
            trianglesBuffer = new ComputeBuffer(sharedMesh.triangles.Length, sizeof(int));
            trianglesBuffer.SetData(sharedMesh.triangles);

            voxelizeCompute.SetBuffer(1, "_StaticVoxels", staticVoxelsBuffer);
            voxelizeCompute.SetBuffer(1, "_MeshVertices", verticesBuffer);
            voxelizeCompute.SetBuffer(1, "_MeshTriangleIndices", trianglesBuffer);
            voxelizeCompute.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
            voxelizeCompute.SetVector("_BoundsExtent", boundsExtent);
            voxelizeCompute.SetMatrix("_MeshLocalToWorld", child.localToWorldMatrix);
            voxelizeCompute.SetInt("_VoxelCount", totalVoxels);
            voxelizeCompute.SetInt("_TriangleCount", sharedMesh.triangles.Length);
            voxelizeCompute.SetFloat("_VoxelSize", voxelSize);
            voxelizeCompute.SetFloat("_IntersectionBias", intersectionBias);
            
            voxelizeCompute.Dispatch(1, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);

            verticesBuffer.Release();
            trianglesBuffer.Release();
        }

        smokeVoxelsBuffer = new ComputeBuffer(totalVoxels, 4);
        smokePingVoxelsBuffer = new ComputeBuffer(totalVoxels, 4);
        
        // Clear buffers
        voxelizeCompute.SetBuffer(0, "_Voxels", smokeVoxelsBuffer);
        voxelizeCompute.Dispatch(0, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);
        voxelizeCompute.SetBuffer(0, "_Voxels", smokePingVoxelsBuffer);
        voxelizeCompute.Dispatch(0, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);
        
        voxelizeCompute.SetBuffer(2, "_SmokeVoxels", smokeVoxelsBuffer);

        voxelizeCompute.SetBuffer(3, "_StaticVoxels", staticVoxelsBuffer);
        voxelizeCompute.SetBuffer(3, "_SmokeVoxels", smokeVoxelsBuffer);
        voxelizeCompute.SetBuffer(3, "_PingVoxels", smokePingVoxelsBuffer);
        
        voxelizeCompute.SetBuffer(4, "_Voxels", smokeVoxelsBuffer);
        voxelizeCompute.SetBuffer(4, "_PingVoxels", smokePingVoxelsBuffer);

        // Debug instancing args
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)debugMesh.GetIndexCount(0);
        args[1] = (uint)totalVoxels;
        args[2] = (uint)debugMesh.GetIndexStart(0);
        args[3] = (uint)debugMesh.GetBaseVertex(0);
        argsBuffer.SetData(args);
    }

    float Easing(float x) {
        return 1 - 1 / (2 * x * x * x + 1);
    }

    void Update() {
        voxelizeCompute.SetInt("_MaxFillSteps", maxFillSteps);
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 50)) {
                voxelizeCompute.SetVector("_SmokeOrigin", hit.point);
                
                radius = 0;
                voxelizeCompute.SetBuffer(0, "_Voxels", smokeVoxelsBuffer);
                voxelizeCompute.Dispatch(0, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);

                voxelizeCompute.Dispatch(2, 1, 1, 1);
            }
        }

        if (iterateFill || constantFill) {
            voxelizeCompute.SetVector("_Radius", Vector4.Lerp(Vector4.zero, maxRadius, Easing(radius)));

            voxelizeCompute.Dispatch(3, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);
            voxelizeCompute.Dispatch(4, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);

            iterateFill = false;
            radius += growthSpeed * Time.deltaTime;
        }

        if (debugStaticVoxels) {
            debugVoxelMaterial.SetBuffer("_Voxels", staticVoxelsBuffer);
            debugVoxelMaterial.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
            debugVoxelMaterial.SetVector("_BoundsExtent", boundsExtent);
            debugVoxelMaterial.SetFloat("_VoxelSize", voxelSize);
            debugVoxelMaterial.SetInt("_MaxFillSteps", maxFillSteps);

            Graphics.DrawMeshInstancedIndirect(debugMesh, 0, debugVoxelMaterial, debugBounds, argsBuffer);
        }

        if (debugSmokeVoxels) {
            debugVoxelMaterial.SetBuffer("_Voxels", smokeVoxelsBuffer);
            debugVoxelMaterial.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
            debugVoxelMaterial.SetVector("_BoundsExtent", boundsExtent);
            debugVoxelMaterial.SetFloat("_VoxelSize", voxelSize);
            debugVoxelMaterial.SetInt("_MaxFillSteps", maxFillSteps);

            Graphics.DrawMeshInstancedIndirect(debugMesh, 0, debugVoxelMaterial, debugBounds, argsBuffer);
        }
    }

    void OnDisable() {
        staticVoxelsBuffer.Release();
        smokeVoxelsBuffer.Release();
        smokePingVoxelsBuffer.Release();
        argsBuffer.Release();
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(debugBounds.center, debugBounds.extents * 2);
    }
}
