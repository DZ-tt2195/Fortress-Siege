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

[Serializable] public class Row
{
    public int position;
    public Button button;
    [ReadOnly] public MovingTroop[] playerTroops;
    [ReadOnly] public MovingAura auraHere = null;
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
    public int turnNumber { get; private set; }
    List<Action> actionStack = new();
    int currentStep = -1;
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
        turnNumber = 0;
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

        for (int i = 0; i<allRows.Count; i++)
        {
            Row row = allRows[i];
            row.playerTroops = new MovingTroop[2];
            row.position = i;
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
        storePlayers.Shuffle();

        for (int i = 0; i < storePlayers.childCount; i++)
        {
            GameObject nextPlayer = storePlayers.transform.GetChild(i).gameObject;
            DoFunction(() => AddPlayer(nextPlayer.GetComponent<PhotonView>().ViewID, i, nextPlayer.name.Equals("Bot") ? 1 : 0));
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

    int waiting = 2;
    [PunRPC]
    internal void PlayerDone()
    {
        waiting--;
        if (waiting == 0 && PhotonNetwork.IsMasterClient)
        {
            Log.inst.AddTextRPC($"{playersInOrder[0].name} vs. {playersInOrder[1].name}.", LogAdd.Remember, 0);
            foreach (Player player in playersInOrder)
                player.DrawCardRPC(4, -1);
            Log.inst.ShareSteps();
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
        instructions.text = KeywordTooltip.instance.EditText(text);
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
            actionStack[currentStep]();
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
        AddStep(CheckDeadPlayers);

        AddStep(NewResources);
        PlayerSteps(1);
        PlayerSteps(0);
        AddStep(TroopsAttack);
        AddStep(CheckDeadPlayers);

        void NewResources()
        {
            turnNumber++;
            Log.inst.AddTextRPC("", LogAdd.Remember, 0);
            Log.inst.AddTextRPC($"Start of round {turnNumber}.", LogAdd.Remember, 0);

            foreach (Player player in playersInOrder)
            {
                player.CoinRPC(-1 * player.coins, -1, "");

                int totalCoins = turnNumber;
                foreach ((Card card, Entity entity) in GatherAbilities())
                    totalCoins += card.CoinEffect(player, entity, 0);
                player.CoinRPC(totalCoins, 0, "");
                player.DrawCardRPC(1, 0, "");
            }

            Log.inst.ShareSteps();
        }

        void PlayerSteps(int position)
        {
            Player player = playersInOrder[position];
            AddStep(() => player.DoFunction(() => player.YourTurn(), player.realTimePlayer));
        }

        void TroopsAttack()
        {
            SimulateBattle(true);
        }

        void CheckDeadPlayers()
        {
            bool keepPlaying = true;
            foreach (Player player in playersInOrder)
            {
                if (player.myBase.myHealth <= 0)
                    keepPlaying = false;
            }

            if (keepPlaying)
                Continue();
            else
                DoFunction(() => DisplayEnding(-1), RpcTarget.All);
        }
    }

    #endregion

#region Attacking

    internal void SimulateBattle(bool playing)
    {
        Log.inst.AddTextRPC("", LogAdd.Remember, 0);
        Log.inst.AddTextRPC("Combat phase.", LogAdd.Remember, 0);

        foreach ((Card card, Entity entity) in GatherAbilities())
        {
            if (entity.currentRow >= 0)
                card.StartOfCombat(entity, 1);
        }
        CleanUp(1);
        StartCoroutine(DoBattle(playing));
    }

    IEnumerator DoBattle(bool playing)
    { 
        foreach (Row row in allRows)
        {
            MovingTroop firstTroop = row.playerTroops[0];
            MovingTroop secondTroop = row.playerTroops[1];

            bool firstAlive = IsAlive(firstTroop);
            bool secondAlive = IsAlive(secondTroop);

            if (firstAlive || secondAlive)
            {
                Log.inst.AddTextRPC($"Row {row.position + 1}", LogAdd.Remember, 0);
                int attack1 = firstAlive ? firstTroop.Attack(false, 1) : 0;
                int attack2 = secondAlive ? secondTroop.Attack(false, 1) : 0;

                foreach ((Card card, Entity entity) in GatherAbilities())
                {
                    if (entity.currentRow >= 0)
                    {
                        if (attack1 > 0)
                            card.CardAttacked(entity, firstTroop, secondAlive ? secondTroop : playersInOrder[1].myBase, 1);
                        if (attack2 > 0)
                            card.CardAttacked(entity, secondTroop, firstAlive ? firstTroop : playersInOrder[0].myBase, 1);
                    }
                }

                if (playing)
                {
                    yield return Log.inst.AddWait();
                }
            }

            CleanUp(2);
        }

        bool IsAlive(MovingTroop troop) => troop != null && troop.calcHealth >= 1;

        EndRound(playing);
    }

    void EndRound(bool playing)
    {
        if (playing)
        {
            Log.inst.AddTextRPC("", LogAdd.Remember, 0);
            Log.inst.AddTextRPC($"End of round {turnNumber}.", LogAdd.Remember, 0);
        }

        foreach (Row row in allRows)
        {
            if (row.auraHere != null)
            {
                row.auraHere.myCard.EndOfTurn(row.auraHere, 1);
            }
            foreach (MovingTroop troop in row.playerTroops)
            {
                if (troop != null)
                    troop.myCard.EndOfTurn(troop, 1);
            }
        }

        CleanUp(1);

        if (playing)
            Log.inst.ShareSteps();
    }

    public void CleanUp(int logged)
    {
        bool extraCleaning;
        do
        {
            extraCleaning = false;
            foreach (Player player in playersInOrder)
                player.myBase.UpdateText();

            foreach (Row row in allRows)
            {
                foreach (MovingTroop troop in row.playerTroops)
                {
                    if (troop != null)
                    {
                        troop.RecalculateStats();
                        if (troop.calcHealth <= 0)
                        {
                            extraCleaning = true;
                            troop.DestroyEntityRPC(logged);
                        }
                    }
                }
            }
        } while (extraCleaning);
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

        List<Player> playerLifeInOrder = playersInOrder.OrderByDescending(player => player.myBase.myHealth).ToList();
        int nextPlacement = 1;

        Log.inst.AddTextRPC("", LogAdd.Personal);
        Log.inst.AddTextRPC("The game has ended.", LogAdd.Personal);
        Instructions("The game has ended.");

        Player resignPlayer = null;
        if (resignPosition >= 0)
        {
            resignPlayer = playersInOrder[resignPosition];
            Log.inst.AddTextRPC($"{resignPlayer.name} has resigned.", LogAdd.Personal);
        }

        for (int i = 0; i < playerLifeInOrder.Count; i++)
        {
            Player player = playerLifeInOrder[i];
            if (player != resignPlayer)
            {
                EndstatePlayer(player, false);
                if (i == 0 || playerLifeInOrder[i - 1].myBase.myHealth != player.myBase.myHealth)
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
        scoreText.text += $"\n\n{player.name} - {player.myBase.myHealth} Health {(resigned ? $"[Resigned]" : "")}";
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

    public Player OpposingPlayer(Player player)
    {
        return OpposingPlayer(player.playerPosition);
    }

    public Player OpposingPlayer(int position)
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

    public MovingTroop FindMyTroop(Player player, int row)
    {
        if (row <= -1 || row >= 5)
            return null;
        else if (player.playerPosition == 0)
            return allRows[row].playerTroops[0];
        else
            return allRows[row].playerTroops[1];
    }

    public MovingTroop FindOpposingTroop(Player player, int row)
    {
        if (row <= -1 || row >= 5)
            return null;
        else if (player.playerPosition == 0)
            return allRows[row].playerTroops[1];
        else
            return allRows[row].playerTroops[0];
    }

    [PunRPC]
    internal void SetCurrentPlayer(int playerPosition)
    {
        currentPlayer = playersInOrder[playerPosition];
    }

    public List<(Card, Entity)> GatherAbilities()
    {
        List<(Card, Entity)> listOfCards = new();
        foreach (Row row in allRows)
        {
            if (row.auraHere != null)
                listOfCards.Add((row.auraHere.myCard, row.auraHere));
            if (row.playerTroops[0] != null)
                listOfCards.Add((row.playerTroops[0].myCard, row.playerTroops[0]));
            if (row.playerTroops[1] != null)
                listOfCards.Add((row.playerTroops[1].myCard, row.playerTroops[1]));
        }
        return listOfCards;
    }

    #endregion

}
