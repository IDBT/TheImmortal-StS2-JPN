using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using MokouMod.MokouModCode.Scripts;

namespace MokouMod.MokouModCode.Cards.Rare;

public class BlazingBambooTube : MokouModCard
{
    public BlazingBambooTube() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithVars(
            new CalculationBaseVar(10M),
            new ExtraDamageVar(5M),
            new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((Func<CardModel, Creature?, decimal>)((card, _) =>
                {
                    var exhaustPile = PileType.Exhaust.GetPile(card.Owner);
                    return exhaustPile.Cards.Count(c => c.Keywords.Contains(MokouModKeywords.Fuel));
                }))
        );
        WithTip(MokouModKeywords.Fuel);
    }

    public override Character.MokouMod.Animation Anim => Character.MokouMod.Animation.SpellBackflip;

    protected override async Task OnPlayMokou(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(cardPlay.Target);
        if (GodotObject.IsInstanceValid(creatureNode))
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireSmokePuffVfx.Create(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(2M);
    }
}