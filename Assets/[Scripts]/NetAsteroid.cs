using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetAsteroid : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 vel;
    public Vector2 Vel { get { return vel; } set { vel = value; rb.velocity = value; } }
    public int NetID = -1;
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position += new Vector3(Vel.x * Time.deltaTime, Vel.y * Time.deltaTime, 0f);
    }
}
