using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections.Generic;
using System;

public enum StatusEffect { Shielded, Stunned }

public class MovingTroop : Entity
{

#region Setup

    TMP_Text powerText;
    TMP_Text heartText;
    TMP_Text statusText;

    int myHealth;
    int myPower;
    public int calcHealth { get; private set; }
    public int calcPower { get; private set; }

    public Dictionary<StatusEffect, bool> statusDict = new();

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        heartText = this.transform.Find("Heart Text").GetComponent<TMP_Text>();
        powerText = this.transform.Find("Power Text").GetComponent<TMP_Text>();
        statusText = this.transform.Find("Status Text").GetComponent<TMP_Text>();
        foreach (StatusEffect value in Enum.GetValues(typeof(StatusEffect)))
            statusDict.Add(value, false);
    }

    [PunRPC]
    internal void AssignCardInfo(bool undo, int playerPosition, int cardID)
    {
        if (!undo)
        {
            this.player = Manager.inst.playersInOrder[playerPosition];
            this.image.transform.localScale = new(playerPosition == 0 ? 1 : -1, 1, 1);
            if (border != null)
                border.color = this.player.playerPosition == 0 ? Color.white : Color.black;

            if (cardID >= 0)
            {
                myCard = PhotonView.Find(cardID).GetComponent<TroopCard>();
                this.name = myCard.name;
                this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");

                TroopCard intoTroop = (TroopCard)myCard;
                this.myHealth = intoTroop.health;
                this.myPower = intoTroop.power;
            }
        }
    }

    #endregion

#region Gameplay

    public void ChangeStatsRPC(int power, int health, int logged, string source = "")
    {
        string parathentical = source == "" ? "" : $" (to {source})";
        if (statusDict[StatusEffect.Shielded])
        {
            if (power < 0)
            {
                Log.inst.AddText($"{player.name}'s {this.name} is Shielded from losing {Mathf.Abs(power)} Power{parathentical}.", logged);
                power = 0;
            }
            if (health < 0)
            {
                Log.inst.AddText($"{player.name}'s {this.name} is Shielded from losing {Mathf.Abs(health)} Health{parathentical}.", logged);
                health = 0;
            }
        }
        Log.inst.RememberStep(this, StepType.Revert, () => ChangeStats(false, power, health, logged, source));
    }

    [PunRPC]
    void ChangeStats(bool undo, int power, int health, int logged, string source)
    {
        string parathentical = source == "" ? "" : $" ({source})";
        if (undo)
        {
            myPower -= power;
            myHealth -= health;
        }
        else
        {
            myPower += power;
            if (power > 0)
                Log.inst.AddText($"{player.name}'s {this.name} gets +{power} Power{parathentical}.", logged);
            else if (power < 0)
                Log.inst.AddText($"{player.name}'s {this.name} loses {Mathf.Abs(power)} Power{parathentical}.", logged);

            myHealth += health;
            if (health > 0)
                Log.inst.AddText($"{player.name}'s {this.name} gets +{health} Health{parathentical}.", logged);
            else if (health < 0)
                Log.inst.AddText($"{player.name}'s {this.name} loses {Mathf.Abs(health)} Health{parathentical}.", logged);
        }
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        calcPower = myPower;
        calcHealth = myHealth;

        (int troopPower, int troopHealth) = myCard.PassiveStats(this);
        calcPower += (troopPower < 0 && statusDict[StatusEffect.Shielded]) ? 0 : troopPower;
        calcHealth += (troopHealth < 0 && statusDict[StatusEffect.Shielded]) ? 0 : troopHealth;

        statusText.text = "";
        if (statusDict[StatusEffect.Stunned])
            statusText.text += "StunImage";
        if (statusDict[StatusEffect.Shielded])
            statusText.text += "ShieldedImage";
        statusText.text = KeywordTooltip.instance.EditText(statusText.text);

        MovingAura enviro = Manager.inst.allRows[currentRow].auraHere;
        if (enviro != null)
        {
            (int enviroPower, int enviroHealth) = enviro.myCard.PassiveStats(this, enviro);
            calcPower += (enviroPower < 0 && statusDict[StatusEffect.Shielded]) ? 0 : enviroPower; ;
            calcHealth += (enviroHealth < 0 && statusDict[StatusEffect.Shielded]) ? 0 : enviroHealth;
        }

        powerText.text = calcPower.ToString();
        heartText.text = calcHealth.ToString();
    }

    public void Attack(int logged)
    {
        RecalculateStats();
        Player opposingPlayer = Manager.inst.OpposingPlayer(this.player);
        MovingTroop opposingTroop = Manager.inst.FindOpposingTroop(this.player, this.currentRow);

        if (statusDict[StatusEffect.Stunned])
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} can't attack (it's Stunned).", logged);
            this.StatusEffectRPC(StatusEffect.Stunned, false, logged+1, "");
        }
        else if (calcPower <= 0)
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} can't attack (it has {calcPower} Power).", logged);
        }
        else if (opposingTroop != null)
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} attacks {opposingPlayer.name}'s {opposingTroop.name}.", logged);
            opposingTroop.ChangeStatsRPC(0, -this.calcPower, logged + 1, "");
            foreach ((Card card, Entity entity) in Manager.inst.GatherAbilities())
                card.CardAttacked(entity, this, opposingTroop, logged+1);
        }
        else
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} attacks {opposingPlayer.name}.", logged);
            opposingPlayer.myBase.ChangeHealthRPC(-this.calcPower, logged + 1, "");
            foreach ((Card card, Entity entity) in Manager.inst.GatherAbilities())
                card.CardAttacked(entity, this, opposingPlayer.myBase, logged+1);
        }
    }

    #endregion

