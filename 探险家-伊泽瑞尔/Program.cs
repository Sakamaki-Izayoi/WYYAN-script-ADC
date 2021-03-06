﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using System.Diagnostics;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using System.Threading;

namespace Ezreal
{
    class Program
    {
        public const string ChampionName = "Ezreal";
        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell R1;
        public static List<Obj_AI_Base> minions;
        public static bool attackNow = true;
        public static int FarmId;
        public static float qRange = 1100;

        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;

        public static bool Esmart = false;
        public static double WCastTime = 0;
        public static double OverKill = 0;
        public static double OverFarm = 0;
        public static double lag = 0;
        public static double diag = 0;
        public static double diagF = 0;

        public static float DragonDmg = 0;
        public static float DragonDmg2 = 0;
        public static double DragonTime = 0;
        public static double DragonTime2 = 0;
        public static string MsgDebug = "wait";
        public static double NotTime = 0;

        public static Items.Item Potion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);
        public static Items.Item Youmuu = new Items.Item(3142, 0);

        public static int Muramana = 3042;
        public static int Tear = 3070;
        public static int Manamune = 3004;

        public static Menu Config;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 1200);

            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 3000f);
            R1 = new Spell(SpellSlot.R, 3000f);

            Q.SetSkillshot(0.25f, 50f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.2f, 160f, 2000f, false, SkillshotType.SkillshotLine);
            R1.SetSkillshot(1.2f, 200f, 2000f, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
            SpellList.Add(R1);
            //Create the menu
            Config = new Menu("OKTW伊泽瑞尔", ChampionName, true);
            var targetSelectorMenu = new Menu("目标 选择", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            Config.AddSubMenu(new Menu("走砍 设置", "Orbwalking"));

            //Load the orbwalker and add it to the submenu.
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.AddToMainMenu();


            Config.SubMenu("物品").AddItem(new MenuItem("mura", "自动 魔切").SetValue(true));
            Config.SubMenu("物品").AddItem(new MenuItem("stack", "假如蓝量充足丨自动堆叠魔切Or女神").SetValue(false));
            Config.SubMenu("物品").AddItem(new MenuItem("pots", "使用 药水").SetValue(true));

            Config.SubMenu("E 设置").AddItem(new MenuItem("AGC", "被突进自动E").SetValue(true));
            Config.SubMenu("E 设置").AddItem(new MenuItem("smartE", "智能 E 按键").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            Config.SubMenu("E 设置").AddItem(new MenuItem("autoE", "自动 E").SetValue(true));
            Config.SubMenu("E 设置").AddItem(new MenuItem("autoEwall", "尝试 E 翻墙").SetValue(false));

            Config.SubMenu("R 设置").AddItem(new MenuItem("autoR", "自动 R").SetValue(true));
            Config.SubMenu("R 设置").AddItem(new MenuItem("Rcc", "R 状态").SetValue(true));
            Config.SubMenu("R 设置").AddItem(new MenuItem("Raoe", "R 伤害").SetValue(true));
            Config.SubMenu("R 设置").SubMenu("R 抢野怪").AddItem(new MenuItem("Rjungle", "R 抢野怪").SetValue(true));
            Config.SubMenu("R 设置").SubMenu("R 抢野怪").AddItem(new MenuItem("Rdragon", "小龙").SetValue(true));
            Config.SubMenu("R 设置").SubMenu("R 抢野怪").AddItem(new MenuItem("Rbaron", "大龙").SetValue(true));
            Config.SubMenu("R 设置").SubMenu("R 抢野怪").AddItem(new MenuItem("Rred", "红buff").SetValue(true));
            Config.SubMenu("R 设置").SubMenu("R 抢野怪").AddItem(new MenuItem("Rblue", "蓝buff").SetValue(true));
            Config.SubMenu("R 设置").SubMenu("R 抢野怪").AddItem(new MenuItem("Rally", "友方的buff也抢").SetValue(false));
            Config.SubMenu("R 设置").AddItem(new MenuItem("hitchanceR", "非常高命中率R").SetValue(true));
            Config.SubMenu("R 设置").AddItem(new MenuItem("useR", "手动 R 按键").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space

            Config.SubMenu("显示设置").SubMenu("走砍 风格~").AddItem(new MenuItem("OrbDraw", "显示 AA 范围").SetValue(false));
            Config.SubMenu("显示设置").SubMenu("走砍 风格~").AddItem(new MenuItem("1", "禁用走砍 > 范围 > AA线圈"));
            Config.SubMenu("显示设置").SubMenu("走砍 风格~").AddItem(new MenuItem("2", "自己Hp: 0-30 红, 30-60 橙,60-100 绿"));
            Config.SubMenu("显示设置").AddItem(new MenuItem("noti", "显示 通知").SetValue(false));
            Config.SubMenu("显示设置").AddItem(new MenuItem("qRange", "Q 范围").SetValue(false));
            Config.SubMenu("显示设置").AddItem(new MenuItem("wRange", "W 范围").SetValue(false));
            Config.SubMenu("显示设置").AddItem(new MenuItem("eRange", "E 范围").SetValue(false));
            Config.SubMenu("显示设置").AddItem(new MenuItem("onlyRdy", "显示R可击杀目标").SetValue(true));
            Config.SubMenu("显示设置").AddItem(new MenuItem("orb", "走砍 目标").SetValue(true));
            Config.SubMenu("显示设置").AddItem(new MenuItem("qTarget", "Q 目标").SetValue(true));
            Config.SubMenu("显示设置").AddItem(new MenuItem("semi", "手动 R 目标").SetValue(false));

            Config.SubMenu("打钱设置").AddItem(new MenuItem("farmQ", "Q 补刀").SetValue(true));
            Config.SubMenu("打钱设置").AddItem(new MenuItem("LC", "Q 清线").SetValue(true));
            Config.SubMenu("打钱设置").AddItem(new MenuItem("Mana", "Q清线 最低蓝量").SetValue(new Slider(60, 100, 20)));
            Config.SubMenu("打钱设置").AddItem(new MenuItem("LCP", "清线使用Q 触发被动 减少 E,R CD").SetValue(true));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu("骚扰设置").AddItem(new MenuItem("haras" + enemy.BaseSkinName, "基地R目标 : " + enemy.BaseSkinName).SetValue(true));
            Config.AddItem(new MenuItem("wPush", "W 友军 (推塔时)").SetValue(true));
            Config.AddItem(new MenuItem("noob", "青铜 虐菜模式").SetValue(false));
            Config.AddItem(new MenuItem("Hit", "技能 命中").SetValue(new Slider(3, 4, 0)));
            Config.AddItem(new MenuItem("debug", "调试 模式").SetValue(false));

            //Add the events we are going to use:
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        #region Farm
        public static void farmQ()
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 800, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    Q.Cast(mob, true);
                }
            }

            if (!Config.Item("farmQ").GetValue<bool>())
                return;

            minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            foreach (var minion in minions.Where(minion => FarmId != minion.NetworkId && !Orbwalker.InAutoAttackRange(minion) && minion.Health < Q.GetDamage(minion)))
            {
                if (minion.IsMoving)
                    Q.Cast(minion);
                else
                    Q.Cast(minion.Position);
                FarmId = minion.NetworkId;
            }
            if (Config.Item("LC").GetValue<bool>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && !Orbwalking.CanAttack() && (ObjectManager.Player.ManaPercentage() > Config.Item("Mana").GetValue<Slider>().Value || ObjectManager.Player.UnderTurret(false)))
            {
                foreach (var minion in minions.Where(minion => FarmId != minion.NetworkId && Orbwalker.InAutoAttackRange(minion)))
                {
                    if (minion.Health < Q.GetDamage(minion) * 0.8 && minion.Health > minion.FlatPhysicalDamageMod)
                    {
                        if (minion.IsMoving)
                            Q.Cast(minion);
                        else
                            Q.Cast(minion.Position);
                    }

                }
                if (Config.Item("LCP").GetValue<bool>() && ((!E.IsReady() || Game.Time - GetPassiveTime() > -1.5)) && !ObjectManager.Player.UnderTurret(false))
                {
                    foreach (var minion in minions.Where(minion => FarmId != minion.NetworkId && minion.Health > Q.GetDamage(minion) * 1.5 && Orbwalker.InAutoAttackRange(minion)))
                    {
                        if (minion.IsMoving)
                            Q.Cast(minion);
                        else
                            Q.Cast(minion.Position);
                    }
                }
            }
        }

        #endregion

        private static void Game_OnGameUpdate(EventArgs args)
        {
            diagF = diag - Game.Time;
            //debug("diag" + diagF);
            diag = Game.Time;

            if (E.IsReady())
            {
                ManaMenager();
                if (Config.Item("smartE").GetValue<KeyBind>().Active)
                    Esmart = true;
                if (Esmart && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 4)
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
            }
            else
                Esmart = false;

            if (Config.Item("autoE").GetValue<bool>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && E.IsReady())
            {
                ManaMenager();
                var t2 = TargetSelector.GetTarget(950, TargetSelector.DamageType.Physical);
                var t = TargetSelector.GetTarget(1400, TargetSelector.DamageType.Physical);

                if (E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA
                    && ObjectManager.Player.CountEnemiesInRange(260) > 0
                    && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 3
                    && t.Position.Distance(Game.CursorPos) > t.Position.Distance(ObjectManager.Player.Position))
                {
                    if (Config.Item("autoEwall").GetValue<bool>())
                        FindWall();
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                }
                else if (ObjectManager.Player.Health > ObjectManager.Player.MaxHealth * 0.4
                    && !ObjectManager.Player.UnderTurret(true)
                    && (Game.Time - OverKill > 0.4)
                    && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(700) < 3)
                {
                    if (t.IsValidTarget()
                     && ObjectManager.Player.Mana > QMANA + EMANA + WMANA
                     && t.Position.Distance(Game.CursorPos) + 300 < t.Position.Distance(ObjectManager.Player.Position)
                     && Q.IsReady()
                     && Q.GetDamage(t) + E.GetDamage(t) > t.Health
                     && !Orbwalking.InAutoAttackRange(t)
                     && Q.WillHit(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), Q.GetPrediction(t).UnitPosition)
                         )
                    {
                        E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                        debug("E kill Q");
                    }
                    else if (t2.IsValidTarget()
                     && t2.Position.Distance(Game.CursorPos) + 300 < t2.Position.Distance(ObjectManager.Player.Position)
                     && ObjectManager.Player.Mana > EMANA + RMANA
                     && ObjectManager.Player.GetAutoAttackDamage(t2) + E.GetDamage(t2) > t2.Health
                     && !Orbwalking.InAutoAttackRange(t2))
                    {
                        var position = ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range);
                        if (W.IsReady())
                            W.Cast(position, true);
                        E.Cast(position, true);
                        debug("E kill aa");
                        OverKill = Game.Time;
                    }
                    else if (t.IsValidTarget()
                     && ObjectManager.Player.Mana > QMANA + EMANA + WMANA
                     && t.Position.Distance(Game.CursorPos) + 300 < t.Position.Distance(ObjectManager.Player.Position)
                     && W.IsReady()
                     && W.GetDamage(t) + E.GetDamage(t) > t.Health
                     && !Orbwalking.InAutoAttackRange(t)
                     && Q.WillHit(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), Q.GetPrediction(t).UnitPosition)
                         )
                    {
                        E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                        debug("E kill W");
                    }
                }
            }

            if (Q.IsReady())
            {
                ManaMenager();
                if (Config.Item("mura").GetValue<bool>())
                {
                    int Mur = Items.HasItem(Muramana) ? 3042 : 3043;
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Items.HasItem(Mur) && Items.CanUseItem(Mur) && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA)
                    {
                        if (!ObjectManager.Player.HasBuff("Muramana"))
                            Items.UseItem(Mur);
                    }
                    else if (ObjectManager.Player.HasBuff("Muramana") && Items.HasItem(Mur) && Items.CanUseItem(Mur))
                        Items.UseItem(Mur);
                }

                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (ObjectManager.Player.CountEnemiesInRange(900) == 0)
                    t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                else
                    t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var wDmg = W.GetDamage(t);
                    if (qDmg > t.Health)
                        Q.Cast(t, true);
                    if (qDmg * 3 > t.Health && Config.Item("noob").GetValue<bool>() && t.CountAlliesInRange(800) > 1)
                        debug("Q noob mode");
                    else if (t.IsValidTarget(W.Range) && qDmg + wDmg > t.Health)
                    {
                        Q.Cast(t, true);
                        OverKill = Game.Time;
                    }
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA)
                        CastSpell(Q, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if ((Farm && attackNow && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA) && !ObjectManager.Player.UnderTurret(true))
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range) && Config.Item("haras" + enemy.BaseSkinName).GetValue<bool>()))
                        {
                            CastSpell(Q, enemy, Config.Item("Hit").GetValue<Slider>().Value);
                        }
                    }

                    else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            {
                                Q.Cast(enemy, true);
                            }
                        }
                    }
                }
                if ((Game.Time - lag > 0.1) && Farm && attackNow && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + QMANA * 3)
                {
                    farmQ();
                    lag = Game.Time;
                }
                else if (!Farm && Config.Item("stack").GetValue<bool>() && !ObjectManager.Player.HasBuff("Recall") && ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.95 && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo && !t.IsValidTarget() && (Items.HasItem(Tear) || Items.HasItem(Manamune)))
                {
                    Q.Cast(ObjectManager.Player.ServerPosition);
                }

            }
            if (W.IsReady() && attackNow)
            {
                ManaMenager();
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var wDmg = W.GetDamage(t);
                    if (wDmg > t.Health)
                    {
                        CastSpell(W, t, Config.Item("Hit").GetValue<Slider>().Value);
                        OverKill = Game.Time;
                    }
                    else if (wDmg + qDmg > t.Health && Q.IsReady())
                        CastSpell(W, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if (qDmg * 2 > t.Health && Config.Item("noob").GetValue<bool>() && t.CountAlliesInRange(800) > 1)
                        debug("W noob mode");
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(W, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if (Farm && Config.Item("haras" + t.BaseSkinName).GetValue<bool>() && !ObjectManager.Player.UnderTurret(true) && (ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.8 || W.Level > Q.Level) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA + WMANA)
                        CastSpell(W, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            {
                                W.Cast(enemy, true);
                            }
                        }
                    }
                }
            }
            PotionMenager();
            var tr = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            if (Config.Item("useR").GetValue<KeyBind>().Active && tr.IsValidTarget())
            {
                R.CastIfWillHit(tr, 2, true);
                R1.Cast(tr, true, true);
            }
            if (R.IsReady() && Config.Item("Rjungle").GetValue<bool>())
            {
                KsJungle();
            }
            else
                DragonTime = 0;

            if (R.IsReady() && Config.Item("autoR").GetValue<bool>() && ObjectManager.Player.CountEnemiesInRange(800) == 0 && (Game.Time - OverKill > 0.6))
            {
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(R.Range)))
                {
                    if (ValidUlt(target))
                    {
                        float predictedHealth = target.Health + target.HPRegenRate * 2;
                        double Rdmg = R.GetDamage(target);
                        if (Rdmg > predictedHealth)
                            Rdmg = getRdmg(target);
                        var qDmg = Q.GetDamage(target);
                        var wDmg = W.GetDamage(target);
                        if (Rdmg > predictedHealth && target.CountAlliesInRange(400) == 0)
                        {
                            castR(target);
                            debug("R normal");
                        }
                        else if (Rdmg > predictedHealth && target.HasBuff("Recall"))
                        {
                            R.Cast(target, true, true);
                            debug("R recall");
                        }
                        else if ((target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                         target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) ||
                         target.HasBuffOfType(BuffType.Taunt)) && Config.Item("Rcc").GetValue<bool>() &&
                            target.IsValidTarget(Q.Range + E.Range) && Rdmg + qDmg * 4 > predictedHealth)
                        {
                            R.CastIfWillHit(target, 2, true);
                            R.Cast(target, true);
                        }
                        else if (target.IsValidTarget(R.Range) && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Config.Item("Raoe").GetValue<bool>())
                        {
                            R.CastIfWillHit(target, 3, true);
                        }
                        else if (target.IsValidTarget(Q.Range + E.Range) && Rdmg + qDmg + wDmg > predictedHealth && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Config.Item("Raoe").GetValue<bool>())
                        {
                            R.CastIfWillHit(target, 2, true);
                        }
                    }
                }
            }

            if (Orbwalker.GetTarget() == null)
                attackNow = true;
        }

        private static void CastSpell(Spell QWER, Obj_AI_Hero target, int HitChanceNum)
        {
            //HitChance 0 - 2
            // example CastSpell(Q, ts, 2);

            if (HitChanceNum == 0)
                QWER.Cast(target, true);
            else if (HitChanceNum == 1)
                QWER.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
            else if (HitChanceNum == 2)
            {
                if (target.Path.Count() < 2 && (int)QWER.GetPrediction(target).Hitchance > 4)
                    QWER.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
            }
            else if (HitChanceNum == 3)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                float SiteToSite = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed)) * 6 - QWER.Width;
                float BackToFront = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed));
                if (ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) < SiteToSite || ObjectManager.Player.Distance(target.Position) < SiteToSite)
                    QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                else if (target.Path.Count() < 2
                    && (target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) > SiteToSite
                    || Math.Abs(ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) - ObjectManager.Player.Distance(target.Position)) > BackToFront
                    || target.HasBuffOfType(BuffType.Slow) || target.HasBuff("Recall")
                    || (target.Path.Count() == 0 && target.Position == target.ServerPosition)
                    ))
                {
                    if (target.IsFacing(ObjectManager.Player) || target.Path.Count() == 0)
                    {
                        if (ObjectManager.Player.Distance(target.Position) < QWER.Range - ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.Position) / QWER.Speed) + (target.BoundingRadius * 2)))
                            QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                    else
                    {
                        QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                }
            }
            else if (HitChanceNum == 4 )
            {
                var poutput = QWER.GetPrediction(target);
                if ((target.IsFacing(ObjectManager.Player) && (int)poutput.Hitchance == 5) || (target.Path.Count() == 0 && target.Position == target.ServerPosition))
                {
                    if (ObjectManager.Player.Distance(target.Position) < QWER.Range - ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.Position) / QWER.Speed) + (target.BoundingRadius * 2)))
                    {
                        QWER.Cast(poutput.CastPosition);
                    }
                }
                else if ((int)poutput.Hitchance == 5)
                {
                    QWER.Cast(poutput.CastPosition);
                }  
            }
        }

        private static void castR(Obj_AI_Hero target)
        {
            if (Config.Item("hitchanceR").GetValue<bool>())
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if (target.Path.Count() < 2 && (ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) - ObjectManager.Player.Distance(target.Position)) > 400)
                {
                    R.CastIfHitchanceEquals(target, HitChance.High, true);
                }
            }
            else
                R.Cast(target, true);
        }

        public static void debug(string msg)
        {
            MsgDebug = msg;
            NotTime = Game.Time;
            if (Config.Item("debug").GetValue<bool>())
                Game.PrintChat(msg);
        }

        private static double getRdmg(Obj_AI_Base target)
        {
            var rDmg = R.GetDamage(target);
            var dmg = 0;
            PredictionOutput output = R.GetPrediction(target);
            Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
            direction.Normalize();
            List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
            foreach (var enemy in enemies)
            {
                PredictionOutput prediction = R.GetPrediction(enemy);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            var allMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All);
            foreach (var minion in allMinionsR)
            {
                PredictionOutput prediction = R.GetPrediction(minion);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + minion.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            //if (Config.Item("debug").GetValue<bool>())
            //    Game.PrintChat("R collision" + dmg);
            if (dmg > 7)
                return rDmg * 0.7;
            else
                return rDmg - (rDmg * 0.1 * dmg);
        }

        private static void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            attackNow = true;
            if (FarmId != target.NetworkId)
                FarmId = target.NetworkId;
        }

        private static bool Farm
        {
            get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit); }
        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            attackNow = false;
            if (FarmId != args.Target.NetworkId)
                FarmId = args.Target.NetworkId;

            if (Config.Item("mura").GetValue<bool>())
            {
                int Mur = Items.HasItem(Muramana) ? 3042 : 3043;
                if (args.Target.IsEnemy && args.Target.IsValid<Obj_AI_Hero>() && Items.HasItem(Mur) && Items.CanUseItem(Mur) && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA)
                {
                    if (!ObjectManager.Player.HasBuff("Muramana"))
                        Items.UseItem(Mur);
                }
                else if (ObjectManager.Player.HasBuff("Muramana") && Items.HasItem(Mur) && Items.CanUseItem(Mur))
                    Items.UseItem(Mur);
            }
            if (W.IsReady() && Config.Item("wPush").GetValue<bool>() && args.Target.IsValid<Obj_AI_Turret>() && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA + WMANA + RMANA)
            {
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (!ally.IsMe && ally.IsAlly && ally.Distance(ObjectManager.Player.Position) < 600)
                        W.Cast(ally);
                }
            }
        }

        private static bool ValidUlt(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity)
            || target.HasBuffOfType(BuffType.SpellImmunity)
            || target.IsZombie
            || target.HasBuffOfType(BuffType.Invulnerability)
            || target.HasBuffOfType(BuffType.SpellShield)
            )
                return false;
            else
                return true;
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(400) < 3)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    if (Config.Item("autoEwall").GetValue<bool>())
                        FindWall();
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                    debug("E AGC");
                }
            }
            return;
        }


        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.Target != null && Config.Item("autoE").GetValue<bool>() && args.Target.IsMe && unit.IsValid<Obj_AI_Hero>() && unit.IsMelee() && E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA && args.SData.IsAutoAttack()
                    && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 3)
            {
                if (Config.Item("autoEwall").GetValue<bool>())
                    FindWall();
                E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
            }
        }

        private static float GetRealDistance(GameObject target)
        {
            return ObjectManager.Player.ServerPosition.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        public static void ManaMenager()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.Level * 2;
            else
                RMANA = R.Instance.ManaCost; ;

            if (Farm)
                RMANA = RMANA + ObjectManager.Player.CountEnemiesInRange(2500) * 20;

            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }

        public static void PotionMenager()
        {
            if (Config.Item("pots").GetValue<bool>() && !ObjectManager.Player.InFountain() && !ObjectManager.Player.HasBuff("Recall"))
            {
                if (Potion.IsReady() && !ObjectManager.Player.HasBuff("RegenerationPotion", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(700) > 0 && ObjectManager.Player.Health + 200 < ObjectManager.Player.MaxHealth)
                        Potion.Cast();
                    else if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.6)
                        Potion.Cast();
                }
                if (ManaPotion.IsReady() && !ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < RMANA + WMANA + EMANA + RMANA)
                        ManaPotion.Cast();
                }
            }
        }

        private static float GetPassiveTime()
        {
            return
                ObjectManager.Player.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Name == "ezrealrisingspellforce")
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
        }

        private static void FindWall()
        {
            var CircleLineSegmentN = 20;

            var outRadius = 700 / (float)Math.Cos(2 * Math.PI / CircleLineSegmentN);
            var inRadius = 300 / (float)Math.Cos(2 * Math.PI / CircleLineSegmentN);
            var bestPoint = ObjectManager.Player.Position;
            for (var i = 1; i <= CircleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / CircleLineSegmentN;
                var point = new Vector2(ObjectManager.Player.Position.X + outRadius * (float)Math.Cos(angle), ObjectManager.Player.Position.Y + outRadius * (float)Math.Sin(angle)).To3D();
                var point2 = new Vector2(ObjectManager.Player.Position.X + inRadius * (float)Math.Cos(angle), ObjectManager.Player.Position.Y + inRadius * (float)Math.Sin(angle)).To3D();
                if (!point.IsWall() && point2.IsWall() && Game.CursorPos.Distance(point) < Game.CursorPos.Distance(bestPoint))
                    bestPoint = point;
            }
            if (bestPoint != ObjectManager.Player.Position && bestPoint.Distance(Game.CursorPos) < bestPoint.Distance(ObjectManager.Player.Position) && bestPoint.CountEnemiesInRange(500) < 3)
                E.Cast(bestPoint);
        }

        private static void KsJungle()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, float.MaxValue, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            foreach (var mob in mobs)
            {
                //debug(mob.SkinName);
                if (((mob.SkinName == "SRU_Dragon" && Config.Item("Rdragon").GetValue<bool>())
                    || (mob.SkinName == "SRU_Baron" && Config.Item("Rbaron").GetValue<bool>())
                    || (mob.SkinName == "SRU_Red" && Config.Item("Rred").GetValue<bool>())
                    || (mob.SkinName == "SRU_Blue" && Config.Item("Rblue").GetValue<bool>()))
                    && (mob.CountAlliesInRange(1000) == 0 || Config.Item("Rally").GetValue<bool>())
                    && mob.Health < mob.MaxHealth
                    && mob.Distance(Player.Position) > 1000
                    )
                {
                    if (DragonDmg == 0)
                        DragonDmg = mob.Health;

                    if (Game.Time - DragonTime > 3)
                    {
                        if (DragonDmg - mob.Health > 0)
                        {
                            DragonDmg = mob.Health;
                        }
                        DragonTime = Game.Time;
                    }
                    else
                    {
                        var DmgSec = (DragonDmg - mob.Health) * (Math.Abs(DragonTime - Game.Time) / 3);
                        //debug("DS  " + DmgSec);
                        if (DragonDmg - mob.Health > 0)
                        {
                            debug(mob.SkinName + " " + (DmgSec / 3) + " dmg per sec");
                            var timeTravel = GetUltTravelTime(ObjectManager.Player, R.Speed, R.Delay, mob.Position);
                            var timeR = (mob.Health - R.GetDamage(mob)) / (DmgSec / 3);
                            //debug("timeTravel " + timeTravel + "timeR " + timeR + "d " + R.GetDamage(mob));
                            if (timeTravel > timeR)
                            {
                                R.Cast(mob.Position);
                            }
                        }
                        else
                        {
                            DragonDmg = mob.Health;
                        }
                        //debug("" + GetUltTravelTime(ObjectManager.Player, R.Speed, R.Delay, mob.Position));
                    }
                }
            }
        }

        private static float GetUltTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos)
        {
            float distance = Vector3.Distance(source.ServerPosition, targetpos);
            float missilespeed = speed;

            return (distance / missilespeed + delay);
        }


        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("OrbDraw").GetValue<bool>())
            {
                if (ObjectManager.Player.HealthPercentage() > 60)
                    Utility.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.GreenYellow, 2, 1);
                else if (ObjectManager.Player.HealthPercentage() > 30)
                    Utility.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.Orange, 3, 1);
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.Red, 4, 1);
            }
            if (Config.Item("qRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
            }
            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
            }
            if (Config.Item("eRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
            }
            if (Config.Item("rRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (R.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
            }

            if (Config.Item("orb").GetValue<bool>())
            {
                var orbT = Orbwalker.GetTarget();

                if (orbT.IsValidTarget())
                {
                    if (orbT.Health > orbT.MaxHealth * 0.6)
                        Utility.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.GreenYellow, 5, 1);
                    else if (orbT.Health > orbT.MaxHealth * 0.3)
                        Utility.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.Orange, 10, 1);
                    else
                        Utility.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.Red, 10, 1);
                }

            }

            if (Config.Item("noti").GetValue<bool>())
            {
                if (Game.Time - NotTime < 10)
                {
                    Drawing.DrawText(Drawing.Width * 0.01f, Drawing.Height * 0.5f, System.Drawing.Color.Red, MsgDebug);
                }
                else
                {
                    MsgDebug = "wait";
                    Drawing.DrawText(Drawing.Width * 0.01f, Drawing.Height * 0.5f, System.Drawing.Color.GreenYellow, MsgDebug);
                }

                var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
                if (target.IsValidTarget())
                {

                    var poutput = Q.GetPrediction(target);
                    if ((int)poutput.Hitchance == 5)
                        Render.Circle.DrawCircle(poutput.CastPosition, 50, System.Drawing.Color.YellowGreen);
                    if (Q.GetDamage(target) > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }
                    else if (Q.GetDamage(target) + W.GetDamage(target) > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }
                    else if (Q.GetDamage(target) + W.GetDamage(target) + E.GetDamage(target) > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W + E kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }
                }
            }
        }
    }
}