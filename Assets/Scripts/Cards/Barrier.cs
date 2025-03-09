using UnityEngine;

public class Barrier : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.extraText = "Your Troops here have +3 Health.";
        this.artistText = "Joshua Stewart\nDominion: Empires\n(Wall)";
    }

    public override (int, int) PassiveStats(MovingTroop troop, MovingAura enviro = null)
    {
        if (enviro.currentRow == troop.currentRow && enviro.player == troop.player)
            return (0, 3);
        else
            return (0, 0);
    }
}
