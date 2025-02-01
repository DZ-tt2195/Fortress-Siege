using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using MyBox;
using System;
using System.Linq.Expressions;

public enum StepType { UndoPoint, Revert, None }
[Serializable] public class NextStep
{
    public StepType stepType { get; private set; }
    [TextArea(5, 5)] public string actionName { get; private set; }
    public PhotonCompatible source { get; private set; }
    public Expression<Action> action { get; private set; }
    public bool completed = false;

    internal NextStep(PhotonCompatible source, StepType stepType, Expression<Action> action)
    {
        this.source = source;
        this.action = action;
        this.actionName = $"{action.ToString().Replace("() => ", "")}";
        ChangeType(stepType);
    }

    internal void ChangeType(StepType stepType)
    {
        this.stepType = stepType;
        completed = stepType != StepType.UndoPoint;
    }
}

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
        Debug.Log($"new chain at {toThisPoint.actionName}: {PrintDecisions()}");
        this.toThisPoint = toThisPoint;
    }
}

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
    public List<MovingTroop> availableTroops = new();

    [Foldout("UI", true)]
    [SerializeField] TMP_Text resourceText;
    Button resignButton;
    Transform keepHand;

    [Foldout("Choices", true)]
    public int choice { get; private set; }
    public List<Action> inReaction = new();

    [Foldout("Undo", true)]
    [SerializeField] List<NextStep> historyStack = new();
    int currentDecisionInStack = -1;
    public NextStep currentStep { get; private set; }

    [Foldout("AI", true)]
    List<DecisionChain> chainsToResolve = new();
    List<DecisionChain> finishedChains = new();
    public DecisionChain currentChain { get; private set; }
    public int chainTracker { get; private set; }

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

        for (int i = 0; i < 5; i++)
        {
            GameObject nextTroop = Manager.instance.MakeObject(CarryVariables.instance.movingTroopPrefab.gameObject);
            DoFunction(() => AddTroop(nextTroop.GetComponent<PhotonView>().ViewID));
        }

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

    [PunRPC]
    void AddTroop(int PV)
    {
        GameObject obj = PhotonView.Find(PV).gameObject;
        obj.AddComponent(Type.GetType("MovingTroop"));
        availableTroops.Add(obj.GetComponent<MovingTroop>());
    }

    #endregion

