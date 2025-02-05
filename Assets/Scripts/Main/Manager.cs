using System.Collections;
using System.Collections.Generic;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using System.Linq.Expressions;

[Serializable] public class Row
{
    public Button button;
    [ReadOnly] public MovingTroop[] playerTroops;
    [ReadOnly] public Environment environment = null;
}

public class Manager : PhotonCompatible
{

#region Variables

    public static Manager inst;

    [Foldout("Players", true)]
    public List<Player> playersInOrder;
    public Transform storePlayers { get; private set; }
    public Player currentPlayer { get; private set; }

    [Foldout("Gameplay", true)]
    int turnNumber = 0;
    List<Action> actionStack = new();
    int currentStep = -1;

    [Foldout("Cards", true)]
    public Transform deck;
    public Transform discard;
    public List<Row> allRows = new();

    [Foldout("UI and Animation", true)]
    [SerializeField] TMP_Text instructions;
    public float opacity { get; private set; }
    bool decrease = true;
    public Canvas canvas { get; private set; }

    [Foldout("Ending", true)]
    [SerializeField] Transform endScreen;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] Button quitGame;

    #endregion

#region Setup

    protected override void Awake()
    {
        base.Awake();
        inst = this;
        bottomType = this.GetType();
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        storePlayers = GameObject.Find("Store Players").transform;
    }

    private void FixedUpdate()
    {
        if (decrease)
            opacity -= 0.05f;
        else
            opacity += 0.05f;
        if (opacity < 0 || opacity > 1)
            decrease = !decrease;
    }

    public GameObject MakeObject(GameObject prefab)
    {
        if (PhotonNetwork.IsConnected)
            return PhotonNetwork.Instantiate(prefab.name, Vector3.zero, new());
        else
            return Instantiate(prefab);
    }

    private void Start()
    {
        if (PhotonNetwork.CurrentRoom.MaxPlayers == 1)
            MakeObject(CarryVariables.inst.playerPrefab.gameObject);
        MakeObject(CarryVariables.inst.playerPrefab.gameObject);

        foreach (Row row in allRows)
            row.playerTroops = new MovingTroop[2];

        if (PhotonNetwork.IsMasterClient)
        {
            while (deck.childCount < 20)
            {
                for (int i = 0; i < CarryVariables.inst.cardScripts.Count; i++)
                {
                    GameObject next = MakeObject(CarryVariables.inst.cardPrefab.gameObject);
                    DoFunction(() => AddCard(next.GetComponent<PhotonView>().ViewID, CarryVariables.inst.cardScripts[i]), RpcTarget.AllBuffered);
                }
            }
        }
        StartCoroutine(Setup());
    }

    IEnumerator Setup()
    {
        CoroutineGroup group = new(this);
        group.StartCoroutine(WaitForPlayers());
        group.StartCoroutine(SinglePlayerWait());

        IEnumerator SinglePlayerWait()
        {
            if (PhotonNetwork.CurrentRoom.MaxPlayers == 1)
                yield return new WaitForSeconds(1f);
        }

        IEnumerator WaitForPlayers()
        {
            if (PhotonNetwork.IsConnected)
            {
                instructions.text = $"Waiting for more players ({storePlayers.childCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
                while (storePlayers.childCount < PhotonNetwork.CurrentRoom.MaxPlayers)
                {
                    instructions.text = $"Waiting for more players ({storePlayers.childCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
                    yield return null;
                }
                instructions.text = $"All players are in.";
            }
        }

        while (group.AnyProcessing)
        {
            yield return null;
        }

        if (PhotonNetwork.IsMasterClient)
            ReadySetup();
    }

    void ReadySetup()
    {
        deck.Shuffle();
        storePlayers.Shuffle();

        for (int i = 0; i < storePlayers.childCount; i++)
        {
            GameObject nextPlayer = storePlayers.transform.GetChild(i).gameObject;
            DoFunction(() => AddPlayer(nextPlayer.GetComponent<PhotonView>().ViewID, i, nextPlayer.name.Equals("Computer") ? 1 : 0));
        }
    }

    [PunRPC]
    void AddPlayer(int PV, int position, int playerType)
    {
        Player nextPlayer = PhotonView.Find(PV).GetComponent<Player>();
        playersInOrder ??= new();
        playersInOrder.Insert(position, nextPlayer);
        instructions.text = "";
        nextPlayer.AssignInfo(position, (PlayerType)playerType);
    }

    [PunRPC]
    void AddCard(int ID, string cardName)
    {
        GameObject nextObject = PhotonView.Find(ID).gameObject;
        nextObject.name = cardName;
        nextObject.transform.SetParent(deck);
        nextObject.transform.localPosition = new(250, 10000);
        Type type = Type.GetType(cardName.Replace(" ", ""));
        nextObject.AddComponent(type);
    }

    int waiting = 2;
    [PunRPC]
    internal void PlayerDone()
    {
        waiting--;
        if (waiting == 0)
        {
            Log.inst.DoFunction(() => Log.inst.AddText($"{playersInOrder[0].name} vs {playersInOrder[1].name}", 0 ));
            Continue();
        }
    }

    #endregion

