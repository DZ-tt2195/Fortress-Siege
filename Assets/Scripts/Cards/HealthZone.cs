using UnityEngine;

public class HealthZone : EnviroCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.abilityValue = 3;
        this.extraText = "Your troops here have +3 Health.";
        Math();
    }

    public override (int, int) EnviroStats(Environment enviro, MovingTroop troop)
    {
        if (enviro.currentRow == troop.currentRow && enviro.player == troop.player)
            return (0, 3);
        else
            return (0, 0);
    }
}
