using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

enum RTSize : int
{
    P128 = 128,
    P256 = 256,
    P512 = 512,
    P1024 = 1024,
    P2048 = 2048,
}

enum InputScale : int
{
    X2 = 2,
    X4 = 4,
    X8 = 8,
}

public class InterWaterGPU : MonoBehaviour,IInterWater
{
    public ComputeShader cs;

    private ComputeBuffer _cb;
    private RenderTexture _rt;
    private float[] _inputData;

    private Renderer _meshRenderer;
    private Material _material;
    
    [SerializeField]
    private RTSize rtWidth;
    [SerializeField]
    private RTSize rtHeight;

    private int _inputWidth;
    private int _inputHeight;
    private int _rtWidth;
    private int _rtHeight;
    private int _threadGroupX,_threadGroupY;

    private int _kernelName;

    [SerializeField]
    private InputScale inputScale = InputScale.X2;

    private bool _isDispatched = false;
    
    private const int BaseSize = 128;
    private void Awake()
    {
        Application.targetFrameRate = 60;
        if (Setup())
        {
            SetMaterial();
            SetCShader();
            _isDispatched = true;
        }
    }

    bool Setup()
    {
        _meshRenderer = GetComponent<Renderer>();
        if (_meshRenderer != null)
        {
            _material = _meshRenderer.sharedMaterial;
            if (_material == null)
            {
                return false;
            }
            
            InitData();
            
            return true;
        }
        return false;
    }

    void SetMaterial()
    {
        _material.SetTexture("_WaveTex",_rt);
    }

    void SetCShader()
    {
        _kernelName = cs.FindKernel("CSMain");
        cs.SetInt("rtWidth",_rtWidth);
        cs.SetInt("rtHeight",_rtHeight);
        cs.SetInt("inputWidth", _inputWidth);
        cs.SetInt("inputHeight",_inputHeight);
        cs.SetTexture(_kernelName,"Result",_rt);
        cs.GetKernelThreadGroupSizes(_kernelName,out var x,out var y,out _);
        _threadGroupX = (int)(_rtWidth / x);
        _threadGroupY = (int) (_rtHeight / y);
        loopIndex = 0;        
    }

    private int loopIndex = 0;
    private void Update()
    {
        if (_isDispatched)
        {
            if (_hasClick) {
                _hasClick = false;
                AddInterPoint(_hitPoint);
            }
            cs.SetInt("curIndex",loopIndex);
            _cb.SetData(_inputData);
            cs.SetBuffer(_kernelName,"Input",_cb);
            cs.Dispatch(_kernelName, _threadGroupX, _threadGroupY,1);
            ClearInputData();
            loopIndex++;
            if (loopIndex == 3)
            {
                loopIndex = 0;
            }
        }
    }

    void CreatRt()
    {
        _rtWidth = (int) rtWidth;
        _rtHeight = (int) rtHeight;
        _rt = new RenderTexture(_rtWidth,_rtHeight,0,RenderTextureFormat.ARGB32);		
        _rt.enableRandomWrite = true;
        _rt.Create();
        
        Graphics.SetRenderTarget(_rt);
        GL.Clear(false, true, new Color(0.5f,0.5f,0.5f,0.5f));
    }
    
    void InitData()
    {
        CreatRt();
        int scale = (int) inputScale;
        _inputWidth = _rtWidth / scale;
        _inputHeight = _rtHeight / scale;
        int inputSize = _inputWidth * _inputHeight;
        _inputData = new float[inputSize];
        for (int i = 0; i < inputSize; i++)
        {
            _inputData[i] = 0.5f;
        }
        
        _cb = new ComputeBuffer(inputSize,sizeof(float));
        _cb.SetData(_inputData);
    }
    
    
    [SerializeField]
    Vector2 meshSize;
    Vector3 _hitPoint;
    bool _hasClick;
    RaycastHit _hit;
    public void OnClickDown (Vector3 pos) {
        var ray = Camera.main.ScreenPointToRay (pos);
        if (Physics.Raycast (ray, out _hit)) {
            _hitPoint = _hit.point;
            _hasClick = true;
        }
    }

    private List<Vector3> posList = new List<Vector3>();
    public void AddHitPoint(Vector3 pos)
    {
        posList.Add(pos);
    }

    void AddInputData(int x, int y)
    {
        int index = x + y * _inputWidth;
        _inputData[index] = 1f;
    }

    void ClearInputData()
    {
        if (_inputData != null)
        {
            for (int i = 0; i < _inputData.Length; i++)
            {
                _inputData[i] = 0.5f;
            }
        }
    }
    
    public void AddInterPoint(Vector3 pos)
    {
        Vector3 off = pos - transform.position;
        off.x = Mathf.Clamp(off.x, -meshSize.x * 0.5f+1, meshSize.x * 0.5f-1);
        off.z = Mathf.Clamp(off.z, -meshSize.y * 0.5f+1, meshSize.y * 0.5f-1);
        
        Vector2 uv = new Vector2(off.x / meshSize.x + 0.5f, off.z / meshSize.y + 0.5f);
        int x = Mathf.RoundToInt(_inputWidth * uv.x);
        int y = Mathf.RoundToInt(_inputHeight * uv.y);
        
        AddInputData(x, y);
    }

    public Vector2 GetMeshSize()
    {
        return meshSize;
    }

    private void OnDisable()
    {
        if (_cb != null)
        {
            _cb.Release();
            _cb = null;
        }

        if (_rt != null)
        {
            _rt.Release();
            _rt = null;
        }
    }
}
