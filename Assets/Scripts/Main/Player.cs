using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using MyBox;
using System;
using System.Text.RegularExpressions;

[Serializable] public class DecisionChain
{
    public bool complete = false;
    public List<int> decisions;
    public float math = 0;
    public NextStep toThisPoint;

    public string PrintDecisions()
    {
        string answer = "";
        foreach (int next in this.decisions)
            answer += $"{next}, ";
        return answer;
    }

    public DecisionChain(NextStep toThisPoint)
    {
        complete = false;
        decisions = new();
        this.toThisPoint = toThisPoint;
    }

    public DecisionChain(List<int> oldList, int toAdd, NextStep toThisPoint)
    {
        complete = false;
        decisions = new(oldList) {toAdd};
        //Debug.Log($"new chain at {toThisPoint.actionName}: {PrintDecisions()}");
        this.toThisPoint = toThisPoint;
    }
}

public enum PlayerType { Human, Bot }

public class Player : PhotonCompatible
{

#region Variables

    [Foldout("Player info", true)]
    [ReadOnly] public int playerPosition;
    public PlayerType myType { get; private set; }
    public Photon.Realtime.Player realTimePlayer { get; private set; }
    public int coins { get; private set; }
    public PlayerBase myBase { get; private set; }
    public List<MovingTroop> availableTroops = new();
    public List<MovingAura> availableEnviros = new();

    [Foldout("Cards", true)]
    public List<Card> cardsInHand = new();
    [SerializeField] Transform deck;

    [Foldout("UI", true)]
    [SerializeField] TMP_Text resourceText;
    Button resignButton;
    Transform keepHand;

    [Foldout("Choices", true)]
    public int choice { get; private set; }
    public List<Action> inReaction = new();
    public NextStep currentStep { get; private set; }

    [Foldout("AI", true)]
    List<DecisionChain> chainsToResolve = new();
    List<DecisionChain> finishedChains = new();
    public DecisionChain currentChain { get; private set; }
    public int chainTracker;

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
            if (PhotonNetwork.CurrentRoom.MaxPlayers == 1 && Manager.inst.storePlayers.childCount == 0)
                DoFunction(() => SendName("Bot"), RpcTarget.AllBuffered);
            else
                DoFunction(() => SendName(PlayerPrefs.GetString("Online Username")), RpcTarget.AllBuffered);

            for (int i = 0; i < 5; i++)
            {
                GameObject nextTroop = Manager.inst.MakeObject(CarryVariables.inst.movingTroopPrefab);
                DoFunction(() => AddTroop(nextTroop.GetComponent<PhotonView>().ViewID));
                GameObject nextEnviro = Manager.inst.MakeObject(CarryVariables.inst.environmentPrefab);
                DoFunction(() => AddEnviro(nextEnviro.GetComponent<PhotonView>().ViewID));
            }

            List<string> newList = new();
            newList.AddRange(CarryVariables.inst.cardScripts);
            List<string> shuffledCards = newList.Shuffle();