#region Hand

    [PunRPC]
    public void FindCardsFromDeck(int cardsToDraw, int logged)
    {
        for (int i = 0; i < cardsToDraw; i++)
        {
            Card card = Manager.instance.deck.GetChild(0).GetComponent<Card>();
            card.transform.SetParent(null);
            DoFunction(() => SendPlayerCardToAsker(card.pv.ViewID, logged), this.realTimePlayer);
        }
    }

    [PunRPC]
    internal void SendPlayerCardToAsker(int PV, int logged)
    {
        RememberStep(this, StepType.Revert, () => AddToHand(false, PV, logged));
    }

    [PunRPC]
    void AddToHand(bool undo, int PV, int logged)
    {
        if (undo)
        {
            DoFunction(() => ReturnPlayerCardToDeck(PV), RpcTarget.MasterClient);
            SortHand();
        }
        else
        {
            Card card = PhotonView.Find(PV).GetComponent<Card>();
            PutInHand(card);

            if (InControl())
                Log.instance.AddText($"{this.name} draws {card.name}.", logged);
            else
                Log.instance.AddText($"{this.name} draws 1 Card.", logged);
        }
    }

    [PunRPC]
    void ReturnPlayerCardToDeck(int PV)
    {
        Card card = PhotonView.Find(PV).GetComponent<Card>();
        cardsInHand.Remove(card);
        card.transform.SetParent(Manager.instance.deck);
        card.transform.SetAsFirstSibling();
        StartCoroutine(card.MoveCard(new(0, -10000), 0.25f, Vector3.one));
    }

    void PutInHand(Card card)
    {
        cardsInHand.Add(card);
        card.transform.localPosition = new Vector2(0, -1100);
        card.layout.FillInCards(card);
        card.layout.cg.alpha = 0;
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

    public void DiscardPlayerCard(Card card, int logged)
    {
        RememberStep(this, StepType.Revert, () => DiscardFromHand(false, card.pv.ViewID, logged));
    }

    [PunRPC]
    void DiscardFromHand(bool undo, int PV, int logged)
    {
        Card card = PhotonView.Find(PV).GetComponent<Card>();

        if (undo)
        {
            PutInHand(card);
        }
        else
        {
            cardsInHand.Remove(card);
            card.transform.SetParent(Manager.instance.discard);
            Log.instance.AddText($"{this.name} discards {card.name}.", logged);
            StartCoroutine(card.MoveCard(new(0, -10000), 0.25f, Vector3.one));
        }
        SortHand();
    }

    [PunRPC]
    public void GainLoseCoin(bool undo, int amount, int logged)
    {
        if (undo)
        {
            coins -= amount;
        }
        else
        {
            coins += amount;
            if (amount >= 0)
                Log.instance.AddText($"{this.name} gains ${amount}.", logged);
            else
                Log.instance.AddText($"{this.name} loses ${Mathf.Abs(amount)}.", logged);
        }
        if (myBase != null)
            myBase.UpdateText();
    }

    #endregion

#region Main Turn

    [PunRPC]
    public void YourTurn()
    {
        historyStack.Clear();
        chainsToResolve.Clear();
        finishedChains.Clear();
        chainTracker = -1;

        Manager.instance.DoFunction(() => Manager.instance.Instructions($"Waiting on {this.name}..."), RpcTarget.Others);
        PreserveTextRPC("");
        PreserveTextRPC($"{this.name}'s turn");
        RememberStep(this, StepType.UndoPoint, () => MayPlayCard());

        if (myType == PlayerType.Computer)
        {
            currentChain = new(historyStack[0]);
            chainsToResolve.Add(currentChain);
            StartCoroutine(SimulateGame());
        }
        else
        {
            PopStack();
        }

        IEnumerator SimulateGame()
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
            chainTracker = -1;

            string answer = $"{currentChain.math} -> ";
            foreach (int nextInt in currentChain.decisions)
                answer += $"{nextInt} ";
            Debug.Log(answer);

            finishedChains.Clear();
            RememberStep(this, StepType.UndoPoint, () => MayPlayCard());
            PopStack();
        }
    }

    internal void MayPlayCard()
    {
        List<string> actions = new() { $"End Turn" };
        List<Card> canPlay = cardsInHand.Where(card => card.CanPlayMe(this, true)).ToList();

        if (myType == PlayerType.Computer)
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
            ChooseButton(actions, Vector3.zero, (canPlay.Count) == 0 ? "You can't play any cards." : "What to play?", ActionResolution);
            ChooseCardOnScreen(canPlay, (canPlay.Count) == 0 ? "You can't play any cards." : "What to play?", null);
        }
        void ActionResolution()
        {
            try
            {
                Card toPlay = canPlay[choice - 100];
                PreserveTextRPC($"{this.name} plays {toPlay.name}.", SimulatedLog(0));
                DiscardPlayerCard(toPlay, -1);
                RememberStep(this, StepType.Revert, () => GainLoseCoin(false, -1 * toPlay.coinCost, SimulatedLog(0)));
                toPlay.OnPlayEffect(this, 0);
            }
            catch
            {
                if (myType == PlayerType.Computer && !currentChain.complete)
                {
                    currentChain.complete = true;
                    chainsToResolve.Remove(currentChain);
                    finishedChains.Add(currentChain);

                    Manager.instance.SimulateBattle(this);

                    currentChain.math = PlayerScore(this.playerPosition) - PlayerScore(Manager.instance.OtherPlayer(this).playerPosition);
                    Debug.Log($"CHAIN ENDED with score {currentChain.math}. decisions: {currentChain.PrintDecisions()}");
                    currentChain = null;

                    float PlayerScore(int playerPosition)
                    {
                        Player player = Manager.instance.playersInOrder[playerPosition];
                        int answer = player.myBase.currentHealth + player.cardsInHand.Count * 2;
                        if (playerPosition == this.playerPosition)
                            answer -= coins*2;

                        foreach (Row row in Manager.instance.allRows)
                        {
                            MovingTroop troop = row.playerTroops[playerPosition];
                            if (troop != null && troop.currentHealth >= 1)
                                answer += troop.currentDamage + troop.currentHealth;
                        }

                        if (player.myBase.currentHealth <= 0)
                            return -Mathf.Infinity;
                        else
                            return answer;
                    }
                }
                else
                {
                    PreserveTextRPC($"{this.name} ends their turn.");
                    Manager.instance.DoFunction(() => Manager.instance.Continue());
                }
            }
        }
    }

    #endregion

