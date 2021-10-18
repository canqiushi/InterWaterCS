using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Rain : MonoBehaviour
{
    public GameObject Water;
    public GameObject raindrop;
    public float CountPerSecond;

    private IInterWater _interWater;
    private Vector2 size;
    private float rate;
    
    void Start()
    {
        _interWater = Water.GetComponent<IInterWater>();
        size = _interWater.GetMeshSize();
        size *= 0.5f;
        timer = 0;
        rate = 1f / CountPerSecond;
        if (rate < Time.smoothDeltaTime)
        {
            rate = Time.smoothDeltaTime;
        }
    }

    private float timer = 0;
    void Update()
    {
        timer += Time.smoothDeltaTime;

        rate = 1f / CountPerSecond;
        if (rate < Time.smoothDeltaTime)
        {
            rate = Time.smoothDeltaTime;
        }

        if (timer > rate)
        {
            float x = Random.Range(-size.x, size.x);
            float z = Random.Range(-size.y, size.y);

            var obj = Instantiate(raindrop, new Vector3(x, 150, z), Quaternion.identity, transform);
            var freeDown = obj.AddComponent<FreeDown>();
            freeDown.Init(_interWater);
            timer = 0;
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20,100,1000,200));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Density",GUILayout.Width(70));
        CountPerSecond = GUILayout.HorizontalSlider(CountPerSecond, 0, 60,GUILayout.Width(400),GUILayout.Height(50));
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
