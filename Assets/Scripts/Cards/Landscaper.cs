using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Landscaper : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 2;
        this.health = 3;
        this.abilityValue = 2;
        this.extraText = "When you play this: An opposing Troop loses 2 Power.";
        Math();
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    {
        Player otherPlayer = Manager.inst.OpposingPlayer(player);
        List<Row> withTroops = otherPlayer.FilterRows(true);

        if (player.myType == PlayerType.Computer)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                player.inReaction.Add(LosePower);
                player.DecisionMade(next);
            }
            else
            {
                if (withTroops.Count == 0)
                    player.NewChains(-1, 0, 1);
                else
                    player.NewChains(0, withTroops.Count, 1);
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
            if (player.choice < withTroops.Count)
            {
                Row targetRow = withTroops[player.choice];
                MovingTroop targetTroop = targetRow.playerTroops[otherPlayer.playerPosition];
                targetTroop.ChangeStatsRPC(-2, 0, logged);
            }
            else
            {
                Log.inst.PreserveTextRPC($"{this.name} has no troops to target.", logged);
            }
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}