#region Gameplay Loop

    void AddStep(Action action, int position = -1)
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            if (position < 0 || currentStep < 0)
                actionStack.Add(action);
            else
                actionStack.Insert(currentStep + position, action);
        }
    }

    [PunRPC]
    public void Instructions(string text)
    {
        instructions.text = /*KeywordTooltip.instance.EditText*/(text);
    }

    [PunRPC]
    public void Continue()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
            Invoke(nameof(NextAction), 0.25f);
    }

    void NextAction()
    {
        if (currentStep < actionStack.Count - 1)
        {
            currentStep++;

            bool keepPlaying = true;
            foreach (Player player in playersInOrder)
            {
                if (player.myBase.currentHealth <= 0)
                    keepPlaying = false;
            }

            if (keepPlaying)
                actionStack[currentStep]();
            else
                DoFunction(() => DisplayEnding(-1), RpcTarget.All);
        }
        else
        {
            AddBasicLoop();
            NextAction();
        }
    }

    void AddBasicLoop()
    {
        AddStep(NewResources);
        PlayerSteps(0);
        PlayerSteps(1);
        AddStep(TroopsAttack);

        AddStep(NewResources);
        PlayerSteps(1);
        PlayerSteps(0);
        AddStep(TroopsAttack);

        void NewResources()
        {
            turnNumber++;
            Log.inst.PreserveTextRPC("", 0);
            Log.inst.PreserveTextRPC($"Start of round {turnNumber}", 0);
            Player host = FindThisPlayer();

            foreach (Player player in playersInOrder)
            {
                player.DrawCardRPC(host.realTimePlayer, 1, 1);
                Log.inst.RememberStep(player, StepType.Revert, () => player.GainLoseCoin(false, -1 * player.coins, -1));
                Log.inst.RememberStep(player, StepType.Revert, () => player.GainLoseCoin(false, turnNumber, 1));
            }

            Log.inst.ShareSteps();
            Continue();
        }

        void PlayerSteps(int position)
        {
            Player player = playersInOrder[position];
            AddStep(() => player.DoFunction(() => player.YourTurn(), player.realTimePlayer));
        }

        void TroopsAttack()
        {
            Log.inst.PreserveTextRPC("", 0);
            Log.inst.PreserveTextRPC("Attack phase", 0);
            SimulateBattle();
            Log.inst.ShareSteps();
            Continue();
        }
    }

    #endregion

