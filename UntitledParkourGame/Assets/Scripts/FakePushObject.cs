using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class FakePushObject : MonoBehaviour
{
    // Start is called before the first frame update
    //public NetworkVariable<Vector3> direction = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    //public NetworkVariable<float> distance = new NetworkVariable<float>();
    public float distance;
    //public NetworkVariable<bool> isActive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    //public NetworkVariable<bool> isInfected = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    //public NetworkVariable<ulong> id = new NetworkVariable<ulong>();
    //public float persistTime;
    //public float hitboxActiveTime;
    public float speed;

    public LayerMask collisionMask;
    
    //private float activeTimer;
    //private float persistTimer;
    private Vector3 startMarker;
    private Vector3 endMarker;
    //private float startTime;
    //private float journeyLength;
    Rigidbody rb;
    void Start()
    {

        //activeTimer = 0; 
        //persistTimer = 0;
        //isActive.Value = true;
        startMarker= transform.position;
        //endMarker = startMarker + transform.rotation.eulerAngles * distance;
        //journeyLength = (endMarker- startMarker).magnitude;
        //startTime = Time.time;
        rb = GetComponent<Rigidbody>();
        rb.velocity = this.transform.forward*speed;
    }

    // Update is called once per frame
    void Update()
    {
        if ((transform.position-startMarker).magnitude >= distance)
        {
            Invoke(nameof(DestroyHelper), 0.2f);
            rb.velocity = Vector3.zero;
        }

        if(rb.velocity == Vector3.zero)
        {
            Invoke(nameof(DestroyHelper), 0.2f);
        }
        /*
        if(activeTimer < hitboxActiveTime)
        {
            activeTimer += Time.deltaTime;
        } else
        {
            isActive.Value = false;
        }
        */
        // Distance moved equals elapsed time times speed..
       
    }

    private void DestroyHelper()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("OnTriggerEnter");
        GameObject go = other.gameObject;   
        if(collisionMask == (collisionMask | (1 << go.layer)))
        {
            //Debug.Log("Destroying");
            rb.velocity = Vector3.zero;
            Destroy(gameObject);
        }
        
    }
    
    /*
    private void FixedUpdate()
    {
        float distCovered = (Time.time - startTime) * speed;

        // Fraction of journey completed equals current distance divided by total distance.
        float fractionOfJourney = distCovered / journeyLength;

        // Set our position as a fraction of the distance between the markers.
        rb.position = Vector3.Lerp(startMarker, endMarker, fractionOfJourney);
    }
    */
}
