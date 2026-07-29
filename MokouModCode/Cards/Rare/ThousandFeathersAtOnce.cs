using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MokouMod.MokouModCode.Extensions;
using MokouMod.MokouModCode.Powers;

namespace MokouMod.MokouModCode.Cards.Rare;

public class ThousandFeathersAtOnce : MokouModCard
{
    private bool HasHeatwave => IsMutable && Owner != null && Owner.Creature.HasPower<HeatwavePower>();
    private int _repeatIncrement = 0;

    public ThousandFeathersAtOnce() : base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithPower<BurnPower>(2);
        WithCalculatedVar("Repeat", 1, (card, _) => card is ThousandFeathersAtOnce thousand ? thousand._repeatIncrement : 0);
        WithKeywords(CardKeyword.Exhaust);
        WithTip(new TooltipSource(card => new HoverTip(new LocString("cards", Id.Entry + ".extraTipTitle"), new LocString("cards", Id.Entry + ".extraTipDescription"))));
        WithCostUpgradeBy(-1);
    }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override TargetType TargetType => !HasHeatwave ? TargetType.AnyEnemy : TargetType.AllEnemies;

    protected override async Task OnPlayMokou(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (var i = 0; i < (int)((CalculatedVar)DynamicVars["Repeat"]).Calculate(cardPlay.Target); ++i)
        {
            SfxCmd.Play("feather.wav".SoundEffectPath());
            if (HasHeatwave)
            {
                var lastEnemy = CombatState.HittableEnemies.LastOrDefault();
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NShivThrowVfx.Create(Owner.Creature, lastEnemy, Colors.Red));
                foreach (var enemy in CombatState.HittableEnemies)
                    NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireBurstVfx.Create(enemy, 0.5f));
                await PowerCmd.Apply<BurnPower>(choiceContext, CombatState.HittableEnemies, DynamicVars["BurnPower"].BaseValue, Owner.Creature, this);
            }
            else
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NShivThrowVfx.Create(Owner.Creature, cardPlay.Target, Colors.Red));
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireBurstVfx.Create(cardPlay.Target, 0.5f));
                await PowerCmd.Apply<BurnPower>(choiceContext, cardPlay.Target, DynamicVars["BurnPower"].BaseValue, Owner.Creature, this);
            }
        }
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Character.Id.ToString() == "CHARACTER.KEINEMOD-KEINE_MOD" && cardPlay.Card.Owner != Owner)
            _repeatIncrement += 2;
        else
            _repeatIncrement++;
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner.Creature))
            _repeatIncrement = 0;
        return Task.CompletedTask;
    }
}