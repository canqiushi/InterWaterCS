using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeDown : MonoBehaviour
{
    private float g = 9.8f;

    private float speed = 0;

    private float timer = 0;

    private IInterWater _interWater;

    public void Init(IInterWater wave)
    {
        _interWater = wave;
        speed = 200;
    }

	// Update is called once per frame
	void Update ()
    {
        timer += Time.smoothDeltaTime;
        float v = speed + g * timer;
        
        transform.Translate(Vector3.down * Time.smoothDeltaTime * v);

        if (transform.position.y <= 0)
        {
            //mWave.AddHitPoint(transform.position);
            _interWater.AddInterPoint(transform.position);
            Destroy(gameObject);
        }
    }
}
