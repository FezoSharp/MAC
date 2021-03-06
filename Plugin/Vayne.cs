﻿using LeagueSharp;
using LeagueSharp.Common;
using MAC.Model;
using MAC.Util;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAC.Plugin
{
    internal class Vayne : PluginModel
    {

        public Spell Q;
        public Spell W;
        public Spell E;
        public Spell R;

        private AttackableUnit lastObjectAttacked = null;
        private int stackCount = 0;

        //Bonus de dano no 3 ataque
        public int[] danoVerdadeiro = { 20, 30, 40, 50, 60 };
        public int[] bonusPorcentagemHP = { 4, 5, 6, 7, 8 };

        public Vayne()
        {
            Q = new Spell(SpellSlot.Q, 300f);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 550f);
            E.SetTargetted(0.25f, 1600f);

            R = new Spell(SpellSlot.R);

            Game.OnGameUpdate += GameOnOnGameUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;

            MiscControl.PrintChat(MiscControl.stringColor("Vayne Loaded", MiscControl.TableColor.Red));
        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            var drawQ = GetBool("drawQ");
            var drawE = GetBool("drawE");
            var comboTypeIndex = Menu.Item("comboType").GetValue<StringList>().SelectedIndex;

            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);

            var wts = Drawing.WorldToScreen(Player.Position);

            var p = Player.Position;

            if (GetBool("disableAll"))
                return;

            if (drawQ)
                Utility.DrawCircle(p, Q.Range, Q.IsReady() ? System.Drawing.Color.Aqua : System.Drawing.Color.Red);

            if (drawE)
                Utility.DrawCircle(p, E.Range, E.IsReady() ? System.Drawing.Color.Aqua : System.Drawing.Color.Red);

            if (GetBool("drawComboType"))
                switch (comboTypeIndex)
                {
                    case 0:
                        Drawing.DrawText(wts[0] - 35, wts[1] + 10, System.Drawing.Color.White, "Manual Combo Selected");
                        break;
                    case 1:
                        Drawing.DrawText(wts[0] - 35, wts[1] + 10, System.Drawing.Color.Red, "Advanced Combo Selected");
                        break;
                    case 2:
                        Drawing.DrawText(wts[0] - 35, wts[1] + 10, System.Drawing.Color.Gold, "OMG GOSU o.O");
                        break;
                }

            if (GetBool("drawCondemnPosition"))
            {
                var position = GetBestCondemnPosition(target);
                Utility.DrawCircle(position, 80, System.Drawing.Color.White);
            }

        }

        private void GameOnOnGameUpdate(EventArgs args)
        {
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
            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);

            var comboTypeIndex = Menu.Item("comboType").GetValue<StringList>().SelectedIndex;
            
            /*
                Gosu mode ignore all your combo configs 
             */

            if (comboTypeIndex == 2)
            {
                if (R.IsReady() && Player.Distance(target.Position) < Player.AttackRange)
                {
                    R.Cast();
                }

                if (CanUseItem(3142) && Player.Distance(target.Position) < Player.AttackRange)
                    UseItem(3142);

                if ((Player.Health / Player.MaxHealth) * 100 < (target.Health / target.MaxHealth) * 100 && (CanUseItem(3153) || CanUseItem(3144)))
                {
                    UseItem(3144, target);

                    UseItem(3153, target);
                }

                if (stackCount == 2 && !lastObjectAttacked.IsDead && lastObjectAttacked.Type == GameObjectType.obj_AI_Hero)
                {
                    if (E.IsReady() && calcularDanoAtaque(target, false) > target.Health && E.InRange(target.Position))
                    {
                        E.Cast(target);
                    }
                    else if (E.IsReady() && Q.IsReady() && calcularDanoAtaque(target, false) > target.Health && Player.Distance(target.Position) < E.Range + Q.Range)
                    {
                        Q.Cast(target.Position);
                        E.Cast(target);
                    }
                }

                if (Q.IsReady() && 
                    stackCount == 2 && 
                    !lastObjectAttacked.IsDead &&
                    lastObjectAttacked.Type == GameObjectType.obj_AI_Hero && 
                    target == lastObjectAttacked && 
                    Q.InRange(target.Position))
                {
                    Q.Cast(target);
                }

                if (E.IsReady())
                {
                    if (CondemnCheck(Player.Position, out target))
                    {
                        E.Cast(target);
                    }
                    else
                    {
                        var postition = GetBestCondemnPosition(target);
                        if (postition == null)
                            return;

                        if (Player.Distance(postition) <= Q.Range && Q.IsReady())
                        {
                            Q.Cast(postition);
                        }
                    }
                }
            }

            /* End of Gosu Mode */

            /**
             * Advanced Combo Mode 
             */

            if (comboTypeIndex == 1)
            {

                if (R.IsReady() && GetBool("comboR") && Player.Distance(target.Position) < Player.AttackRange && GetValue<Slider>("minEnemiesInRangeR").Value >= enemiesInRange(Player, Player.AttackRange))
                {
                    R.Cast();
                }

                if (CanUseItem(3142) && Player.Distance(target.Position) < Player.AttackRange && GetBool("BotrkC"))
                    UseItem(3142);

                if ((Player.Health / Player.MaxHealth) * 100 < (target.Health / target.MaxHealth) * 100 && (CanUseItem(3153) || CanUseItem(3144)) && GetBool("BotrkC"))
                {
                    UseItem(3144, target);

                    UseItem(3153, target);
                }

                if (stackCount == 2 && !lastObjectAttacked.IsDead && lastObjectAttacked.Type == GameObjectType.obj_AI_Hero)
                {
                    if (E.IsReady() && calcularDanoAtaque(target, false) > target.Health && E.InRange(target.Position) && GetBool("comboE"))
                    {
                        E.Cast(target);
                    }
                    else if (E.IsReady() && Q.IsReady() && calcularDanoAtaque(target, false) > target.Health && Player.Distance(target.Position) < E.Range + Q.Range && GetBool("comboE") && GetBool("comboQ"))
                    {
                        Q.Cast(target.Position);
                        E.Cast(target);
                    }
                }

                if (Q.IsReady() &&
                    stackCount == 2 &&
                    !lastObjectAttacked.IsDead &&
                    lastObjectAttacked.Type == GameObjectType.obj_AI_Hero &&
                    target == lastObjectAttacked &&
                    Q.InRange(target.Position) && GetBool("comboQ"))
                {
                    Q.Cast(target);
                }

                if (E.IsReady() && GetBool("comboE"))
                {
                    if (CondemnCheck(Player.Position, out target))
                    {
                        E.Cast(target);
                    }
                    else
                    {
                        var postition = GetBestCondemnPosition(target);
                        if (postition == null)
                            return;

                        if (Player.Distance(postition) <= Q.Range && Q.IsReady() && GetBool("comboQ"))
                        {
                            Q.Cast(postition);
                        }
                    }
                }

            }

            /* End of Advanced Combo Mode */

            /**
             * Manual Combo Mode 
             */

            if (comboTypeIndex == 0)
            {

                if (E.IsReady() && GetBool("comboE"))
                {
                    if (CondemnCheck(Player.Position, out target))
                    {
                        E.Cast(target);
                    }
                }

            }

            /* End of Manual Combo Mode */

        }

        private void Harass()
        {
            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);

            if (E.IsReady() && GetBool("harassE") && CondemnCheck(Player.Position, out target))
            {
                E.Cast(target);
            }

        }

        private void LaneClear()
        {

            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);

            if (E.IsReady() && GetBool("harassE") && CondemnCheck(Player.Position, out target))
            {
                E.Cast(target);
            }

        }

        public override void OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
            {
                if (!target.IsDead)
                {
                    if (lastObjectAttacked == target)
                    {
                        stackCount++;
                        if (stackCount > 2)
                        {
                            stackCount = 0;
                        }
                    }
                    else
                    {
                        stackCount = 0;
                    }
                    lastObjectAttacked = target;
                }
            }

            var comboTypeIndex = Menu.Item("comboType").GetValue<StringList>().SelectedIndex;

            if (OrbwalkerMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (comboTypeIndex == 2 && Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }else if (comboTypeIndex < 2 && GetBool("comboQ") && Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }
            }
            else if (OrbwalkerMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (GetBool("harassQ") && Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }
            }
            else if (OrbwalkerMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                if (GetBool("laneClearQ") && Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }
            }
        }

        public int enemiesInRange(Obj_AI_Hero obj, float range)
        {
            var nearEnemies =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(x => x.IsEnemy)
                        .Where(x => !x.IsDead)
                        .Where(x => x.Distance(obj.Position) <= range);
            return nearEnemies.Count();
        }

        public float calcularDanoAtaque(Obj_AI_Base target, bool autoAttack)
        {
            double dmg = 0;

            if (autoAttack && stackCount == 2 && target == lastObjectAttacked)
            {
                dmg = Player.GetAutoAttackDamage(target, false) + danoDardoPrata(target);
            }
            else if (!autoAttack && stackCount == 2 && target == lastObjectAttacked)
            {
                dmg = danoDardoPrata(target);
            }

            return (float)dmg;
        }

        public float danoDardoPrata(Obj_AI_Base target)
        {
            return (float)danoVerdadeiro[Player.Spellbook.GetSpell(SpellSlot.W).Level] + target.MaxHealth * (bonusPorcentagemHP[Player.Spellbook.GetSpell(SpellSlot.W).Level] / 100);
        }

        /*
         * Ayy this pasterino lmao
         * AsunaChan (segura o tchan, amarra o tchan)
         */

        bool CondemnCheck(Vector3 Position, out Obj_AI_Hero target)
        {
            if (isUnderEnemyTurret(Player.Position) && GetBool("NoEEnT"))
            {
                target = null;
                return false;
            }
            foreach (var En in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.IsValidTarget() && !GetBool("nC" + hero.ChampionName) && hero.Distance(Player.Position) <= E.Range))
            {
                var EPred = E.GetPrediction(En);
                int pushDist = Menu.Item("PushDistance").GetValue<Slider>().Value;
                var FinalPosition = EPred.UnitPosition.To2D().Extend(Position.To2D(), -pushDist).To3D();
                for (int i = 1; i < pushDist; i += (int)En.BoundingRadius)
                {
                    Vector3 loc3 = EPred.UnitPosition.To2D().Extend(Position.To2D(), -i).To3D();
                    var OrTurret = GetBool("CondemnTurret") && isUnderTurret(FinalPosition);
                    var OrFountain = GetBool("CondemnTurret") && isFountain(FinalPosition);
                    if (isWall(loc3) || OrTurret || OrFountain)
                    {
                        target = En;
                        return true;
                    }
                }
            }
            target = null;
            return false;
        }

        /*
         Hellsing and Fluxy kappa
         */
        static Vector3 GetBestCondemnPosition(Obj_AI_Hero target)
        {

            // 12 angle checks for casting, prefer to player direction
            var direction = (Player.ServerPosition - target.Position).Normalized();
            var maxAngle = 180f;
            var step = maxAngle / 6f;
            var currentAngle = 0f;
            var currentStep = 0f;

            var toPosition =  Vector3.Zero;
            var fromPosition = Player.Position;

            while (true)
            {
                // Validate the counter, break if no valid spot was found in previous loops
                if (currentStep > maxAngle && currentAngle < 0)
                    break;

                // Check next angle
                if ((currentAngle == 0 || currentAngle < 0) && currentStep != 0)
                {
                    currentAngle = (currentStep) * (float)Math.PI / 180;
                    currentStep += step;
                }
                else if (currentAngle > 0)
                    currentAngle = -currentAngle;

                Vector3 checkPoint;

                // One time only check for direct line of sight without rotating
                if (currentStep == 0)
                {
                    currentStep = step;
                    checkPoint = target.Position + 500 * direction;
                }
                // Rotated check
                else
                    checkPoint = target.Position + 500 * direction.Rotated(currentAngle);

                if (target.Position.GetFirstWallPoint(checkPoint).HasValue)
                {
                    toPosition = Player.Position + 500 * (checkPoint - target.Position).Normalized();
                }
            }

            //var insecLinePos = Drawing.WorldToScreen(toPosition);

            return V2E(fromPosition, toPosition, target.Distance(fromPosition) + 300).To3D();
        }

        static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }

        bool isWall(Vector3 Pos)
        {
            CollisionFlags cFlags = NavMesh.GetCollisionFlags(Pos);
            return (cFlags == CollisionFlags.Wall);
        }

        bool isUnderTurret(Vector3 Position)
        {
            foreach (var tur in ObjectManager.Get<Obj_AI_Turret>().Where(turr => turr.IsAlly && (turr.Health != 0)))
            {
                if (tur.Distance(Position) <= 975f) return true;
            }
            return false;
        }

        bool isUnderEnemyTurret(Vector3 Position)
        {
            foreach (var tur in ObjectManager.Get<Obj_AI_Turret>().Where(turr => turr.IsEnemy && (turr.Health != 0)))
            {
                if (tur.Distance(Position) <= 975f) return true;
            }
            return false;
        }

        bool isFountain(Vector3 Position)
        {
            float fountainRange = 750;
            var map = Utility.Map.GetMap();
            if (map != null && map._MapType == Utility.Map.MapType.SummonersRift)
            {
                fountainRange = 1050;
            }
            return
                ObjectManager.Get<GameObject>()
                    .Where(spawnPoint => spawnPoint is Obj_SpawnPoint && spawnPoint.IsAlly)
                    .Any(
                        spawnPoint =>
                            Vector2.Distance(Position.To2D(), spawnPoint.Position.To2D()) <
                            fountainRange);
        }

        public override void Combo(Menu config)
        {
            config.AddItem(new MenuItem("comboQ", "Use Q").SetValue(true));
            config.AddItem(new MenuItem("comboE", "Use E").SetValue(true));
            config.AddItem(new MenuItem("comboR", "Use R").SetValue(true));
        }

        public override void Harass(Menu config)
        {
            config.AddItem(new MenuItem("harassQ", "Use Q").SetValue(true));
            config.AddItem(new MenuItem("harassE", "Use E").SetValue(false));
        }

        public override void Laneclear(Menu config)
        {
            config.AddItem(new MenuItem("laneClearQ", "Use Q").SetValue(true));
        }

        public override void Misc(Menu config)
        {
            config.AddItem(new MenuItem("comboType", "Combo Type").SetValue(new StringList(new[] { "Normal", "Advanced", "Gosu" }, 2)));
            config.AddItem(new MenuItem("condemnNextAuto", "Condemn on next Auto Attack").SetValue(false));
            config.AddItem(new MenuItem("minEnemiesInRangeR", "Min. enemies in range to cast Ultimate").SetValue(new Slider(2, 1, 5)));
        }

        public override void Extra(Menu config)
        {
            var MiscMSubMenu = new Menu("Misc - Mana Manager", "MiscM");
            {
                MiscMSubMenu.AddItem(new MenuItem("saveMana", "% safe for Combo").SetValue(new Slider(50, 0, 100)));
            }

            config.AddSubMenu(MiscMSubMenu);

            var MiscCSubMenu = new Menu("Misc - Condemn", "MiscC");
            {
                MiscCSubMenu.AddItem(new MenuItem("PushDistance", "E Push Dist").SetValue(new Slider(425, 400, 500)));
                MiscCSubMenu.AddItem(new MenuItem("CondemnTurret", "Try to Condemn to turret").SetValue(false));
                MiscCSubMenu.AddItem(new MenuItem("NoEEnT", "No E Under enemy turret").SetValue(true));
            }

            config.AddSubMenu(MiscCSubMenu);

            var MiscNoCondemn = new Menu("Don't Condemn", "NoCondemn");
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
                {
                    MiscNoCondemn.AddItem(new MenuItem("nC" + hero.ChampionName, hero.ChampionName).SetValue(false));
                }
            }

            config.AddSubMenu(MiscNoCondemn);
        }

        public override void Drawings(Menu config)
        {
            config.AddItem(new MenuItem("disableAll", "Disable All Drawings").SetValue(false));
            config.AddItem(new MenuItem("drawQ", "Draw Q").SetValue(true));
            config.AddItem(new MenuItem("drawE", "Draw E").SetValue(true));
            config.AddItem(new MenuItem("drawComboType", "Draw Combo Type").SetValue(true));
            config.AddItem(new MenuItem("drawCondemnPosition", "Show Best Condemn position").SetValue(true));
        }
    }
}