            for (int i = 0; i < shuffledCards.Count; i++)
            {
                int nextPosition = i;
                GameObject next = Manager.inst.MakeObject(CarryVariables.inst.cardPrefab.gameObject);
                DoFunction(() => AddCard(i, next.GetComponent<PhotonView>().ViewID, shuffledCards[i]), RpcTarget.AllBuffered);
            }
        }
    }

    [PunRPC]
    void AddCard(int position, int ID, string cardName)
    {
        GameObject nextObject = PhotonView.Find(ID).gameObject;
        nextObject.transform.SetParent(deck);
        nextObject.transform.SetSiblingIndex(position);
        nextObject.transform.localPosition = new(0, -10000);

        Type type = Type.GetType(cardName);
        nextObject.AddComponent(type);
        nextObject.name = Regex.Replace(cardName, "(?<=[a-z])(?=[A-Z])", " ");
        Card card = nextObject.GetComponent<Card>();
        card.layout.FillInCards(card);
    }

    [PunRPC]
    void SendName(string username)
    {
        pv.Owner.NickName = username;
        this.name = username;
        this.transform.SetParent(Manager.inst.storePlayers);
    }

    internal void AssignInfo(int position, PlayerType type)
    {
        this.playerPosition = position;
        this.myType = type;
        Manager.inst.storePlayers.transform.localScale = Manager.inst.canvas.transform.localScale;
        this.transform.localPosition = Vector3.zero;
        if (PhotonNetwork.IsConnected)
            realTimePlayer = PhotonNetwork.PlayerList[pv.OwnerActorNr - 1];

        if (InControl())
        {
            GameObject obj = Manager.inst.MakeObject(CarryVariables.inst.playerBasePrefab.gameObject);
            DoFunction(() => GetBase(obj.GetComponent<PhotonView>().ViewID), RpcTarget.All);
            if (this.myType == PlayerType.Human)
            {
                resignButton.onClick.AddListener(() => Manager.inst.DoFunction(() => Manager.inst.DisplayEnding(this.playerPosition), RpcTarget.All));
                Invoke(nameof(MoveScreen), 0.2f);
                pv.Owner.NickName = this.name;
            }

            DrawCardRPC(4, -1);
            Manager.inst.DoFunction(() => Manager.inst.PlayerDone());
        }
    }

    [PunRPC]
    void GetBase(int PV)
    {
        myBase = PhotonView.Find(PV).gameObject.GetComponent<PlayerBase>();
        myBase.AssignPlayer(this, 20);
    }

    [PunRPC]
    void AddTroop(int PV)
    {
        GameObject obj = PhotonView.Find(PV).gameObject;
        obj.AddComponent(Type.GetType("MovingTroop"));
        availableTroops.Add(obj.GetComponent<MovingTroop>());
    }

    [PunRPC]
    void AddEnviro(int PV)
    {
        GameObject obj = PhotonView.Find(PV).gameObject;
        obj.AddComponent(Type.GetType("MovingAura"));
        availableEnviros.Add(obj.GetComponent<MovingAura>());
    }

    #endregion

#region Cards

    public void DrawCardRPC(int cardAmount, int logged, string source = "")
    {
        for (int i = 0; i < cardAmount; i++)
        {
            Card card = deck.GetChild(0).GetComponent<Card>();
            Log.inst.RememberStep(this, StepType.Revert, () => DrawFromDeck(false, card.pv.ViewID, logged, source));
        }
    }

    [PunRPC]
    void DrawFromDeck(bool undo, int PV, int logged, string source)
    {
        Card card = PhotonView.Find(PV).GetComponent<Card>();
        string parathentical = source == "" ? "" : $" ({source})";

        if (undo)
        {
            cardsInHand.Remove(card);
            card.transform.SetParent(deck.transform);
            card.transform.SetAsFirstSibling();
            StartCoroutine(card.MoveCard(new(0, -10000), 0.25f, Vector3.one));
        }
        else
        {
            PutCardInHand(card);
            if (InControl() && myType == PlayerType.Human)
                Log.inst.AddText($"{this.name} draws {card.name}{parathentical}.", logged);
            else
                Log.inst.AddText($"{this.name} draws 1 Card{parathentical}.", logged);
        }
        SortHand();
    }

    public void PutCardInHand(Card card)
    {
        cardsInHand.Add(card);
        card.transform.localPosition = new Vector2(0, -1100);
        card.layout.FillInCards(card);
        card.layout.cg.alpha = 0;
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
        cardsInHand = cardsInHand.OrderBy(card => card.coinCost).ToList();

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

    public void DiscardPlayerCard(Card card, int logged)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => DiscardFromHand(false, card.pv.ViewID, logged));
    }

    [PunRPC]
    void DiscardFromHand(bool undo, int PV, int logged)
    {
        Card card = PhotonView.Find(PV).GetComponent<Card>();

        if (undo)
        {
            PutCardInHand(card);
        }
        else
        {
            cardsInHand.Remove(card);
            card.transform.SetParent(null);
            Log.inst.AddText($"{this.name} discards {card.name}.", logged);
            StartCoroutine(card.MoveCard(new(0, -10000), 0.25f, Vector3.one));
        }
        SortHand();
    }

    public void BounceCardRPC(Entity entity, int logged, string source = "")
    {
        entity.MoveEntityRPC(-1, -1);
        Log.inst.RememberStep(this, StepType.Revert, () => BounceCard(false, entity.myCard.pv.ViewID, logged, source));
    }

    [PunRPC]
    void BounceCard(bool undo, int PV, int logged, string source)
    {
        string parathentical = source == "" ? "" : $" ({source})";
        Card card = PhotonView.Find(PV).GetComponent<Card>();
        if (undo)
        {
            cardsInHand.Remove(card);
            card.transform.localPosition = new Vector2(0, -1100);
            card.transform.SetParent(null);
        }
        else
        {
            Log.inst.AddText($"{this.name}'s {card.name} gets Bounced{parathentical}.", logged);
            PutCardInHand(card);
        }
        SortHand();
    }

    #endregion

