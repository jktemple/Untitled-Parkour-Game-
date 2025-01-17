
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VirusTagGameModeManager : NetworkBehaviour
{
    // used to set the first infected's base speed
    //private PlayerMovement pm;

    public float roundLength;
    public float betweenRoundTime;
    public int maxScore;

    //public float defaultRunSpeed;
    //public float infectedRunSpeed;
    private float roundLengthTimer;
    private bool roundOngoing;
    private bool gameOngoing;

    private GameObject gameOverUI;
    private GameObject roundOverUI;
    private GameObject roundStartUI;
    private GameObject infectedUI;
    private GameObject runUI;
    public NetworkVariable<float> currentTime = new NetworkVariable<float>();
    [SerializeField] private Button startRoundButton;

    public GameObject[] spawnPoints;
    Shoving[] shoveList;
    Queue<Shoving> scoreQueue = new Queue<Shoving>();
    Stack<Shoving> orderStack;
    public TextMeshProUGUI buttonText;

    private void Awake()
    {
        gameOverUI = GameObject.Find("GameOverUI");
        gameOverUI.SetActive(false);
        roundOverUI = GameObject.Find("RoundOverUI");
        roundOverUI.SetActive(false);
        roundStartUI = GameObject.Find("RoundStartUI");
        roundStartUI.SetActive(false);
        infectedUI = GameObject.Find("You'reItUI");
        infectedUI.SetActive(false);
        runUI = GameObject.Find("RunUI");
        runUI.SetActive(false); 
    }
    // Start is called before the first frame update
    void Start()
    {
       
        //spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        if (startRoundButton!=null)
        startRoundButton.onClick.AddListener(() => {
            if (!gameOngoing) Invoke(nameof(StartGame), 5f);
            FindObjectOfType<InGameMenuBehaviors>().ResumeGame();
            startRoundButton.gameObject.SetActive(false);
            //else if (!roundOngoing) StartRound();
        });

        
    }

    bool betweenRounds;
    private float betweenRoundTimer;
    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;

        /*
        if(Keyboard.current.f5Key.wasPressedThisFrame)
        {
            if(!gameOngoing && !roundOngoing) StartGame();
            else if(gameOngoing && !roundOngoing) StartRound();
        }
        */


        if (roundLengthTimer > 0 && roundOngoing)
        {
            roundLengthTimer -= Time.deltaTime;
            if (AllInfected())
            {
                EndRound();
            }
        } else if(roundOngoing && roundLengthTimer < 0)
        {
            EndRound();
        }

        if(betweenRounds && betweenRoundTimer > 0)
        {
            betweenRoundTimer -= Time.deltaTime;
        } else if(betweenRounds && betweenRoundTimer < 0)
        {
            StartRound();
            betweenRounds = false;
        }

        if (roundOngoing) currentTime.Value = roundLengthTimer;
        else if (betweenRounds) currentTime.Value = betweenRoundTimer;

        if (!gameOngoing && buttonText!=null) buttonText.SetText("Start Game");
        else if (!roundOngoing && buttonText != null) buttonText.SetText("Start Round");
        else if(buttonText != null) buttonText.SetText("Round Ongoing");
    }


    private bool AllInfected()
    {
        Shoving[] listOfPlayers = GameObject.FindObjectsOfType<Shoving>();
        bool r = true;
        foreach(Shoving s in listOfPlayers)
        {
            if (!s.infected.Value) 
            {
                r = false;
            }
            else if(s.infected.Value && !scoreQueue.Contains(s))
            {
                scoreQueue.Enqueue(s);
                SendTaggedUI(s.OwnerClientId);
            }
        }
        return r;
    }

    public void MovePlayersToSpawnPoints()
    {
        if (!IsServer) { return; }
        MoveToSpawn[] moveList = FindObjectsOfType<MoveToSpawn>();
        Shuffle(spawnPoints);
        for (int i = 0; i < moveList.Length; i++)
        {
            if (i < spawnPoints.Length) 
            {
                moveList[i].spawnPointTransfrom.Value = spawnPoints[i].transform.position;
            }
            else
            {
                moveList[i].spawnPointTransfrom.Value = spawnPoints[0].transform.position;
            }
            moveList[i].moveToSpawn.Value = true;
        }
    }

    void Shuffle(GameObject[] moveList) 
    {
          for (int i = 0; i < moveList.Length - 1; i++) 
          {
              int rnd = Random.Range(i,  moveList.Length);
              GameObject tempGO = moveList[rnd];
              moveList[rnd] = moveList[i];
              moveList[i] = tempGO;
          }
    }

    void Shuffle(Shoving[] moveList)
    {
        for (int i = 0; i < moveList.Length - 1; i++)
        {
            int rnd = Random.Range(i, moveList.Length);
            Shoving tempGO = moveList[rnd];
            moveList[rnd] = moveList[i];
            moveList[i] = tempGO;
        }
    }


    private void StartGame()
    {
        shoveList = FindObjectsOfType<Shoving>();
        Debug.Log("shoveList size = " + shoveList.Length);
        Shuffle(shoveList);
        orderStack = new Stack<Shoving>(shoveList);
        foreach (Shoving s in shoveList)
        {
            s.score.Value = 0;
        }
        gameOngoing = true;
        Debug.Log("Starting game Num Players = " + orderStack.Count);
        StartBetwenRounds();
    }

    private void EndGame()
    {
        gameOngoing = false;
        //Display Game Over stuff
        //Winner +
        //Final Score Board
    }

    private void StartBetwenRounds()
    {
        betweenRounds = true;
        betweenRoundTimer = betweenRoundTime;
        StartBetweenRoundClientRPC();
    }

    [ClientRpc]
    public void StartBetweenRoundClientRPC()
    {
        GameObject label = GameObject.Find("Round Timer Lable");
        label.GetComponent<TextMeshProUGUI>().SetText("Round Starting:");
    }

    [ClientRpc]
    public void StartRoundUIClientRPC()
    {
        GameObject label = GameObject.Find("Round Timer Lable");
        label.GetComponent<TextMeshProUGUI>().SetText("Round Timer:");
        roundStartUI.SetActive(true);
        StartCoroutine(FadeOutRoundStart(1.5f));
        Debug.Log("Starting Round");
        /*
         * Pull up UI element that says round Starting and Fade it out
         */
    }

    IEnumerator FadeOutRoundStart(float time) {
        float t = 0.0f;
        Image image = roundStartUI.GetComponentInChildren<Image>();
        TextMeshProUGUI text = roundStartUI.GetComponentInChildren<TextMeshProUGUI>();
        Color iColor = image.color;
        Color tColor = text.color;
        float iStartAlpha = iColor.a;
        float tStartAlpha = tColor.a;
        while(t < time)
        {
            t += Time.deltaTime;
            float currIAlpha = Mathf.Lerp(iStartAlpha, 0, t / time);
            float currTAlpha = Mathf.Lerp(tStartAlpha, 0, t / time);
            iColor.a = currIAlpha;
            tColor.a = currTAlpha;
            text.color = tColor;
            image.color = iColor;
            yield return null;
        }
        iColor.a = iStartAlpha;
        tColor.a = tStartAlpha;
        text.color = tColor;
        image.color = iColor;
        roundStartUI.SetActive(false);
        
    }

    [ClientRpc]
    public void EndRoundUIClientRPC()
    {
        Debug.Log("Round Over");
        roundOverUI.SetActive(true);
        GameObject m = GameObject.Find("RoundOverBody");
        if (AllInfected())
        {
            Debug.Log("All Players Infected");
            m.GetComponent<TextMeshProUGUI>().SetText("All Players Infected");
        }
        else
        {
            string temp = "";
            Shoving[] shovings = FindObjectsOfType<Shoving>();
            foreach (Shoving s in shovings)
            {
                if (!s.infected.Value)
                {
                    temp += s.playerName.Value + "\n";
                }
            }
            Debug.Log("Surving Players:\n" + temp);
            m.GetComponent<TextMeshProUGUI>().SetText("Surving Players:\n" + temp);
        }
        Invoke(nameof(HideRoundOver), 10f);
    }
    

    [ClientRpc]
    public void EndGameUIClientRPC()
    {
        Debug.Log("Game Over! Final Scores:");
        FindObjectOfType<InGameMenuBehaviors>().ShowScore();
        startRoundButton.gameObject.SetActive(true);
        gameOverUI.SetActive(true);
        Invoke(nameof(HideGameOver), 10f);
        /*
         * 
         */
    }

    void SendUntaggedUI(ulong[] ids)
    {
        if (!IsServer) { return; }
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = ids 
            }
        };

        UntaggedUIClientRPC(clientRpcParams);
    }

    [ClientRpc]
    public void UntaggedUIClientRPC(ClientRpcParams clientRpcParams = default)
    {
        runUI.SetActive(true);
        Invoke(nameof(HideRun), 2f);
    } 

    void SendTaggedUI(ulong targetID)
    {
        if(!IsServer) { return; }
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { targetID }
            }
        };

        TaggedUIClientRPC(clientRpcParams);
    }

    [ClientRpc]
    public void TaggedUIClientRPC(ClientRpcParams clientRpcParams = default)
    {
        infectedUI.SetActive(true);
        Invoke(nameof(HideTagged), 2f);
    }

    private void HideRun()
    {
        runUI.SetActive(false);
    }

    private void HideTagged()
    {
        infectedUI.SetActive(false);
    }
    private void HideGameOver()
    {
        gameOverUI.SetActive(false);
    }

    private void HideRoundOver()
    {
        roundOverUI.SetActive(false);
    }
    public void StartRound()
    {
        if(roundOngoing) { return; }
        AssignInfectedPlayer();
        MovePlayersToSpawnPoints();
        StartRoundUIClientRPC();
        roundLengthTimer = roundLength;  
        roundOngoing = true;
        //shoveList = FindObjectsOfType<Shoving>();
    }

    public void EndRound()
    {
        //do something'
        roundOngoing= false;
        MovePlayersToSpawnPoints();
        currentTime.Value = 0;
        AssignScores();
        scoreQueue.Clear();
        if(orderStack.Count <= 0)
        {
            EndGame();
            EndGameUIClientRPC();
        } else
        {
            EndRoundUIClientRPC();
            StartBetwenRounds();
        }
    }

    void AssignScores()
    {
        foreach(Shoving s in shoveList)
        {
            if(s.infected.Value == false)
            {
                s.score.Value += maxScore;
            }
        }
        int increment = maxScore / shoveList.Length;
        int count = 0;
        while (scoreQueue.Count > 0)
        {
           Shoving s = scoreQueue.Dequeue();
           s.score.Value += (count * increment);
           count++;
        }
    }

    public void AssignInfectedPlayer()
    {
        if (!IsServer) return;
        //search for all the players in the current scene
        //randomly chose one to make infected

       
        List<ulong> idList = new List<ulong>();
        foreach (Shoving sho in shoveList)
        {
            sho.infected.Value = false;
            idList.Add(sho.OwnerClientId);
            //sho.pm.runSpeed.Value = defaultRunSpeed;
        }
        if (orderStack.Count > 0)
        {
            Shoving s = orderStack.Pop();
            s.infected.Value = true;
            scoreQueue.Enqueue(s);
            Debug.Log("Sending Tagged UI Client #" + s.OwnerClientId);
            SendTaggedUI(s.OwnerClientId);

            //add to first infected's base speed here
            // don't += here bc it get's called every new round and could double up on someone
            //s.pm.runSpeed.Value = infectedRunSpeed;

            idList.Remove(s.OwnerClientId);
            Debug.Log("Sending Untagged UI Clients #" + idList.ToString());
            SendUntaggedUI(idList.ToArray());
        }
    }

    void SendXray(bool xray, ulong id)
    {

    }

    [ClientRpc]
    void XRayClientRPC(bool xray, ClientRpcParams clientRpcParams = default)
    {
        
    }
}
