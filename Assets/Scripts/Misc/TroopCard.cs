using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class TroopCard : Card
{

#region Setup

    public int power { get; protected set; }
    public int health { get; protected set; }
    protected int abilityValue;
    List<Row> canPlayInColumn = new();

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    protected float Math()
    {
        float math = (-2 - this.coinCost*2) + (health + power + abilityValue);
        if (Mathf.Abs(math) >= 1f)
            Debug.Log($"{this.name}'s math: {math}");
        return math;
    }

    public override Color MyColor()
    {
        return Color.red;
    }

    #endregion

#region Gameplay

    public override bool CanPlayMe(Player player, bool pay)
    {
        canPlayInColumn = player.FilterRows(false);
        if (canPlayInColumn.Count >= 1)
            return base.CanPlayMe(player, pay);
        else
            return false;
    }

    public override void OnPlayEffect(Player player, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRow(player, logged));
    }

    void ChooseRow(Player player, int logged)
    {
        if (player.myType == PlayerType.Computer)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                //Debug.Log($"resolved choose row with choice {next}");
                player.inReaction.Add(PlayTroop);
                player.DecisionMade(next);
            }
            else
            {
                //Debug.Log($"add rows: {player.chainTracker}, {player.currentChain.decisions.Count}");
                player.NewChains(0, canPlayInColumn.Count, 0);
            }
        }
        else if (player.myType == PlayerType.Human)
        {
            player.ChooseRow(canPlayInColumn, $"Where to play {this.name}?", PlayTroop);
        }

        void PlayTroop()
        {
            MovingTroop troop = player.availableTroops[0];
            Log.inst.RememberStep(troop, StepType.Revert, () => troop.AssignCardInfo(false, player.playerPosition, this.pv.ViewID));

            int rememberChoice = player.choice;
            Log.inst.RememberStep(troop, StepType.Revert, () => troop.MoveTroop(false, -1, rememberChoice, logged+1));
            Log.inst.RememberStep(player, StepType.UndoPoint, () => player.MayPlayCard());
        }
    }

    #endregion

}