#region Turn

    public void CoinRPC(int amount, int logged, string source = "")
    {
        Log.inst.RememberStep(this, StepType.Revert, () => GainLoseCoin(false, amount, logged, source));
    }

    [PunRPC]
    void GainLoseCoin(bool undo, int amount, int logged, string source)
    {
        string parathentical = source == "" ? "" : $" ({source})";
        if (undo)
        {
            coins -= amount;
        }
        else
        {
            coins += amount;
            if (amount >= 0)
                Log.inst.AddText($"{this.name} gains {amount} Coin{parathentical}.", logged);
            else
                Log.inst.AddText($"{this.name} loses {Mathf.Abs(amount)} Coin{parathentical}.", logged);
        }
        if (myBase != null)
            myBase.UpdateText();
    }

    [PunRPC]
    internal void YourTurn()
    {
        Log.inst.historyStack.Clear();
        Log.inst.currentDecisionInStack = -1;

        chainsToResolve.Clear();
        finishedChains.Clear();
        chainTracker = -1;

        Manager.inst.DoFunction(() => Manager.inst.Instructions($"Waiting on {this.name}..."));
        Manager.inst.DoFunction(() => Manager.inst.SetCurrentPlayer(this.playerPosition));

        Log.inst.DoFunction(() => Log.inst.AddText("", 0));
        Log.inst.DoFunction(() => Log.inst.AddText($"{this.name}'s turn", 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => MayPlayCard());

        if (myType == PlayerType.Bot)
        {
            currentChain = new(Log.inst.historyStack[0]);
            chainsToResolve.Add(currentChain);
            StartCoroutine(FindAIRoute());
        }
        else
        {
            PopStack();
        }
    }

    IEnumerator FindAIRoute()
    {
        yield return new WaitForSeconds(1f);
        PopStack();

        while (chainsToResolve.Count > 0)
        {
            yield return null;
        }

        Debug.Log($"{finishedChains.Count} chains finished");
        finishedChains = finishedChains.Shuffle();
        currentChain = finishedChains.OrderByDescending(chain => chain.math).FirstOrDefault();

        string answer = $"Best chain: {currentChain.math} -> ";
        foreach (int nextInt in currentChain.decisions)
            answer += $"{nextInt} ";
        Debug.Log(answer);

        finishedChains.Clear();
        Log.inst.InvokeUndo(Log.inst.historyStack[0]);
        Log.inst.RememberStep(this, StepType.UndoPoint, () => MayPlayCard());
        chainTracker = -1;
        PopStack();
    }

    internal void MayPlayCard()
    {
        Manager.inst.CleanUp(1);
        List<string> actions = new() { $"End Turn" };
        List<Card> canPlay = cardsInHand.Where(card => card.CanPlayMe(this, true)).ToList();

        if (myType == PlayerType.Bot)
        {
            if (chainTracker < currentChain.decisions.Count)
            {
                int next = currentChain.decisions[chainTracker];
                //Debug.Log($"resolved continue turn with choice {next}");
                inReaction.Add(ActionResolution);
                DecisionMade(next);
            }
            else
            {
                //Debug.Log($"{chainTracker}, {currentChain.decisions.Count}");
                NewChains(-1, canPlay.Count, 100);
            }
        }
        else
        {
            ChooseButton(actions, Vector3.zero, (canPlay.Count) == 0 ? "Can't play cards." : "What to play?", ActionResolution);
            ChooseCardOnScreen(canPlay, (canPlay.Count) == 0 ? "You can't play any cards." : "What to play?", null);
        }

        void ActionResolution()
        {
            int convertedChoice = choice - 100;
            if (convertedChoice < canPlay.Count && convertedChoice >= 0)
            {
                Card toPlay = canPlay[convertedChoice];
                Log.inst.PreserveTextRPC($"{this.name} plays {toPlay.name}.", 0);
                DiscardPlayerCard(toPlay, -1);
                Log.inst.RememberStep(this, StepType.Revert, () => GainLoseCoin(false, -1 * toPlay.coinCost, 0, ""));
                toPlay.OnPlayEffect(this, 0);
            }
            else
            {
                if (myType == PlayerType.Bot && !currentChain.complete)
                {
                    FinishChain();
                }
                else
                {
                    Log.inst.PreserveTextRPC($"{this.name} ends their turn.");
                    Log.inst.ShareSteps();
                    Manager.inst.DoFunction(() => Manager.inst.Instructions($""));
                    Manager.inst.DoFunction(() => Manager.inst.Continue());
                }
            }
        }
    }

    void FinishChain()
    {
        currentChain.complete = true;
        chainsToResolve.Remove(currentChain);
        finishedChains.Add(currentChain);

        Manager.inst.SimulateBattle();
        currentChain.math = PlayerScore(this) - PlayerScore(Manager.inst.OpposingPlayer(this));
        Debug.Log($"CHAIN ENDED with score {currentChain.math}. decisions: {currentChain.PrintDecisions()}");
        currentChain = null;

        float PlayerScore(Player player)
        {
            int answer = player.myBase.myHealth + player.cardsInHand.Count * 2;

            foreach ((Card card, Entity entity) in Manager.inst.GatherAbilities())
                answer += card.CoinEffect(player, entity, -1);

            if (player == this)
                answer -= coins * 2;

            foreach (Row row in Manager.inst.allRows)
            {
                MovingTroop troop = row.playerTroops[playerPosition];
                if (troop != null && troop.calcHealth >= 1)
                    answer += troop.calcPower + troop.calcHealth;
            }

            if (player.myBase.myHealth <= 0)
                return -Mathf.Infinity;
            else
                return answer;
        }
    }

    #endregion

#region Decide

    public void ChooseButton(List<string> possibleChoices, Vector2 position, string changeInstructions, Action action)
    {
        Popup popup = Instantiate(CarryVariables.inst.textPopup);
        popup.StatsSetup(this, changeInstructions, position);

        for (int i = 0; i < possibleChoices.Count; i++)
            popup.AddTextButton(possibleChoices[i]);

        inReaction.Add(() => Destroy(popup.gameObject));
        if (action != null)
        {
            inReaction.Add(action);
            Manager.inst.Instructions(changeInstructions);
        }
    }

    public void ChooseCardOnScreen(List<Card> listOfCards, string changeInstructions, Action action)
    {
        IEnumerator haveCardsEnabled = KeepCardsOn();
        inReaction.Add(Disable);
        if (action != null)
        {
            inReaction.Add(action);
            Manager.inst.Instructions(changeInstructions);
        }

        if (listOfCards.Count == 0 && action != null)
            PopStack();
        else if (listOfCards.Count == 1 && action != null)
            DecisionMade(0);
        else
            StartCoroutine(haveCardsEnabled);

        IEnumerator KeepCardsOn()
        {
            float elapsedTime = 0f;
            while (elapsedTime < 0.3f)
            {
                for (int j = 0; j < listOfCards.Count; j++)
                {
                    Card nextCard = listOfCards[j];
                    int buttonNumber = j + 100;

                    nextCard.button.onClick.RemoveAllListeners();
                    nextCard.button.interactable = true;
                    nextCard.button.onClick.AddListener(() => DecisionMade(buttonNumber));
                    nextCard.border.gameObject.SetActive(true);
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        void Disable()
        {
            StopCoroutine(haveCardsEnabled);

            foreach (Card nextCard in listOfCards)
            {
                nextCard.button.onClick.RemoveAllListeners();
                nextCard.button.interactable = false;
                nextCard.border.gameObject.SetActive(false);
            }
        }
    }

    public void ChooseRow(List<Row> listOfRows, string changeInstructions, Action action)
    {
        IEnumerator haveRowsEnabled = KeepRowsOn();
        inReaction.Add(Disable);
        if (action != null)
        {
            inReaction.Add(action);
            Manager.inst.Instructions(changeInstructions);
        }

        if (listOfRows.Count == 0 && action != null)
            PopStack();
        else if (listOfRows.Count == 1 && action != null)
            DecisionMade(0);
        else
            StartCoroutine(haveRowsEnabled);

        IEnumerator KeepRowsOn()
        {
            float elapsedTime = 0f;
            while (elapsedTime < 0.3f)
            {
                for (int j = 0; j < listOfRows.Count; j++)
                {
                    Row nextRow = listOfRows[j];
                    int buttonNumber = j;

                    nextRow.button.onClick.RemoveAllListeners();
                    nextRow.button.interactable = true;
                    nextRow.button.onClick.AddListener(() => DecisionMade(buttonNumber));
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        void Disable()
        {
            StopCoroutine(haveRowsEnabled);

            foreach (Row nextRow in listOfRows)
            {
                nextRow.button.onClick.RemoveAllListeners();
                nextRow.button.interactable = false;
            }
        }
    }

    public void ChooseSlider(int min, int max, string changeInstructions, Action action)
    {
        SliderChoice slider = Instantiate(CarryVariables.inst.sliderPopup);
        slider.StatsSetup(this, "Choose a number.", min, max, new(0, -1000));

        inReaction.Add(() => Destroy(slider.gameObject));
        inReaction.Add(action);
        Manager.inst.Instructions(changeInstructions);
    }

    #endregion

#region Resolve

    public void NewChains(int low, int high, int increment)
    {
        chainsToResolve.Add(new(currentChain.decisions ?? new(), low, currentStep));

        for (int i = low + 1; i < high; i++)
        {
            int remember = i + increment;
            chainsToResolve.Add(new(currentChain?.decisions, remember, currentStep));
        }
        chainsToResolve.Remove(currentChain);

        FindNewestChain();
        currentStep.action.Compile().Invoke();
    }

    void FindNewestChain()
    {
        bool needUndo = false;

        for (int i = chainsToResolve.Count - 1; i >= 0; i--)
        {
            DecisionChain newChain = chainsToResolve[i];
            if (!newChain.complete)
            {
                if (currentChain == null)
                {
                    needUndo = true;
                }
                else
                {
                    for (int j = 0; j < currentChain.decisions.Count; j++)
                    {
                        if (currentChain.decisions[j] != newChain.decisions[j])
                        {
                            needUndo = true;
                            break;
                        }
                    }
                }

                chainsToResolve.RemoveAt(i);
                currentChain = newChain;
                currentStep = newChain.toThisPoint;
                //Debug.Log($"switched chains (undo {needUndo}), {currentChain.toThisPoint.actionName}. decisions: {currentChain.PrintDecisions()}");
                break;
            }
        }

        if (needUndo)
        {
            //Debug.Log($"AI UNDO to {currentStep.actionName}");
            Log.inst.InvokeUndo(currentStep);
        }
    }

    public void PopStack()
    {
        if (Log.inst.currentDecisionInStack >= 0)
        {
            int number = Log.inst.currentDecisionInStack;
            Log.inst.RememberStep(Log.inst, StepType.Revert, () => Log.inst.DecisionComplete(false, number));
        }

        List<Action> newActions = new();
        for (int i = 0; i < inReaction.Count; i++)
            newActions.Add(inReaction[i]);

        inReaction.Clear();
        foreach (Action action in newActions)
            action();

        if (currentChain == null && myType == PlayerType.Bot)
            FindNewestChain();

        for (int i = Log.inst.historyStack.Count - 1; i >= 0; i--)
        {
            NextStep step = Log.inst.historyStack[i];
            if (step.stepType == StepType.UndoPoint && !step.completed)
            {
                currentStep = step;
                Log.inst.currentDecisionInStack = i;
                if (currentChain != null)
                {
                    currentChain.toThisPoint = step;
                    chainTracker++;
                }

                //Debug.Log($"now do: {step.actionName}");
                Log.inst.undoToThis = step;
                step.action.Compile().Invoke();
                break;
            }
        }
    }

    public void DecisionMade(int value)
    {
        choice = value;
        //Debug.Log($"made choice of {value}");
        PopStack();
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
        foreach (Transform transform in Manager.inst.storePlayers)
            transform.localPosition = new(0, -10000);
        this.transform.localPosition = Vector3.zero;
    }

    public List<Row> FilterRows(bool hasTroop)
    {
        List<Row> answer = new();
        foreach (Row row in Manager.inst.allRows)
        {
            MovingTroop troop = row.playerTroops[this.playerPosition];
            if (hasTroop && troop != null && troop.calcHealth >= 1)
                answer.Add(row);
            else if (!hasTroop)
                answer.Add(row);
        }
        return answer;
    }

    #endregion

}
