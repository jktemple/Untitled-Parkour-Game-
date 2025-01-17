using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using FMOD.Studio;

public class PushObject : NetworkBehaviour
{
    // Start is called before the first frame update
    //public NetworkVariable<Vector3> direction = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    //public NetworkVariable<float> distance = new NetworkVariable<float>();
    public float distance;
    //public NetworkVariable<bool> isActive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isInfected = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> id = new NetworkVariable<ulong>();
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
    float startTime;

    //Audio Instances
    private EventInstance playerShovingFailedsfx;
    private EventInstance playerShovingSuccesssfx;


    public override void OnNetworkSpawn()
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
        /*
        if (id.Value != NetworkManager.LocalClientId && IsClient)
        {
            this.gameObject.layer = LayerMask.NameToLayer("Default");
        }
        else if (IsClient && id.Value == )
        {
            this.gameObject.layer = LayerMask.NameToLayer("invisible");
        }*/
        startTime = Time.time;
    }
    private void Start()
    {
        /*
        if (id.Value != NetworkManager.LocalClientId && IsClient)
        {
            this.gameObject.layer = LayerMask.NameToLayer("Default");
        }
        else if (IsClient)
        {
            this.gameObject.layer = LayerMask.NameToLayer("invisible");
        }
        */

        //Audio Instances Create
        playerShovingFailedsfx = AudioManager.instance.CreateInstance(FMODEvents.instance.playerShovingFailedsfx);
        playerShovingSuccesssfx = AudioManager.instance.CreateInstance(FMODEvents.instance.playerShovingSuccesssfx);
    }

    // void start(){
    //     playerShovingFailedsfx = AudioManager.instance.CreateInstance(FMODEvents.instance.playerShovingFailedsfx);
    // }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Void Updated: Push Object ID = " + id.Value + " LocalClientId =  " + NetworkManager.LocalClientId);
        
        if (id.Value != NetworkManager.LocalClientId && IsClient /*&& (Time.time - startTime) > 0.05f*/)
        {
            this.gameObject.layer = LayerMask.NameToLayer("Default");
        } 
        
        if (!IsServer) return;
        if ((transform.position-startMarker).magnitude >= distance)
        {
            Invoke(nameof(DestroyHelper), 0.2f);
            rb.velocity = Vector3.zero;
            updateFailedSound();
        }
        

        if(rb.velocity == Vector3.zero)
        {
            Invoke(nameof(DestroyHelper), 0.2f);
            updateFailedSound();
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
        if (!IsServer) return;
        GameObject go = other.gameObject;
        if (collisionMask == (collisionMask | (1 << go.layer)))
        {
            // Debug.Log("Destroying");
            rb.velocity = Vector3.zero;
            Destroy(gameObject); 
        }

        if (isInfected.Value == false){
            updateFailedSound();
        }
        else{
            updateSound();
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


    //Audio Updates

    private void updateSound(){
        if(isInfected.Value == true){
            PLAYBACK_STATE shovingplaybackState;
            playerShovingSuccesssfx.getPlaybackState (out shovingplaybackState);

            if(shovingplaybackState.Equals(PLAYBACK_STATE.STOPPED)){
                playerShovingSuccesssfx.start();
            }
        }
        else{
            playerShovingSuccesssfx.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }

    private void updateFailedSound(){
        if(isInfected.Value == false){
            PLAYBACK_STATE shovingFailedplaybackState;
            playerShovingFailedsfx.getPlaybackState (out shovingFailedplaybackState);

            if(shovingFailedplaybackState.Equals(PLAYBACK_STATE.STOPPED)){
                playerShovingFailedsfx.start();
            }
        }
        else{
            playerShovingFailedsfx.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }


}
