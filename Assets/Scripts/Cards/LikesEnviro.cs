using UnityEngine;

public class LikesEnviro : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 2;
        this.health = 2;
        this.abilityValue = 2;
        this.extraText = "If there's an Environment here, this gets +2 Health.";
        Math();
    }

    public override (int, int) PassiveStats(MovingTroop troop, Environment enviro = null)
    {
        if (Manager.inst.allRows[troop.currentRow].environment == null)
            return (0, 0);
        else
            return (0, 2);
    }
}
