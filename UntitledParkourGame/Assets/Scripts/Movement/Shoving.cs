using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using FMOD.Studio;

public class Shoving : NetworkBehaviour
{
    
    public LayerMask playersMask;
    public Transform orientation;
    public Transform playerObject;
    public PlayerMovement pm;
    private PlayerControls inputs;
    public Rigidbody rb;
    public float shoveDistance;
    public float shoveSpherecastRadius;
    public float shoveForce;
    public float shoveCooldown;
    private bool ableToShove;
    public NetworkVariable<bool> shoved = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> shoveDir = new NetworkVariable<Vector3>();
    public NetworkVariable<bool> infected = new NetworkVariable<bool>();
    public NetworkVariable<int> score = new NetworkVariable<int>();
    public NetworkVariable<int> playerNumber = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>("Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private bool inShoveLag = false;

    
    public bool pushObject;

    public Animator animator;
    private int taggedHash;
    public SphereCastVisual sphereCastVisual;
    public PushObject pushObjectPrefab;
    public FakePushObject fakePush;

    public float camOffset;
    public bool hitBoxVisuals;
    public bool sphereCast;

    //Audio Private Instances
    // private EventInstance playerShovingFailedsfx;
    private EventInstance playerGetShovedsfx;

    private ControllerRumbleManager rumble;

    [SerializeField]
    NameTag nameTag;
    // Start is called before the first frame update

    public override void OnNetworkSpawn()
    {
        playerName.OnValueChanged += OnNameChanged;
        base.OnNetworkSpawn();
    }

    public void OnNameChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        nameTag.SetNameTagText(current);
    }
    void Start()
    {

        pm = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
        Debug.Log("shove RigidBody = " + rb);
        if (!IsOwner) return;
        inputs = new PlayerControls();
        inputs.PlayerMovement.Enable();
        ableToShove = true;
        shoved.Value = false;
        taggedHash = Animator.StringToHash("Tagged");
        playerNumber.Value = FindObjectsOfType<Shoving>().Length;
        if(EditPlayerName.Instance != null)
        SetName(EditPlayerName.Instance.GetPlayerName());

        // playerShovingFailedsfx = AudioManager.instance.CreateInstance(FMODEvents.instance.playerShovingFailedsfx);
        playerGetShovedsfx = AudioManager.instance.CreateInstance(FMODEvents.instance.playerGetShovedsfx);
        rumble = GameObject.FindObjectOfType<ControllerRumbleManager>();
        if (IsServer)
        {
            infected.Value = false;
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        if (ableToShove && inputs.PlayerMovement.Shove.triggered)
        {
            if (!pushObject)
            {
                ShoveServerRPC(playerObject.position + orientation.forward * 0.7f, orientation.forward, infected.Value);
                ableToShove = false;
                Invoke(nameof(ResetShove), shoveCooldown);
            } else
            {
                Instantiate<FakePushObject>(fakePush, playerObject.position + orientation.forward*camOffset, Quaternion.LookRotation(orientation.forward));
                ServerRpcParams sendParams = new ServerRpcParams();
                sendParams.Receive.SenderClientId = NetworkManager.LocalClientId;
                SpawnPushObjectServerRPC(playerObject.position + orientation.forward*0.7f, orientation.forward, infected.Value, sendParams);
                ableToShove = false;
                Invoke(nameof(ResetShove), shoveCooldown);

            }
        }

        if(shoved.Value && !inShoveLag)
        {
            rb.AddForce(shoveDir.Value.normalized * shoveForce, ForceMode.Impulse);
            ResetShoveServerRPC();
            inShoveLag = true;
            Invoke(nameof(ResetShoveLag), 0.5f);
            if (rumble != null) { rumble.RumbleBurst(0.75f, 0.75f, 0.25f); }
        }
        animator.SetBool(taggedHash, infected.Value);
    }

    [ServerRpc]
    void SpawnPushObjectServerRPC(Vector3 position, Vector3 direction, bool i, ServerRpcParams serverRpcParams)
    {
        // Debug.Log("Spawning Object Position = " + position + " Direction = " + direction);
        
        PushObject p = Instantiate<PushObject>(pushObjectPrefab, position + direction, Quaternion.LookRotation(direction));
        var clientId = serverRpcParams.Receive.SenderClientId;
        p.id.Value = clientId;
        //p.distance.Value = shoveDistance;
        p.isInfected.Value = i;
        p.GetComponent<NetworkObject>().Spawn();
       

        //Debug.Log("Spawning with id value = " + p.id.Value);
    }
    
    void SetName(string name)
    {
        playerName.Value = name;
    }

    [ServerRpc]
    public void ShoveServerRPC(Vector3 position, Vector3 direction, bool infected)
    {
        //if(!IsServer) return;
        Debug.Log("Shove Executing");
        RaycastHit hit;
        if (sphereCast && Physics.SphereCast(position, shoveSpherecastRadius, direction, out hit, shoveDistance, playersMask))
        {
            Debug.Log("Shove Hit");
            Shoving s = hit.transform.GetComponentInParent<Shoving>();
            s.shoved.Value = true;
            s.shoveDir.Value = direction;
            if(infected)
            {
                s.infected.Value = true;
            }
        }
        if (hitBoxVisuals)
        {
            float diam = shoveSpherecastRadius * 2;
            SphereCastVisual st = Instantiate<SphereCastVisual>(sphereCastVisual);
            
            st.transform.position = position;
            st.diameter = diam;
            SphereCastVisual m = Instantiate<SphereCastVisual>(sphereCastVisual);
           
            m.transform.position = position + direction.normalized * (shoveDistance / 2);
            m.diameter = diam;
            SphereCastVisual l = Instantiate<SphereCastVisual>(sphereCastVisual);
          
            l.transform.position = position + direction.normalized * shoveDistance;
            l.diameter = diam;
            st.GetComponent<NetworkObject>().Spawn();
            m.GetComponent<NetworkObject>().Spawn();
            l.GetComponent<NetworkObject>().Spawn();

        }
        //shoot a sphere cast out from the center of the player object in the direction of orientation
        //if it hits call the client rpc to the owner of that player object

    }

    [ServerRpc]
    public void ResetShoveServerRPC()
    {
        shoved.Value = false;
        shoveDir.Value = Vector3.zero;
    }

    [ServerRpc(RequireOwnership = false)]
    private void makeInfectedServerRPC(bool i)
    {
        infected.Value = i;
    }
    private void ResetShove()
    {
        ableToShove = true;
    }

    private void ResetShoveLag()
    {
        inShoveLag = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
        {
            return;
        }

        if (other.CompareTag("PushObject"))
        {
            PushObject p = other.gameObject.GetComponent<PushObject>();
            if (p == null) { return; }
            if (this.OwnerClientId != p.id.Value)
            {
                shoved.Value = true;
                shoveDir.Value = p.transform.forward;
                if (p.isInfected.Value)
                {
                    infected.Value = true;
                    updateSound();

                    // add to tagged player's base speed here
                    //other.gameObject.GetComponent<PlayerMovement>().runSpeed.Value = 12;
                }
            }
            Destroy(other.gameObject);
        }
    }
    


    // audio sfx for after tag the player
    private void updateSound(){
        if(infected.Value == true){
            PLAYBACK_STATE shovingplaybackState;
            playerGetShovedsfx.getPlaybackState (out shovingplaybackState);

            if(shovingplaybackState.Equals(PLAYBACK_STATE.STOPPED)){
                playerGetShovedsfx.start();
            }
        }
        else{
            playerGetShovedsfx.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }


    // private void updateFailedSound(){
    //     // Debug.Log("testsound");
    //     if(infected.Value == false){
    //         PLAYBACK_STATE shovingFailedplaybackState;
    //         playerShovingFailedsfx.getPlaybackState (out shovingFailedplaybackState);

    //         if(shovingFailedplaybackState.Equals(PLAYBACK_STATE.STOPPED)){
    //             playerShovingFailedsfx.start();
    //         }
    //     }
    //     else{
    //         playerShovingFailedsfx.stop(STOP_MODE.ALLOWFADEOUT);
    //     }
    // }


}
