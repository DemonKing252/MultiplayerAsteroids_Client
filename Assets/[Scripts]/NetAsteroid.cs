using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetAsteroid : MonoBehaviour
{
    public Vector2 Vel;
    public int NetID = -1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += new Vector3(Vel.x * Time.deltaTime, Vel.y * Time.deltaTime, 0f);
    }
}
