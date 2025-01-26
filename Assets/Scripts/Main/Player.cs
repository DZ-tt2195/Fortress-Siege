using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using MyBox;
using System;

public enum PlayerType { Human, Computer }
public class Player : PhotonCompatible
{

#region Variables

    [Foldout("Player info", true)]
    [ReadOnly] public int playerPosition;
    public PlayerType myType { get; private set; }
    public Photon.Realtime.Player realTimePlayer { get; private set; }
    public int coins { get; private set; }
    public PlayerBase myBase { get; private set; }
    public List<Card> cardsInHand = new();

    [Foldout("UI", true)]
    [SerializeField] TMP_Text resourceText;
    Button resignButton;
    Transform keepHand;

    [Foldout("Choices", true)]
    public int choice { get; private set; }
    public Card chosenCard { get; private set; }
    Stack<List<Action>> decisionReact = new();

    #endregion

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();

        resignButton = GameObject.Find("Resign Button").GetComponent<Button>();
        keepHand = transform.Find("Keep Hand");
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnected && pv.AmOwner)
        {
            if (PhotonNetwork.CurrentRoom.MaxPlayers == 1 && Manager.instance.storePlayers.childCount == 0)
                DoFunction(() => SendName("Computer"), RpcTarget.AllBuffered);
            else
                DoFunction(() => SendName(PlayerPrefs.GetString("Online Username")), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void SendName(string username)
    {
        pv.Owner.NickName = username;
        this.name = username;
        this.transform.SetParent(Manager.instance.storePlayers);
    }

    internal void AssignInfo(int position, PlayerType type)
    {
        this.playerPosition = position;
        this.myType = type;
        Manager.instance.storePlayers.transform.localScale = Manager.instance.canvas.transform.localScale;
        this.transform.localPosition = Vector3.zero;
        if (PhotonNetwork.IsConnected)
            realTimePlayer = PhotonNetwork.PlayerList[pv.OwnerActorNr - 1];

        if (InControl())
        {
            GameObject obj = Manager.instance.MakeObject(CarryVariables.instance.playerBasePrefab.gameObject);
            DoFunction(() => GetBase(obj.GetComponent<PhotonView>().ViewID), RpcTarget.All);
            if (this.myType == PlayerType.Human)
            {
                resignButton.onClick.AddListener(() => Manager.instance.DoFunction(() => Manager.instance.DisplayEnding(this.playerPosition), RpcTarget.All));
                Invoke(nameof(MoveScreen), 0.2f);
                pv.Owner.NickName = this.name;
            }
            StartCoroutine(DelayDraw());

            IEnumerator DelayDraw()
            {
                if (this.myType == PlayerType.Computer)
                    yield return new WaitForSeconds(0.2f);
                DoFunction(() => FindCardsFromDeck(4, -1), RpcTarget.MasterClient);
                Manager.instance.DoFunction(() => Manager.instance.PlayerDone());
            }
        }
    }

    [PunRPC]
    void GetBase(int PV)
    {
        myBase = PhotonView.Find(PV).gameObject.GetComponent<PlayerBase>();
        myBase.AssignPlayer(this, 20);
    }

    #endregion

#region Hand

    [PunRPC]
    public void FindCardsFromDeck(int cardsToDraw, int logged)
    {
        int[] listOfCardIDs = new int[cardsToDraw];

        for (int i = 0; i < cardsToDraw; i++)
        {
            Card nextCard = Manager.instance.deck.GetChild(0).GetComponent<Card>();
            nextCard.transform.SetParent(null);
            listOfCardIDs[i] = nextCard.pv.ViewID;
        }
        this.DoFunction(() => DrawCards(listOfCardIDs, logged));
    }

    [PunRPC]
    void DrawCards(int[] cardsToDraw, int logged)
    {
        for (int i = 0; i < cardsToDraw.Length; i++)
        {
            Card card = PhotonView.Find(cardsToDraw[i]).GetComponent<Card>();
            cardsInHand.Add(card);

            if (InControl() && this.myType == PlayerType.Human)
                Log.instance.AddText($"{this.name} draws {card.name}.", logged);
            else
                Log.instance.AddText($"{this.name} draws 1 card.", logged);

            card.transform.SetParent(this.keepHand);
            card.transform.localPosition = new Vector2(0, -1100);
            card.layout.FillInCards(card);
            card.layout.cg.alpha = 0;
        }
        SortHand();
    }

    public void SortHand()
    {
        if (myBase != null)
            myBase.UpdateText();

        float start = -1100;
        float end = 475;
        float gap = 225;

        float midPoint = (start + end) / 2;
        int maxFit = (int)((Mathf.Abs(start) + Mathf.Abs(end)) / gap);

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            Card nextCard = cardsInHand[i];

            nextCard.transform.SetParent(keepHand);
            nextCard.transform.SetSiblingIndex(i);

            float offByOne = cardsInHand.Count - 1;
            float startingX = (cardsInHand.Count <= maxFit) ? midPoint - (gap * (offByOne / 2f)) : (start);
            float difference = (cardsInHand.Count <= maxFit) ? gap : gap * (maxFit / offByOne);

            Vector2 newPosition = new(startingX + difference * i, -540);
            StartCoroutine(nextCard.MoveCard(newPosition, 0.25f, Vector3.one));
            if (InControl() && myType == PlayerType.Human)
                StartCoroutine(nextCard.RevealCard(0.25f));
        }
    }

    [PunRPC]
    void DiscardFromHand(int cardID, int logged)
    {
        Card card = PhotonView.Find(cardID).GetComponent<Card>();
        cardsInHand.Remove(card);

        Log.instance.AddText($"{this.name} discards {card.name}.", logged);
        StartCoroutine(card.MoveCard(new(0, -1000), 0.25f, Vector3.one));
        SortHand();
    }

    [PunRPC]
    public void GainLoseCoin(int amount, int logged)
    {
        coins += amount;
        if (amount >= 0)
            Log.instance.AddText($"{this.name} gains ${amount}.", logged);
        else
            Log.instance.AddText($"{this.name} loses ${Mathf.Abs(amount)}.", logged);

        if (myBase != null)
            myBase.UpdateText();
    }

    #endregion

#region Main Turn

    [PunRPC]
    public void YourTurn()
    {
        Manager.instance.DoFunction(() => Manager.instance.Instructions($"Waiting on {this.name}..."), RpcTarget.Others);
        Log.instance.DoFunction(() => Log.instance.AddText("", 0));
        Log.instance.DoFunction(() => Log.instance.AddText($"{this.name}'s turn", 0));
        ContinueTurn();
    }

    void ContinueTurn()
    { 
        List<string> actions = new() { $"End Turn" };
        ChooseButton(actions, "", ActionResolution);

        List<Card> canPlay = cardsInHand.Where(card => card.CanPlayMe(this, true)).ToList();
        ChooseCardOnScreen(canPlay, $"What to play?", null);

        void ActionResolution()
        {
            if (chosenCard != null)
            {
                PlayCard(chosenCard, ContinueTurn, 0);
            }
            else
            {
                Manager.instance.DoFunction(() => Manager.instance.Continue());
            }
        }
    }

    public void PlayCard(Card card, Action action, int logged)
    {
        Log.instance.DoFunction(() => Log.instance.AddText($"{this.name} plays {card.name}.", logged));
        DoFunction(() => DiscardFromHand(card.pv.ViewID, -1));
        DoFunction(() => GainLoseCoin(-1 * card.coinCost, logged));

        //void youPlayedCard() => Manager.instance.ResolveAbilities(nameof(PlayedCard), PlayedCard.CheckParameters(this, card.dataFile), logged);
        //AddDecisionReact(youPlayedCard, true);
        AddDecisionReact(action, false);
        card.OnPlayEffect(this, logged);
    }

    #endregion

#region Decisions

    public void ChooseButton(List<string> possibleChoices, string changeInstructions, Action action)
    {
        if (action != null) Manager.instance.Instructions(changeInstructions);
        Popup popup = Instantiate(CarryVariables.instance.textPopup);
        popup.StatsSetup(this, "Choices", Vector3.zero);

        for (int i = 0; i < possibleChoices.Count; i++)
            popup.AddTextButton(possibleChoices[i]);

        AddDecisionReact(() => Destroy(popup.gameObject), action != null);
        AddDecisionReact(action, false);
        popup.WaitForChoice();
    }

    public void ChooseCardFromPopup(List<Card> listOfCards, string changeInstructions, Action action, List<float> alphas = null)
    {
        if (action != null) Manager.instance.Instructions(changeInstructions);
        Popup popup = Instantiate(CarryVariables.instance.cardPopup);
        popup.transform.SetParent(this.transform);
        popup.StatsSetup(this, changeInstructions, Vector3.zero);

        AddDecisionReact(Disable, action != null);
        AddDecisionReact(action, false);

        for (int i = 0; i < listOfCards.Count; i++)
        {
            try
            {
                popup.AddCardButton(listOfCards[i], alphas[i]);
            }
            catch
            {
                popup.AddCardButton(listOfCards[i], 1);
            }
        }

        void Disable()
        {
            if (popup != null)
                Destroy(popup.gameObject);
        }
        popup.WaitForChoice();
    }

    public void ChooseCardOnScreen(List<Card> listOfCards, string changeInstructions, Action action)
    {
        if (action != null) Manager.instance.Instructions(changeInstructions);
        Popup popup = null;
        IEnumerator haveCardsEnabled = KeepCardsOn();

        AddDecisionReact(Disable, action != null);
        AddDecisionReact(action, false);

        if (listOfCards.Count == 0 && action != null)
        {
            PopStack();
        }
        else if (listOfCards.Count == 1 && action != null)
        {
            DecisionMade(0, listOfCards[0]);
        }
        else
        {
            StartCoroutine(haveCardsEnabled);
        }

        IEnumerator KeepCardsOn()
        {
            float elapsedTime = 0f;
            while (elapsedTime < 0.3f)
            {
                for (int j = 0; j < listOfCards.Count; j++)
                {
                    Card nextCard = listOfCards[j];
                    int buttonNumber = j;

                    nextCard.button.onClick.RemoveAllListeners();
                    nextCard.button.interactable = true;
                    nextCard.button.onClick.AddListener(() => DecisionMade(buttonNumber, nextCard));
                    nextCard.border.gameObject.SetActive(true);
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        void Disable()
        {
            StopCoroutine(haveCardsEnabled);
            if (popup != null)
                Destroy(popup.gameObject);

            foreach (Card nextCard in listOfCards)
            {
                nextCard.button.onClick.RemoveAllListeners();
                nextCard.button.interactable = false;
                nextCard.border.gameObject.SetActive(false);
            }
        }
    }

    public void ChooseRow(List<Row> listOfColumns, string changeInstructions, Action action)
    {
        if (action != null) Manager.instance.Instructions(changeInstructions);
        Popup popup = null;
        IEnumerator haveCardsEnabled = KeepCardsOn();

        AddDecisionReact(Disable, action != null);
        AddDecisionReact(action, false);

        if (listOfColumns.Count == 0 && action != null)
        {
            PopStack();
        }
        else if (listOfColumns.Count == 1 && action != null)
        {
            DecisionMade(Manager.instance.allRows.IndexOf(listOfColumns[0]));
        }
        else
        {
            StartCoroutine(haveCardsEnabled);
        }

        IEnumerator KeepCardsOn()
        {
            float elapsedTime = 0f;
            while (elapsedTime < 0.3f)
            {
                for (int j = 0; j < listOfColumns.Count; j++)
                {
                    Button nextButton = listOfColumns[j].button;

                    nextButton.onClick.RemoveAllListeners();
                    nextButton.interactable = true;
                    nextButton.onClick.AddListener(() => DecisionMade(Manager.instance.allRows.IndexOf(listOfColumns[j])));
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        void Disable()
        {
            StopCoroutine(haveCardsEnabled);
            if (popup != null)
                Destroy(popup.gameObject);

            foreach (Row nextColumn in Manager.instance.allRows)
            {
                nextColumn.button.onClick.RemoveAllListeners();
                nextColumn.button.interactable = false;
            }
        }
    }

    public void ChooseSlider(int min, int max, string changeInstructions, Action action)
    {
        SliderChoice slider = Instantiate(CarryVariables.instance.sliderPopup);
        slider.StatsSetup(this, "Choose a number.", min, max, new(0, -1000));

        AddDecisionReact(() => Destroy(slider.gameObject), true);
        AddDecisionReact(action, false);
        Manager.instance.Instructions(changeInstructions);
    }

    public void PopStack()
    {
        List<Action> next = decisionReact.Pop();
        foreach (Action action in next)
            action();
    }

    public void DecisionMade(int value)
    {
        choice = value;
        chosenCard = null;
        PopStack();
    }

    public void DecisionMade(int value, Card card)
    {
        choice = value;
        chosenCard = card;
        PopStack();
    }

    public void AddDecisionReact(Action action, bool newTrigger)
    {
        if (action == null)
            return;
        if (decisionReact.Count == 0 || newTrigger)
            decisionReact.Push(new());
        decisionReact.Peek().Add(action);
    }

    #endregion

#region Helpers

    public bool InControl()
    {
        if (PhotonNetwork.IsConnected)
            return this.pv.AmOwner;
        else
            return true;
    }

    void MoveScreen()
    {
        foreach (Transform transform in Manager.instance.storePlayers)
            transform.localPosition = new(0, -10000);
        this.transform.localPosition = Vector3.zero;
    }

    #endregion

}
