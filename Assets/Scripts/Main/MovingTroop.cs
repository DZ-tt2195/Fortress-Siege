using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections.Generic;
using System;

public class MovingTroop : Entity
{

#region Setup

    TMP_Text powerText;
    TMP_Text heartText;

    int myHealth;
    int myPower;
    public int calcHealth { get; private set; }
    public int calcPower { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        heartText = this.transform.Find("Heart Text").GetComponent<TMP_Text>();
        powerText = this.transform.Find("Power Text").GetComponent<TMP_Text>();
    }

    public void AssignCardRPC(Player newPlayer, Card newCard)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => AssignCardInfo
        (false, newPlayer.playerPosition, (myCard == null) ? -1 : myCard.pv.ViewID, newCard.pv.ViewID));
    }

    [PunRPC]
    void AssignCardInfo(bool undo, int playerPosition, int oldCard, int newCard)
    {
        this.player = Manager.inst.playersInOrder[playerPosition];
        this.image.transform.localScale = new(playerPosition == 0 ? 1 : -1, 1, 1);
        if (border != null)
            border.color = this.player.playerPosition == 0 ? Color.white : Color.black;

        if (undo && oldCard < 0) return;
        myCard = PhotonView.Find(undo ? oldCard : newCard).GetComponent<TroopCard>();

        this.name = myCard.name;
        this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");

        TroopCard intoTroop = (TroopCard)myCard;
        this.myHealth = intoTroop.health;
        this.myPower = intoTroop.power; 
    }

    #endregion

#region Gameplay

    public void ChangeStatsRPC(int power, int health, int logged, string source = "")
    {
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
        calcPower += troopPower;
        calcHealth += troopHealth;

        MovingAura enviro = Manager.inst.allRows[currentRow].auraHere;
        if (enviro != null)
        {
            (int enviroPower, int enviroHealth) = enviro.myCard.PassiveStats(this, enviro);
            calcPower += enviroPower;
            calcHealth += enviroHealth;
        }

        powerText.text = calcPower.ToString();
        heartText.text = calcHealth.ToString();
    }

    public int Attack(bool triggerAbilities, int logged)
    {
        RecalculateStats();
        Player opposingPlayer = Manager.inst.OpposingPlayer(this.player);
        MovingTroop opposingTroop = Manager.inst.FindOpposingTroop(this.player, this.currentRow);

        if (calcPower <= 0)
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} can't attack (it has {calcPower} Power).", logged);
            return 0;
        }
        else if (opposingTroop != null)
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} attacks {opposingPlayer.name}'s {opposingTroop.name}.", logged);
            if (triggerAbilities)
            {
                foreach ((Card card, Entity entity) in Manager.inst.GatherAbilities())
                    card.CardAttacked(entity, this, opposingTroop, logged + 1);
            }
            opposingTroop.ChangeStatsRPC(0, -this.calcPower, logged + 1, "");
            return this.calcPower;
        }
        else
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} attacks {opposingPlayer.name}.", logged);
            if (triggerAbilities)
            {
                foreach ((Card card, Entity entity) in Manager.inst.GatherAbilities())
                    card.CardAttacked(entity, this, opposingPlayer.myBase, logged + 1);
            }
            opposingPlayer.myBase.ChangeHealthRPC(-this.calcPower, logged + 1, "");
            return this.calcPower;
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
            if (newPosition > -1 && Manager.inst.allRows[newPosition].playerTroops[player.playerPosition] == this)
                Manager.inst.allRows[newPosition].playerTroops[player.playerPosition] = null;

            this.currentRow = oldPosition;
        }
        else
        {
            if (oldPosition > -1 && Manager.inst.allRows[oldPosition].playerTroops[player.playerPosition] == this)
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
            this.transform.localPosition = new((player.playerPosition) == 0 ? -400 : 400, 0);
            this.transform.localScale = Vector3.one;
            RecalculateStats();
        }
        else
        {
            player.availableTroops.Add(this);
            this.transform.SetParent(null);
        }
    }

    #endregion

}
