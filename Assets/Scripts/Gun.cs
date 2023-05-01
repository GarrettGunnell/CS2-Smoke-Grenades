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

    public float GetRadius1() {
        return r1;
    }

    public float GetRadius2() {
        return r2;
    }

    public float GetDepth() {
        return depth;
    }

    void Start() {
        
    }

    void Update() {
        
    }
}
