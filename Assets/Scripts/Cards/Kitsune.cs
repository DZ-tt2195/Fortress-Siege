using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Kitsune : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.power = 6;
        this.health = 3;
        this.extraText = "When you play this: You may have this get -3 Power +3 Health.";
        this.artistText = "Claus Stephan\nDominion: Rising Sun\n(Kitsune)";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => CardDecision(player, createdEntity, logged));
    }

    void CardDecision(Player player, Entity createdEntity, int logged)
    {
        MovingTroop troop = (MovingTroop)createdEntity;

        if (player.myType == PlayerType.Bot)
        {
            if (player.chainTracker < player.currentChain.decisions.Count)
            {
                int next = player.currentChain.decisions[player.chainTracker];
                //Debug.Log($"resolved continue turn with choice {next}");
                player.inReaction.Add(Decision);
                player.DecisionMade(next);
            }
            else
            {
                player.NewChains(new List<int> { 0, 1});
            }
        }
        else
        {
            player.ChooseButton(new List<string> { "6 Power / 3 Health", "3 Power / 6 Health" }, Vector3.zero, $"Choose {this.name}'s stats.", Decision);
        }

        void Decision()
        {
            if (player.choice == 0)
                Log.inst.AddTextRPC($"{this.name} stays at 3 Power / 6 Health.", LogAdd.Remember, logged);
            else
                troop.ChangeStatsRPC(-3, 3, logged);
            base.DonePlaying(player, createdEntity, logged);
        }
    }
}
