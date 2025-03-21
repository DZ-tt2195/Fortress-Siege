using System.Collections.Generic;
using UnityEngine;

public class Ghost : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 2;
        this.health = 1;
        this.extraText = "When you play this: An opposing troop loses 1 Power and 1 Health.";
        this.artistText = "Eric J. Carter\nDominion: Nocturne\n(Ghost)";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    {
        Player otherPlayer = Manager.inst.OpposingPlayer(player);
        List<Row> withTroops = otherPlayer.FilterRows(true);

        if (player.myType == PlayerType.Bot)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                player.inReaction.Add(ChosenTroop);
                player.DecisionMade(next);
            }
            else
            {

                if (withTroops.Count == 0)
                    player.NewChains(new List<int>() { -1 });
                else
                    player.NewChains(player.RowsToInts(withTroops));
            }
        }
        else
        {
            if (withTroops.Count == 0)
            {
                Log.inst.undoToThis = null;
                base.DonePlaying(player, createdEntity, logged);
                player.DecisionMade(-1);
            }
            else
            {
                player.ChooseRow(withTroops, "Move an opposing troop.", ChosenTroop);
            }
        }

        void ChosenTroop()
        {
            if (player.choice >= 0)
            {
                Row targetRow = Manager.inst.allRows[player.choice];
                MovingTroop targetTroop = targetRow.playerTroops[otherPlayer.playerPosition];
                targetTroop.ChangeStatsRPC(-1, -1, logged);
            }
            else
            {
                Log.inst.PreserveTextRPC($"{this.name} has no Troops to move.", logged);
            }
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
