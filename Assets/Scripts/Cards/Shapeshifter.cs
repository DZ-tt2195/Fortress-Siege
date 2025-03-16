using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Shapeshifter : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 6;
        this.power = 5;
        this.health = 5;
        this.extraText = "When you play this: Choose one: This gets +2 Power -2 Health; or -2 Power +2 Health.";
        this.artistText = "";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    {
        List<string> actions = new() { $"+2 Power -2 Health", "-2 Power +2 Health" };

        if (player.myType == PlayerType.Bot)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                //Debug.Log($"resolved continue turn with choice {next}");
                player.inReaction.Add(Choice);
                player.DecisionMade(next);
            }
            else
            {
                //Debug.Log($"{chainTracker}, {currentChain.decisions.Count}");
                player.NewChains(0, 2, 1);
            }
        }
        else
        {
            player.ChooseButton(actions, Vector3.zero, "Destroy an Aura?", Choice);
        }

        void Choice()
        {
            MovingTroop troop = (MovingTroop)createdEntity;
            if (player.choice == 0)
                troop.ChangeStatsRPC(2, -2, logged, this.name);
            else
                troop.ChangeStatsRPC(-2, 2, logged, this.name);
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
