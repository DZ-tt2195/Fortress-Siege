using UnityEngine;

public class Fog : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.extraText = "Opposing Troops here get -1 Power -1 Health.";
        this.artistText = "Jaime Jones\nMTG: Magic 2012\n(Fog)";
    }

    public override (int, int) PassiveStats(MovingTroop troop, MovingAura enviro = null)
    {
        if (enviro.currentRow == troop.currentRow && troop.player != enviro.player)
            return (-1, -1);
        else
            return (0, 0);
    }
}
