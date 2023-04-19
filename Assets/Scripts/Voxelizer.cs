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

    public Vector3 maxRadius = new Vector3(1, 1, 1);

    [Range(0.01f, 5.0f)]
    public float growthSpeed = 1.0f;

    private ComputeBuffer staticVoxelsBuffer, smokeVoxelsBuffer, argsBuffer;
    private ComputeShader voxelizeCompute;
    private Material debugVoxelMaterial;
    private Bounds debugBounds;
    private int voxelsX, voxelsY, voxelsZ, totalVoxels;
    private float radius;

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

        Mesh sharedMesh = objectsToVoxelize.GetComponent<MeshFilter>().sharedMesh;

        ComputeBuffer verticesBuffer = new ComputeBuffer(sharedMesh.vertexCount, 3 * sizeof(float));
        verticesBuffer.SetData(sharedMesh.vertices);
        ComputeBuffer trianglesBuffer = new ComputeBuffer(sharedMesh.triangles.Length, sizeof(int));
        trianglesBuffer.SetData(sharedMesh.triangles);

        voxelizeCompute.SetBuffer(0, "_StaticVoxels", staticVoxelsBuffer);
        voxelizeCompute.SetBuffer(0, "_MeshVertices", verticesBuffer);
        voxelizeCompute.SetBuffer(0, "_MeshTriangleIndices", trianglesBuffer);
        voxelizeCompute.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
        voxelizeCompute.SetVector("_BoundsExtent", boundsExtent);
        voxelizeCompute.SetMatrix("_MeshLocalToWorld", objectsToVoxelize.transform.localToWorldMatrix);
        voxelizeCompute.SetInt("_VoxelCount", totalVoxels);
        voxelizeCompute.SetInt("_TriangleCount", sharedMesh.triangles.Length);
        voxelizeCompute.SetFloat("_VoxelSize", voxelSize);
        voxelizeCompute.SetFloat("_IntersectionBias", intersectionBias);
        
        voxelizeCompute.Dispatch(0, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);

        verticesBuffer.Release();
        trianglesBuffer.Release();

        smokeVoxelsBuffer = new ComputeBuffer(totalVoxels, 4);
        voxelizeCompute.SetBuffer(1, "_Voxels", smokeVoxelsBuffer);
        voxelizeCompute.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
        voxelizeCompute.SetVector("_BoundsExtent", boundsExtent);
        voxelizeCompute.SetVector("_SmokeOrigin", new Vector3(0, 0, 0));
        voxelizeCompute.SetFloat("_Radius", radius);
        voxelizeCompute.SetInt("_VoxelCount", totalVoxels);

        voxelizeCompute.Dispatch(1, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);

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
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 50)) {
                voxelizeCompute.SetVector("_SmokeOrigin", hit.point);
                radius = 0;
            }
        }



        radius += growthSpeed * Time.deltaTime;

        voxelizeCompute.SetBuffer(1, "_Voxels", smokeVoxelsBuffer);
        voxelizeCompute.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
        voxelizeCompute.SetVector("_BoundsExtent", boundsExtent);
        voxelizeCompute.SetVector("_Radius", Vector3.Lerp(Vector3.zero, maxRadius, Easing(radius)));
        voxelizeCompute.SetInt("_VoxelCount", totalVoxels);
        voxelizeCompute.Dispatch(1, Mathf.CeilToInt(totalVoxels / 128.0f), 1, 1);

        if (debugStaticVoxels) {
            debugVoxelMaterial.SetBuffer("_Voxels", staticVoxelsBuffer);
            debugVoxelMaterial.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
            debugVoxelMaterial.SetVector("_BoundsExtent", boundsExtent);
            debugVoxelMaterial.SetFloat("_VoxelSize", voxelSize);

            Graphics.DrawMeshInstancedIndirect(debugMesh, 0, debugVoxelMaterial, debugBounds, argsBuffer);
        }

        if (debugSmokeVoxels) {
            debugVoxelMaterial.SetBuffer("_Voxels", smokeVoxelsBuffer);
            debugVoxelMaterial.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
            debugVoxelMaterial.SetVector("_BoundsExtent", boundsExtent);
            debugVoxelMaterial.SetFloat("_VoxelSize", voxelSize);

            Graphics.DrawMeshInstancedIndirect(debugMesh, 0, debugVoxelMaterial, debugBounds, argsBuffer);
        }
    }

    void OnDisable() {
        staticVoxelsBuffer.Release();
        smokeVoxelsBuffer.Release();
        argsBuffer.Release();
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(debugBounds.center, debugBounds.extents * 2);
    }
}
