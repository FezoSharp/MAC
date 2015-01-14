using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAC.Model
{
    class ItemHandler : Utilitario
    {

        private static Menu _myMenu;

        private string ActiveName { get; set; }

        private int ActiveId { get; set; }

        private int Range { get; set; }

        private string Type { get; set; }

        private int Mode { get; set; } // 0 = target, 1 = on click, 2 = toggle

        private static readonly List<ItemHandler> ItemList = new List<ItemHandler>();

        public static bool UseTargetted;

        public static bool KillableTarget;

        public static Obj_AI_Hero Target;

        private static int lastMura;

        private static void CreateList()
        {
            ItemList.Add(new ItemHandler
            {
                ActiveId = 3144,
                ActiveName = "Bilgewater Cutlass",
                Type = "Offensive",
                Range = 450,
                Mode = 0,
            });

            ItemList.Add(new ItemHandler
            {
                ActiveId = 3188,
                ActiveName = "Blackfire Torch",
                Type = "Offensive",
                Range = 750,
                Mode = 0,
            });

            ItemList.Add(new ItemHandler
            {
                ActiveId = 3153,
                ActiveName = "Blade of the Ruined King",
                Type = "Offensive",
                Range = 450,
                Mode = 0,
            });

            ItemList.Add(new ItemHandler
            {
                ActiveId = 3128,
                ActiveName = "Deathfire Grasp",
                Type = "Offensive",
                Range = 750,
                Mode = 0,
            });

            ItemList.Add(new ItemHandler
            {
                ActiveId = 3146,
                ActiveName = "Hextech Gunblade",
                Type = "Offensive",
                Range = 700,
                Mode = 0,
            });

            ItemList.Add(new ItemHandler
            {
                ActiveId = 3042,
                ActiveName = "Muramana",
                Type = "Offensive",
                Range = int.MaxValue,
                Mode = 2,
            });

            ItemList.Add(new ItemHandler
            {
                ActiveId = 3074,
                ActiveName = "Ravenous Hydra",
                Type = "Offensive",
                Range = 400,
                Mode = 1,
            });

            ItemList.Add(new ItemHandler
            {
                ActiveId = 3077,
                ActiveName = "Tiamat",
                Type = "Offensive",
                Range = 400,
                Mode = 1,
            });

            ItemList.Add(new ItemHandler
            {
                ActiveId = 3142,
                ActiveName = "Youmuu's Ghostblade",
                Type = "Offensive",
                Range = (int)(ObjectManager.Player.AttackRange * 2),
                Mode = 1,
            });

        }

        public static void AddToMenu(Menu theMenu)
        {
            _myMenu = theMenu;

            //add item list to menu
            CreateList();

            var offensiveItem = new Menu("Offensive Items", "Offensive Items");
            {
                foreach (var item in ItemList)
                {
                    AddOffensiveItem(offensiveItem, item);
                }
                _myMenu.AddSubMenu(offensiveItem);
            }


            Orbwalking.AfterAttack += AfterAttack;
            Orbwalking.OnAttack += OnAttack;
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        private static void AddOffensiveItem(Menu subMenu, ItemHandler item)
        {
            var active = new Menu(item.ActiveName, item.ActiveName);
            {
                active.AddItem(new MenuItem(item.ActiveName, item.ActiveName)).SetValue(true);
                active.AddItem(new MenuItem(item.ActiveName + "dmgCalc", "Add to damage Calculation").SetValue(true));
                active.AddItem(new MenuItem(item.ActiveName + "killAble", "Use only if enemy is killable").SetValue(false));
                active.AddItem(new MenuItem(item.ActiveName + "always", "Always use").SetValue(item.Mode == 1 || item.Mode == 2));
                active.AddItem(new MenuItem(item.ActiveName + "myHP", "Use if HP <= %").SetValue(new Slider(25)));
                active.AddItem(new MenuItem(item.ActiveName + "enemyHP", "Use if target HP <= %").SetValue(new Slider(50)));

                subMenu.AddSubMenu(active);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Target == null || ObjectManager.Player.IsDead)
                return;

            if (!UseTargetted)
                return;

            foreach (var item in ItemList.Where(x => x.Mode == 0 && Items.HasItem(x.ActiveId) && ShouldUse(x.ActiveName)))
            {
                if (Target != null && Items.CanUseItem(item.ActiveId))
                {
                    if (AlwaysUse(item.ActiveName))
                        Items.UseItem(item.ActiveId, Target);

                    if (KillableTarget)
                        Items.UseItem(item.ActiveId, Target);

                    if (ObjectManager.Player.HealthPercentage() <= UseAtMyHp(item.ActiveName) && !OnlyIfKillable(item.ActiveName))
                    {
                        Items.UseItem(item.ActiveId, Target);
                    }

                    if (Target.HealthPercentage() <= UseAtEnemyHp(item.ActiveName) && !OnlyIfKillable(item.ActiveName))
                    {
                        Items.UseItem(item.ActiveId, Target);
                    }
                }
            }

            if (ObjectManager.Player.HasBuff("Muramana") && Items.CanUseItem(3042) && Environment.TickCount - lastMura > 5000)
            {
                Items.UseItem(3042);
            }

            //reset mode
            UseTargetted = false;
            Target = null;
            KillableTarget = false;
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe || !(target is Obj_AI_Hero))
                return;

            foreach (var item in ItemList.Where(x => x.Mode == 1 && Items.CanUseItem(x.ActiveId) && ShouldUse(x.ActiveName)))
            {
                if (AlwaysUse(item.ActiveName))
                    Items.UseItem(item.ActiveId);

                if (KillableTarget)
                    Items.UseItem(item.ActiveId);

                if (ObjectManager.Player.HealthPercentage() <= UseAtMyHp(item.ActiveName) && !OnlyIfKillable(item.ActiveName))
                {
                    Items.UseItem(item.ActiveId);
                }

                if (Target.HealthPercentage() <= UseAtEnemyHp(item.ActiveName) && !OnlyIfKillable(item.ActiveName))
                {
                    Items.UseItem(item.ActiveId);
                }
            }
        }

        private static void OnAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe || !(target is Obj_AI_Hero))
                return;

            foreach (var item in ItemList.Where(x => x.Mode == 2 && Items.CanUseItem(x.ActiveId) && ShouldUse(x.ActiveName)))
            {
                if (!ObjectManager.Player.HasBuff("Muramana"))
                {
                    if (AlwaysUse(item.ActiveName))
                    {
                        Items.UseItem(item.ActiveId, Target);
                        lastMura = Environment.TickCount;
                    }

                    if (KillableTarget)
                    {
                        Items.UseItem(item.ActiveId, Target);
                        lastMura = Environment.TickCount;
                    }

                    if (ObjectManager.Player.HealthPercentage() <= UseAtMyHp(item.ActiveName) && !OnlyIfKillable(item.ActiveName))
                    {
                        Items.UseItem(item.ActiveId, Target);
                        lastMura = Environment.TickCount;
                    }

                    if (Target.HealthPercentage() <= UseAtEnemyHp(item.ActiveName) && !OnlyIfKillable(item.ActiveName))
                    {
                        Items.UseItem(item.ActiveId, Target);
                        lastMura = Environment.TickCount;
                    }
                }
            }
        }
        public static float CalcDamage(Obj_AI_Base target, double currentDmg)
        {
            double dmg = currentDmg;

            foreach (var item in ItemList.Where(x => Items.HasItem(x.ActiveId) && ShouldUse(x.ActiveName) && Items.CanUseItem(x.ActiveId) && AddToDmgCalc(x.ActiveName)))
            {
                //bilge
                if (item.ActiveId == 3144)
                    dmg += ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Bilgewater);

                //blackfire
                if (item.ActiveId == 3188)
                    dmg += ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.BlackFireTorch);

                //Botrk
                if (item.ActiveId == 3153)
                    dmg += ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Botrk);

                //dfg
                if (item.ActiveId == 3128)
                    dmg += ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Dfg);

                //hextech
                if (item.ActiveId == 3146)
                    dmg += ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Hexgun);

                //hydra
                if (item.ActiveId == 3074)
                    dmg += ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Hydra);

                //tiamat
                if (item.ActiveId == 3077)
                    dmg += ObjectManager.Player.GetItemDamage(target, Damage.DamageItems.Tiamat);
            }

            //dmg calc for dfg/blackfire
            if (Items.CanUseItem(3188) || Items.CanUseItem(3128))
                dmg += dmg * 1.2;

            return (float)dmg;
        }

        private static bool ShouldUse(string name)
        {
            return _myMenu.Item(name).GetValue<bool>();
        }

        private static bool AlwaysUse(string name)
        {
            return _myMenu.Item(name + "always").GetValue<bool>();
        }

        private static bool AddToDmgCalc(string name)
        {
            return _myMenu.Item(name + "dmgCalc").GetValue<bool>();
        }

        private static bool OnlyIfKillable(string name)
        {
            return _myMenu.Item(name + "killAble").GetValue<bool>();
        }

        private static int UseAtMyHp(string name)
        {
            return _myMenu.Item(name + "myHP").GetValue<Slider>().Value;
        }

        private static int UseAtEnemyHp(string name)
        {
            return _myMenu.Item(name + "enemyHP").GetValue<Slider>().Value;
        }


    }
}

