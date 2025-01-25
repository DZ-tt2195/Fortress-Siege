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

[Serializable]
public class Column
{
    [SerializeField] Transform transform;
    [HideInInspector] public MovingTroop p1Troop = null;
    [HideInInspector] public MovingTroop p2Troop = null;
    [HideInInspector] public MovingTroop environment = null;
}

public class Manager : PhotonCompatible
{

#region Variables

    public static Manager instance;

    [Foldout("Players", true)]
    public List<Player> playersInOrder { get; private set; }
    public Transform storePlayers { get; private set; }

    [Foldout("Gameplay", true)]
    int turnNumber = 0;
    List<Action> actionStack = new();
    int currentStep = -1;
    List<Action> damageStack = new();
    List<TriggeredAbility> allAbilities = new();

    [Foldout("Cards", true)]
    public Transform deck;
    public List<Card> allCards { get; private set; }
    public List<Column> allColumns = new();

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
        instance = this;
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
        MakeObject(CarryVariables.instance.playerPrefab.gameObject);
        if (PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < CarryVariables.instance.cardScripts.Count; i++)
            {
                GameObject next = MakeObject(CarryVariables.instance.cardPrefab.gameObject);
                DoFunction(() => AddCard(next.GetComponent<PhotonView>().ViewID, CarryVariables.instance.cardScripts[i]), RpcTarget.AllBuffered);
            }
        }
        StartCoroutine(Setup());
    }

    IEnumerator Setup()
    {
        CoroutineGroup group = new(this);
        group.StartCoroutine(WaitForPlayers());

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
        {
            ReadySetup();
        }
    }

    void ReadySetup()
    {
        deck.Shuffle();
        storePlayers.Shuffle();

        for (int i = 0; i < storePlayers.childCount; i++)
            DoFunction(() => AddPlayer(storePlayers.transform.GetChild(i).GetComponent<PhotonView>().ViewID, i ));
    }

    [PunRPC]
    void AddPlayer(int PV, int position)
    {
        Player nextPlayer = PhotonView.Find(PV).GetComponent<Player>();
        playersInOrder ??= new();
        playersInOrder.Insert(position, nextPlayer);
        instructions.text = "";
        nextPlayer.AssignInfo(position);
    }

    [PunRPC]
    void AddCard(int ID, string cardName)
    {
        GameObject nextObject = PhotonView.Find(ID).gameObject;

        nextObject.name = cardName;
        nextObject.transform.SetParent(deck);

        allCards ??= new();
        nextObject.transform.localPosition = new(250 * allCards.Count, 10000);

        Type type = Type.GetType(cardName.Replace(" ", ""));
        nextObject.AddComponent(type);

        Card card = nextObject.GetComponent<Card>();
        allCards.Add(card);
    }

    int waiting = 2;
    [PunRPC]
    internal void PlayerDone()
    {
        waiting--;
        if (waiting == 0)
        {
            Log.instance.DoFunction(() => Log.instance.AddText($"{playersInOrder[0].name} vs {playersInOrder[1].name}", 0 ));
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
        instructions.text = KeywordTooltip.instance.EditText(text);
    }

    [PunRPC]
    public void Continue()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            Invoke(nameof(NextAction), 0.25f);
        }
    }

    void NextAction()
    {
        if (currentStep < actionStack.Count - 1)
        {
            foreach (Action action in damageStack)
                action();
            damageStack.Clear();

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
            Log.instance.DoFunction(() => Log.instance.AddText($"Turn {turnNumber}", 0));
            foreach (Player player in playersInOrder)
            {
                player.DoFunction(() => player.FindCardsFromDeck(1, 0), RpcTarget.MasterClient);
                player.DoFunction(() => player.GainLoseCoin(-1*player.coins, -1));
                player.DoFunction(() => player.GainLoseCoin(turnNumber, 0));
            }
        }

        void PlayerSteps(int position)
        {
            Player player = playersInOrder[position];
            AddStep(() => player.DoFunction(() => player.YourTurn(), player.realTimePlayer));
        }

        void TroopsAttack()
        {
            Log.instance.DoFunction(() => Log.instance.AddText("", 0));
            Log.instance.DoFunction(() => Log.instance.AddText("Attack phase.", 0));
            Continue();
        }
    }

    [PunRPC]
    public void StoreDamage(int pv, int damage, bool multiTarget)
    {
        MovingTroop troop = PhotonView.Find(pv).GetComponent<MovingTroop>();
        //damageStack.Add(() => troop.DoFunction(() => troop.TakeDamage(damage, multiTarget)));
    }

    #endregion

#region Attacking

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

        Log.instance.AddText("");
        Log.instance.AddText("The game has ended.");
        Instructions("The game has ended.");

        Player resignPlayer = null;
        if (resignPosition >= 0)
        {
            resignPlayer = playersInOrder[resignPosition];
            Log.instance.AddText($"{resignPlayer.name} has resigned.");
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

    public void AddAbility(TriggeredAbility ability)
    {
        allAbilities.Add(ability);
    }

    public int CalculateFromAbility(string condition, object[] array, int baseNumber)
    {
        int newNumber = baseNumber;
        this.allAbilities.RemoveAll(ability => ability.source == null);
        foreach (TriggeredAbility ability in this.allAbilities)
        {
            if (ability.CheckAbility(condition, array))
                newNumber += (int)ability.ResolveAbility(-1, array);
        }
        return newNumber;
    }

    public void ResolveAbilities(string condition, object[] array, int logged)
    {
        this.allAbilities.RemoveAll(ability => ability.source == null);
        foreach (TriggeredAbility ability in this.allAbilities)
        {
            if (ability.CheckAbility(condition, array))
                ability.ResolveAbility(logged, array);
        }
    }

    #endregion

}