#region Misc

    public override void MoveEntityRPC(int newPosition, int logged)
    {
        int oldRow = this.currentRow;
        Log.inst.RememberStep(this, StepType.Revert, () => MoveTroop(false, oldRow, newPosition, logged));
    }

    [PunRPC]
    void MoveTroop(bool undo, int oldPosition, int newPosition, int logged)
    {
        if (undo)
        {
            if (newPosition > -1)
                Manager.inst.allRows[newPosition].playerTroops[player.playerPosition] = null;

            this.currentRow = oldPosition;
        }
        else
        {
            if (oldPosition > -1)
                Manager.inst.allRows[oldPosition].playerTroops[player.playerPosition] = null;

            this.currentRow = newPosition;
            if (currentRow >= 0)
                Log.inst.AddText($"{player.name} moves {this.name} to row {newPosition + 1}.", logged);
            else
                Log.inst.AddText($"{player.name}'s {this.name} is destroyed.", logged);
        }

        if (currentRow > -1)
        {
            player.availableTroops.Remove(this);
            Row spawnPoint = Manager.inst.allRows[currentRow];
            spawnPoint.playerTroops[player.playerPosition] = this;

            this.transform.SetParent(spawnPoint.button.transform);
            this.transform.localPosition = new((player.playerPosition) == 0 ? -575 : 575, 0);
            this.transform.localScale = Vector3.one;
            RecalculateStats();
        }
        else
        {
            player.availableTroops.Add(this);
            this.transform.SetParent(null);
        }
    }

    public void StatusEffectRPC(StatusEffect status, bool newStatus, int logged, string source = "")
    {
        Log.inst.RememberStep(this, StepType.Revert, () => ChangeStatus(false, (int)status, newStatus, logged, source));
    }

    [PunRPC]
    void ChangeStatus(bool undo, int statusNumber, bool newStatus, int logged, string source)
    {
        StatusEffect toChange = (StatusEffect)statusNumber;
        string parathentical = source == "" ? "" : $" ({source})";
        if (undo)
        {
            statusDict[toChange] = !newStatus;
        }
        else
        {
            statusDict[toChange] = newStatus;
            if (newStatus)
                Log.inst.AddText($"{player.name}'s {this.name} is {toChange}{parathentical}.", logged);
            else
                Log.inst.AddText($"{player.name}'s {this.name} is no longer {toChange}{parathentical}.", logged);
        }
        RecalculateStats();
    }

    #endregion

}
