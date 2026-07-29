using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using MokouMod.MokouModCode.Cards.Special;
using MokouMod.MokouModCode.Powers;

namespace MokouMod.MokouModCode.Cards.Rare;

public class FengWingAscension : MokouModCard
{
    public FengWingAscension() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(12, 3, (card, _) => (int)Math.Floor(PileType.Exhaust.GetPile(card.Owner).Cards.Count(c => c is Feather or ThousandFeathersAtOnce) * 0.5), ValueProp.Move, 4, 1);
        WithPower<BurnPower>(12, 4);
    }

    public override Character.MokouMod.Animation Anim => Character.MokouMod.Animation.AttackAirKick;

    protected override async Task OnPlayMokou(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this, cardPlay).Targeting(cardPlay.Target).BeforeDamage(async () =>
        {
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(cardPlay.Target);
            if (creatureNode != null)
            {
                var child = NLargeMagicMissileVfx.Create(creatureNode.GetBottomOfHitbox(), new Color("#c81e1e"));
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(child);
            }

            await Cmd.CustomScaledWait(0.3f, 0.4f);
        }).Execute(choiceContext);

        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely((Node)NGroundFireVfx.Create(cardPlay.Target));
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely((Node)NFireBurstVfx.Create(cardPlay.Target, 0.75f));
        await PowerCmd.Apply<BurnPower>(choiceContext, cardPlay.Target, DynamicVars["BurnPower"].BaseValue, Owner.Creature, this);
    }

    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (cardSource == this && power is BurnPower)
            return (int)Math.Floor(PileType.Exhaust.GetPile(Owner).Cards.Count(c => c is Feather or ThousandFeathersAtOnce) * 0.5) * DynamicVars["ExtraDamage"].BaseValue;

        return 0M;
    }
}