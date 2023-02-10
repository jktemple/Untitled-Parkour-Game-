using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//makes the camera follow the player
public class MoveCamera : NetworkBehaviour
{
    public Transform cameraPosition;

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        transform.position = cameraPosition.position;
    }
}
