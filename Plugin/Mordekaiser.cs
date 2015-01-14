#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.Drawing;
using MAC.Model;

#endregion

namespace MAC.Plugin
{
    internal class Mordekaiser : PluginModel
    {

        public Items.Item Dfg;
        public Spell E;
        public Spell Q;
        public Spell W;
        public Spell R;

        private bool ultado = false;

        public Mordekaiser()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 750);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 850);

            E.SetSkillshot(0.25f, 90, 2000, false, SkillshotType.SkillshotCone);

            Dfg = new Items.Item(3128, 750);

            Game.OnGameUpdate += GameOnOnGameUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;
            Orbwalking.AfterAttack += AfterAttack;

            //PrintChat("Mordekaiser Carregado.");

        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            var drawW = GetBool("drawW");
            var drawE = GetBool("drawE");
            var drawR = GetBool("drawR");
            var p = Player.Position;

            if (drawW)
                Utility.DrawCircle(p, W.Range, W.IsReady() ? Color.Aqua : Color.Red);

            if (drawE)
                Utility.DrawCircle(p, E.Range, E.IsReady() ? Color.Aqua : Color.Red);

            if (drawR)
                Utility.DrawCircle(p, R.Range, R.IsReady() ? Color.Aqua : Color.Red);

        }

        private void GameOnOnGameUpdate(EventArgs args)
        {
            autoUt();
            switch (OrbwalkerMode)
            {
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;

                case Orbwalking.OrbwalkingMode.Combo:
                    Combar();
                    break;
            }
        }

        private void Combar()
        {
            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);

            var useDfg = GetBool("useDFG");

            if (useDfg && Dfg.IsReady())
                Dfg.Cast(target);

            if (GetBool("comboW") && W.IsReady())
            {
                if (target.Distance(ObjectManager.Player.Position) <= 250)
                {
                    W.Cast(ObjectManager.Player);
                }
                else
                {
                    var nearTarget = inimigoProximo(target);

                    W.Cast(nearTarget);
                }
            }

            if (GetBool("comboE") && E.IsReady() && E.InRange(target))
            {
                E.CastIfHitchanceEquals(target, HitChance.High, Packets);
            }

        }

        private void Harass()
        {
            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);
            if (target == null)
                return;

            if (GetBool("harassW") && W.IsReady())
            {
                if (target.Distance(ObjectManager.Player.Position) <= 250)
                {
                    W.Cast(ObjectManager.Player);
                }
                else
                {
                    var nearTarget = inimigoProximo(target);

                    W.Cast(nearTarget);
                }
            }

            if (GetBool("harassE") && E.IsReady())
            {
                E.CastIfHitchanceEquals(target, HitChance.High, Packets);
            }

        }

        private Obj_AI_Base inimigoProximo(Obj_AI_Hero target)
        {
            var nearEnemy =
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(x => !x.IsEnemy)
                        .Where(x => !x.IsDead)
                        .Where(x => x.Distance(ObjectManager.Player.Position) <= W.Range)
                        .FirstOrDefault(
                            x => x.Distance(target.Position) <= 250);
            return nearEnemy;
        }

        public override float GetComboDamage(Obj_AI_Hero target)
        {
            double dmg = 0;

            if (Q.IsReady())
                dmg += Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.Q, 1);

            if (W.IsReady())
                dmg += Player.GetSpellDamage(target, SpellSlot.W);

            if (E.IsReady())
            {
                dmg += Player.GetSpellDamage(target, SpellSlot.E);
                dmg += dmg * 0.2;
            }

            if (R.IsReady())
            {
                dmg += Player.GetSpellDamage(target, SpellSlot.R);
                dmg += dmg * 0.2;
            }

            if (!Dfg.IsReady()) return (float)dmg;
            dmg += Player.GetItemDamage(target, Damage.DamageItems.Dfg);
            dmg += dmg * 0.2;

            return (float)dmg;
        }

        private void autoUt()
        {
            if (!R.IsReady() || GetBool("autoR") || ultado)
                return;

            var target =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(x => x.IsEnemy)
                        .Where(x => !x.IsMinion)
                        .Where(x => !x.IsDead)
                        .Where(x => x.Distance(ObjectManager.Player.Position) <= R.Range)
                        .FirstOrDefault(
                            x => x.Health + 30 < (Player.GetSpellDamage(x, SpellSlot.R) * 0.2));

            if (target != null)
            {
                if (target.Health + 30 < (Player.GetSpellDamage(target, SpellSlot.R) * 0.2))
                {
                    R.Cast(target);
                    ultado = true;

                    Utility.DelayAction.Add(40000, () => ultado = false);
                }
            }
        }

        public void AfterAttack(AttackableUnit unit, AttackableUnit mytarget)
        {
            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);

            if (OrbwalkerMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (GetBool("harassQ") && Q.IsReady())
                {
                    Q.Cast(Packets);
                }
            }
            if (OrbwalkerMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (GetBool("comboQ") && Q.IsReady())
                {
                    Q.Cast(Packets);
                }
            }
        }

        public override void Combo(Menu config)
        {
            config.AddItem(new MenuItem("comboQ", "Use Q").SetValue(true));
            config.AddItem(new MenuItem("comboW", "Use W").SetValue(true));
            config.AddItem(new MenuItem("comboE", "Use E").SetValue(true));
        }

        public override void Harass(Menu config)
        {
            config.AddItem(new MenuItem("harassQ", "Use Q").SetValue(true));
            config.AddItem(new MenuItem("harassW", "Use W").SetValue(false));
            config.AddItem(new MenuItem("harassE", "Use E").SetValue(false));
        }

        public override void ItemMenu(Menu config)
        {
            config.AddItem(new MenuItem("useDFG", "Use DFG").SetValue(true));
        }

        public override void Misc(Menu config)
        {
            config.AddItem(new MenuItem("autoR", "Use R if killable").SetValue(true));
        }

        public override void Drawings(Menu config)
        {
            config.AddItem(new MenuItem("drawW", "Draw W").SetValue(true));
            config.AddItem(new MenuItem("drawE", "Draw E").SetValue(true));
            config.AddItem(new MenuItem("drawR", "Draw R").SetValue(true));
        }
    }
}