#region Make Decision

    public void ChooseButton(List<string> possibleChoices, Vector2 position, string changeInstructions, Action action)
    {
        Popup popup = Instantiate(CarryVariables.instance.textPopup);
        popup.StatsSetup(this, changeInstructions, position);

        for (int i = 0; i < possibleChoices.Count; i++)
            popup.AddTextButton(possibleChoices[i]);

        inReaction.Add(() => Destroy(popup.gameObject));
        if (action != null)
        {
            inReaction.Add(action);
            Manager.instance.Instructions(changeInstructions);
        }
    }

    public void ChooseCardOnScreen(List<Card> listOfCards, string changeInstructions, Action action)
    {
        IEnumerator haveCardsEnabled = KeepCardsOn();
        inReaction.Add(Disable);
        if (action != null)
        {
            inReaction.Add(action);
            Manager.instance.Instructions(changeInstructions);
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
        IEnumerator haveCardsEnabled = KeepCardsOn();
        inReaction.Add(Disable);
        if (action != null)
        {
            inReaction.Add(action);
            Manager.instance.Instructions(changeInstructions);
        }

        if (listOfRows.Count == 0 && action != null)
            PopStack();
        else if (listOfRows.Count == 1 && action != null)
            DecisionMade(0);
        else
            StartCoroutine(haveCardsEnabled);

        IEnumerator KeepCardsOn()
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
            StopCoroutine(haveCardsEnabled);

            foreach (Row nextRow in listOfRows)
            {
                nextRow.button.onClick.RemoveAllListeners();
                nextRow.button.interactable = false;
            }
        }
    }

    public void ChooseSlider(int min, int max, string changeInstructions, Action action)
    {
        SliderChoice slider = Instantiate(CarryVariables.instance.sliderPopup);
        slider.StatsSetup(this, "Choose a number.", min, max, new(0, -1000));

        inReaction.Add(() => Destroy(slider.gameObject));
        inReaction.Add(action);
        Manager.instance.Instructions(changeInstructions);
    }

    #endregion

