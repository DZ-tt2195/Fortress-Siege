using UnityEngine;

public class Charm : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.extraText = "Opposing Troops here with 4 Power or more get -4 Power.";
        this.artistText = "Marcel-André Casasola Merkle\nDominion: Plunder\n(Siren)";
    }

    public override (int, int) PassiveStats(MovingTroop troop, MovingAura enviro = null)
    {
        if (enviro.currentRow == troop.currentRow && troop.player != enviro.player && troop.calcPower >= 4)
            return (-4, 0);
        else
            return (0, 0);
    }
}
