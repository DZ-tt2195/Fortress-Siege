using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Text.RegularExpressions;

public class MovingTroop : Entity
{

#region Setup

    TMP_Text powerText;
    TMP_Text heartText;

    int myHealth;
    int myPower;
    public int calcHealth { get; private set; }
    public int calcPower { get; private set; }

    public bool stunned { get; private set; }
    public bool shielded { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        heartText = this.transform.Find("Heart Text").GetComponent<TMP_Text>();
        powerText = this.transform.Find("Power Text").GetComponent<TMP_Text>();
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
                this.name = Regex.Replace(myCard.name, "(?<=[a-z])(?=[A-Z])", " ");
                this.image.sprite = Resources.Load<Sprite>($"Card Art/{this.name}");

                TroopCard intoTroop = (TroopCard)myCard;
                this.myHealth = intoTroop.health;
                this.myPower = intoTroop.power;
            }
        }
    }

    #endregion

#region Gameplay

    public void MoveTroopRPC(int newPosition, int logged)
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
            Log.inst.AddText($"{player.name} moves {this.name} to row {newPosition+1}.", logged);
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

    public void ChangeStatsRPC(int power, int health, int logged, string source)
    {
        string parathentical = source == "" ? "" : $" to ({source})";
        if (power < 0 || health < 0)
        {
            if (power < 0)
            {
                Log.inst.AddText($"{player.name}'s {this.name} would lose {Mathf.Abs(power)} Power{parathentical}.", logged);
                power = 0;
            }
            if (health < 0)
            {
                Log.inst.AddText($"{player.name}'s {this.name} would lose {Mathf.Abs(health)} Health{parathentical}.", logged);
                health = 0;
            }
            ShieldStatusRPC(false, logged, source);
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
        (int troopPower, int troopHealth) = myCard.PassiveStats(this);
        calcPower = myPower + troopPower;
        calcHealth = myHealth + troopHealth;

        Environment enviro = Manager.inst.allRows[currentRow].environment;
        if (enviro != null)
        {
            (int enviroPower, int enviroHealth) = enviro.myCard.PassiveStats(this, enviro);
            calcPower += enviroPower;
            calcHealth += enviroHealth;
        }

        powerText.text = calcPower.ToString();
        heartText.text = calcHealth.ToString();
    }

    public void Attack(int logged)
    {
        Player opposingPlayer = Manager.inst.OpposingPlayer(this.player);
        MovingTroop opposingTroop = Manager.inst.FindOpposingTroop(this.player, this.currentRow);

        if (stunned)
        {
            this.StunStatusRPC(false, logged, "");
        }
        else if (calcPower == 0)
        {
            Log.inst.PreserveTextRPC($"{this.player.name}'s {this.name} has 0 Power.", logged);
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

#region Statuses

    public void ShieldStatusRPC(bool shielded, int logged, string source)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => ShieldStatus(false, shielded, logged, source));
    }

    [PunRPC]
    void ShieldStatus(bool undo, bool newStatus, int logged, string source)
    {
        string parathentical = source == "" ? "" : $" ({source})";
        if (undo)
        {
            shielded = !newStatus;
        }
        else
        {
            shielded = newStatus;
            if (newStatus)
                Log.inst.AddText($"{player.name}'s {this.name} is Shielded{parathentical}.", logged);
            else
                Log.inst.AddText($"{player.name}'s {this.name} is no longer Shielded{parathentical}.", logged);
        }
    }

    public void StunStatusRPC(bool stunned, int logged, string source)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => StunStatus(false, stunned, logged, source));
    }

    [PunRPC]
    void StunStatus(bool undo, bool newStatus, int logged, string source)
    {
        string parathentical = source == "" ? "" : $" ({source})";
        if (undo)
        {
            stunned = !newStatus;
        }
        else
        {
            stunned = newStatus;
            if (newStatus)
                Log.inst.AddText($"{player.name}'s {this.name} is Stunned{parathentical}.", logged);
            else
                Log.inst.AddText($"{player.name}'s {this.name} is no longer Stunned{parathentical}.", logged);
        }
    }

    #endregion

}
