using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;
using Orbwalking = SebbyLib.Orbwalking;

namespace YuleQuinn
{
    internal class Program
    {
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, W, E, R;
        public static float DrawSpellTime=0;
        public static Obj_AI_Hero jungler = ObjectManager.Player;
        public static int HitChanceNum = 4, tickNum = 4, tickIndex = 0;
        public static Obj_SpawnPoint enemySpawn;
        public static SebbyLib.Prediction.PredictionOutput DrawSpellPos;
        public static List<Obj_AI_Hero> Enemies = new List<Obj_AI_Hero>() , Allies = new List<Obj_AI_Hero>();
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static float dodgeTime = Game.Time;

        static void Main(string[] args) { CustomEvents.Game.OnGameLoad += GameOnOnGameLoad;}

        private static void GameOnOnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Quinn")
                return;
			

            enemySpawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy);

            Q = new Spell(SpellSlot.Q);
            E = new Spell(SpellSlot.E);
            W = new Spell(SpellSlot.W);
            R = new Spell(SpellSlot.R);


            Config = new Menu("奎因", "YuleQuinn", true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            new Quinn().LoadQuinn();


            Config.SubMenu("Misc").SubMenu("Prediction").AddItem(new MenuItem("PredictionMODE", "Prediction MODE", true).SetValue(new StringList(new[] { "Common prediction", "神预判" }, 1)));
            Config.SubMenu("Misc").SubMenu("Prediction").AddItem(new MenuItem("HitChance", "Hit Chance", true).SetValue(new StringList(new[] { "Very High", "High", "Medium" }, 0)));

            Config.SubMenu("Misc").AddItem(new MenuItem("comboDisableMode", "Disable auto-attack in combo mode", true).SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("manaDisable", "Disable mana manager in combo", true).SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("collAA", "Disable auto-attack if Yasuo wall collision", true).SetValue(true));


            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    Enemies.Add(hero);
                }
                if (hero.IsAlly)
                    Allies.Add(hero);
            }

            new WardUsing().LoadWard();
            new Tracker().LoadTrack();

            Config.AddToMainMenu();
            Game.OnUpdate += OnUpdate;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
        }


        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Combo && Config.Item("comboDisableMode", true).GetValue<bool>())
            {
                var t = (Obj_AI_Hero)args.Target;
                if( 4 * Player.GetAutoAttackDamage(t) < t.Health - OktwCommon.GetIncomingDamage(t) && !t.HasBuff("luxilluminatingfraulein") && !Player.HasBuff("sheen"))
                    args.Process = false;
            }

            if (!Player.IsMelee && OktwCommon.CollisionYasuo(Player.ServerPosition, args.Target.Position) &&  Config.Item("collAA", true).GetValue<bool>())
            {
                args.Process = false;
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            tickIndex++;

            if (tickIndex > 4)
                tickIndex = 0;

            if (!LagFree(0))
                return;
        }

        public static bool LagFree(int offset)
        {
            if (tickIndex == offset)
                return true;
            else
                return false;
        }

        public static bool Farm { get { return Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed; } }

        public static bool None { get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.None); } }

        public static bool Combo { get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo); } }

        public static bool LaneClear { get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear); } }

        public static void CastSpell(Spell QWER, Obj_AI_Base target)
        {
            if (Config.Item("PredictionMODE", true).GetValue<StringList>().SelectedIndex == 1)
            {
                SebbyLib.Prediction.SkillshotType CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotLine;
                bool aoe2 = false;

                if (QWER.Type == SkillshotType.SkillshotCircle)
                {
                    CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotCircle;
                    aoe2 = true;
                }

                if (QWER.Width > 80 && !QWER.Collision)
                    aoe2 = true;

                var predInput2 = new SebbyLib.Prediction.PredictionInput
                {
                    Aoe = aoe2,
                    Collision = QWER.Collision,
                    Speed = QWER.Speed,
                    Delay = QWER.Delay,
                    Range = QWER.Range,
                    From = Player.ServerPosition,
                    Radius = QWER.Width,
                    Unit = target,
                    Type = CoreType2
                };
                var poutput2 = SebbyLib.Prediction.Prediction.GetPrediction(predInput2);

                //var poutput2 = QWER.GetPrediction(target);

                if (QWER.Speed != float.MaxValue && OktwCommon.CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
                    return;

                if (Config.Item("HitChance", true).GetValue<StringList>().SelectedIndex == 0)
                {
                    if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.VeryHigh)
                        QWER.Cast(poutput2.CastPosition);
                    else if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 && poutput2.Hitchance >= SebbyLib.Prediction.HitChance.High)
                    {
                        QWER.Cast(poutput2.CastPosition);
                    }

                }
                else if (Config.Item("HitChance", true).GetValue<StringList>().SelectedIndex == 1)
                {
                    if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.High)
                        QWER.Cast(poutput2.CastPosition);

                }
                else if (Config.Item("HitChance", true).GetValue<StringList>().SelectedIndex == 2)
                {
                    if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.Medium)
                        QWER.Cast(poutput2.CastPosition);
                }
            }
            else if (Config.Item("PredictionMODE", true).GetValue<StringList>().SelectedIndex == 0)
            {
                if (Config.Item("HitChance", true).GetValue<StringList>().SelectedIndex == 0)
                {
                    QWER.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                    return;
                }
                else if (Config.Item("HitChance", true).GetValue<StringList>().SelectedIndex == 1)
                {
                    QWER.CastIfHitchanceEquals(target, HitChance.High);
                    return;
                }
                else if (Config.Item("HitChance ", true).GetValue<StringList>().SelectedIndex == 2)
                {
                    QWER.CastIfHitchanceEquals(target, HitChance.Medium);
                    return;
                }
            }
        }

        public static void drawText(string msg, Vector3 Hero, System.Drawing.Color color, int weight = 0)
        {
            var wts = Drawing.WorldToScreen(Hero);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1] + weight, color, msg);
        }
    }
}