using System.Collections.Generic;
using UnityEngine;

public class Kitty : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 1;
        this.health = 4;
        this.extraText = "When you play this: An opposing Troop loses 2 Power.";
        this.artistText = "Howard Lyon\nMTG: Eldritch Moon\n(Harmless Offering)";
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
                player.inReaction.Add(LosePower);
                player.DecisionMade(next);
            }
            else
            {
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
                player.ChooseRow(withTroops, "Choose an opposing troop to lose 2 Power.", LosePower);
            }
        }

        void LosePower()
        {
            if (player.choice >= 0)
            {
                Row targetRow = Manager.inst.allRows[player.choice];
                MovingTroop targetTroop = targetRow.playerTroops[otherPlayer.playerPosition];
                targetTroop.ChangeStatsRPC(-2, 0, logged);
            }
            else
            {
                Log.inst.AddTextRPC($"{this.name} has no troops to target.", LogAdd.Remember, logged);
            }
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