#region Resolve Decision

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
        if (myType == PlayerType.Human)
            return;

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
                Debug.Log($"switched chains (undo {needUndo}), {currentChain.toThisPoint.actionName}. decisions: {currentChain.PrintDecisions()}");
                break;
            }
        }

        if (needUndo)
            UndoAmount(currentStep);
    }

    public void PopStack()
    {
        if (currentDecisionInStack >= 0)
        {
            int number = currentDecisionInStack;
            RememberStep(this, StepType.Revert, () => DecisionComplete(false, number));
        }

        List<Action> newActions = new();
        for (int i = 0; i < inReaction.Count; i++)
            newActions.Add(inReaction[i]);

        inReaction.Clear();
        foreach (Action action in newActions)
            action();

        if (currentChain == null)
            FindNewestChain();

        for (int i = historyStack.Count - 1; i >= 0; i--)
        {
            NextStep step = historyStack[i];
            if (step.stepType == StepType.UndoPoint && !step.completed)
            {
                currentStep = step;
                currentDecisionInStack = i;
                if (currentChain != null)
                {
                    currentChain.toThisPoint = step;
                    chainTracker++;
                    Debug.Log($"tracker bumped up to {chainTracker}");
                }
                Debug.Log($"run {step.actionName}");
                step.action.Compile().Invoke();
                break;
            }
        }
        //Debug.LogError($"{currentChain.toThisPoint.actionName} -> failed to find new undo point");
    }

    [PunRPC]
    void DecisionComplete(bool undo, int stepNumber)
    {
        NextStep step = historyStack[stepNumber];
        if (undo)
        {
            step.completed = false;
            Debug.Log($"turned off: {stepNumber}, {step.actionName}");
        }
        else
        {
            step.completed = true;
            Debug.Log($"turned on: {stepNumber}, {step.actionName}");
        }
    }

    public void DecisionMade(int value)
    {
        choice = value;
        //Debug.Log($"made choice of {value}");
        //currentStep = null;
        PopStack();
    }

    #endregion

#region Steps

    public void RememberStep(PhotonCompatible source, StepType type, Expression<Action> action)
    {
        NextStep newStep = new(source, type, action);
        historyStack.Add(newStep);

        //Debug.Log($"step {currentStep}: {action}");
        if (type != StepType.UndoPoint)
        {
            if (myType == PlayerType.Computer && currentChain != null && currentChain.complete)
                newStep.action.Compile().Invoke();
            else
                newStep.source.DoFunction(newStep.action, RpcTarget.All);
        }
    }

    internal void UndoAmount(NextStep toThisPoint)
    {
        Popup[] allPopups = FindObjectsByType<Popup>(FindObjectsSortMode.None);
        foreach (Popup popup in allPopups)
            Destroy(popup.gameObject);

        Card[] allCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (Card card in allCards)
        {
            card.button.interactable = false;
            card.button.onClick.RemoveAllListeners();
            card.border.gameObject.SetActive(false);
        }

        for (int i = historyStack.Count - 1; i >= 0; i--)
        {
            NextStep next = historyStack[i];

            if (next.stepType == StepType.Revert)
            {
                Debug.Log($"undo step {i}: {next.actionName}");
                (string instruction, object[] parameters) = next.source.TranslateFunction(next.action);

                object[] newParameters = new object[parameters.Length];
                newParameters[0] = true;
                for (int j = 1; j < parameters.Length; j++)
                    newParameters[j] = parameters[j];

                next.source.StringParameters(instruction, newParameters);
                historyStack.RemoveAt(i);
            }
            else if (next.stepType == StepType.UndoPoint)
            {
                chainTracker--;

                if (next == toThisPoint || i == 0)
                {
                    Debug.Log($"continue at {i}, reduce tracker to {chainTracker}, {next.actionName}");
                    break;
                }
                else
                {
                    historyStack.RemoveAt(i);
                    Debug.Log($"delete step {i}, reduce tracker to {chainTracker}: {next.actionName}");
                }
            }
        }
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

    public List<Row> FilterRows(bool hasTroop)
    {
        List<Row> answer = new();
        foreach (Row row in Manager.instance.allRows)
        {
            MovingTroop troop = row.playerTroops[this.playerPosition];
            if (hasTroop && troop != null && troop.currentHealth >= 1)
                answer.Add(row);
            else if (!hasTroop)
                answer.Add(row);
        }
        return answer;
    }

    public void PreserveTextRPC(string text, int logged = 0)
    {
        RememberStep(this, StepType.Revert, () => TextShared(false, text, logged));
    }

    [PunRPC]
    void TextShared(bool undo, string text, int logged)
    {
        if (!undo)
            Log.instance.AddText(text, logged);
    }

    public int SimulatedLog(int logged)
    {
        if (currentChain == null || currentChain.complete)
            return logged;
        else
            return -1;
    }

    #endregion

}