#region Attacking

    internal void SimulateBattle()
    {
        //Debug.Log("simulate battle");

        foreach (Row row in allRows)
        {
            MovingTroop firstTroop = row.playerTroops[0];
            MovingTroop secondTroop = row.playerTroops[1];

            if (Alive(firstTroop) && Alive(secondTroop))
            {
                Log.inst.PreserveTextRPC($"{playersInOrder[0].name}'s {firstTroop.name} fights {playersInOrder[1].name}'s {secondTroop.name}.", 0);
                firstTroop.ChangeHealthRPC(-secondTroop.currentDamage, 1);
                secondTroop.ChangeHealthRPC(-firstTroop.currentDamage, 1);
            }
            else if (Alive(firstTroop))
            {
                Log.inst.PreserveTextRPC($"{playersInOrder[0].name}'s {firstTroop.name} attacks {playersInOrder[1].name}.", 0);
                playersInOrder[1].myBase.ChangeHealthRPC(-firstTroop.currentDamage, 1);
            }
            else if (Alive(secondTroop))
            {
                Log.inst.PreserveTextRPC($"{playersInOrder[1].name}'s {secondTroop.name} attacks {playersInOrder[0].name}.", 0);
                playersInOrder[0].myBase.ChangeHealthRPC(-secondTroop.currentDamage, 1);
            }

            bool Alive(MovingTroop troop)
            {
                return troop != null && troop.currentHealth >= 1;
            }
        }
    }

    #endregion

#region Ending

    [PunRPC]
    public void DisplayEnding(int resignPosition)
    {
        scoreText.text = "";
        Popup[] allPopups = FindObjectsByType<Popup>(FindObjectsSortMode.None);
        foreach (Popup popup in allPopups)
            Destroy(popup.gameObject);

        List<Player> playerLifeInOrder = playersInOrder.OrderByDescending(player => player.myBase.currentHealth).ToList();
        int nextPlacement = 1;

        Log.inst.AddText("");
        Log.inst.AddText("The game has ended.");
        Instructions("The game has ended.");

        Player resignPlayer = null;
        if (resignPosition >= 0)
        {
            resignPlayer = playersInOrder[resignPosition];
            Log.inst.AddText($"{resignPlayer.name} has resigned.");
        }

        for (int i = 0; i < playerLifeInOrder.Count; i++)
        {
            Player player = playerLifeInOrder[i];
            if (player != resignPlayer)
            {
                EndstatePlayer(player, false);
                if (i == 0 || playerLifeInOrder[i - 1].myBase.currentHealth != player.myBase.currentHealth)
                    nextPlacement++;
            }
        }

        if (resignPlayer != null)
            EndstatePlayer(resignPlayer, true);
        scoreText.text = KeywordTooltip.instance.EditText(scoreText.text);

        endScreen.gameObject.SetActive(true);
        quitGame.onClick.AddListener(Leave);
    }

    void EndstatePlayer(Player player, bool resigned)
    {
        scoreText.text += $"\n\n{player.name} - {player.myBase.currentHealth} Health {(resigned ? $"[Resigned]" : "")}";
        scoreText.text += "\n";
    }

    void Leave()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("1. Lobby");
        }
        else
        {
            SceneManager.LoadScene("0. Loading");
        }
    }


    #endregion

#region Misc

    public Player OtherPlayer(Player player)
    {
        return OtherPlayer(player.playerPosition);
    }

    public Player OtherPlayer(int position)
    {
        return (position == 0) ? playersInOrder[1] : playersInOrder[0];
    }

    public Player FindThisPlayer()
    {
        foreach (Player player in playersInOrder)
        {
            if (player.InControl())
                return player;
        }
        return null;
    }

    [PunRPC]
    internal void SetCurrentPlayer(int playerPosition)
    {
        currentPlayer = playersInOrder[playerPosition];
    }

    public List<Card> GatherAbilities()
    {
        List<Card> listOfCards = new();
        foreach (Row row in allRows)
        {
            if (row.environment != null)
                listOfCards.Add(row.environment.myCard);
            if (row.playerTroops[0] != null)
                listOfCards.Add(row.playerTroops[0].myCard);
            if (row.playerTroops[1] != null)
                listOfCards.Add(row.playerTroops[1].myCard);
        }
        return listOfCards;
    }

    #endregion

}
