using System.Collections.Generic;
using UnityEngine;

public class Electrician : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 2;
        this.health = 3;
        this.extraText = "When you play this: One of your Troops does an attack.";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    {
        List<Row> withTroops = player.FilterRows(true);

        if (player.myType == PlayerType.Computer)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                player.inReaction.Add(DoAttack);
                player.DecisionMade(next);
            }
            else
            {
                player.NewChains(0, withTroops.Count, 1);
            }
        }
        else
        {
            player.ChooseRow(withTroops, "Choose one of your troops to do an attack.", DoAttack);
        }

        void DoAttack()
        {
            Row targetRow = withTroops[player.choice];
            MovingTroop targetTroop = targetRow.playerTroops[player.playerPosition];
            targetTroop.Attack(logged);
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
