using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public NetworkManager netman;
    public int NetIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        netman = FindObjectOfType<NetworkManager>();

    }
    // Update is called once per frame
    void Update()
    {
        if (netman.NetworkID == NetIndex)
        {
            float horiz = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            transform.Translate(new Vector3(horiz * 3f * Time.deltaTime, vertical * 3f * Time.deltaTime, 0f));

            netman.UpdateClient(transform.position);
        }
    }
}
