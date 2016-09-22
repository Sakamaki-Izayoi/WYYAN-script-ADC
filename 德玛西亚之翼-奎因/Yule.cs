﻿using LeagueSharp;
using LeagueSharp.Common;
using SebbyLib;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using Orbwalking = SebbyLib.Orbwalking;

namespace YuleQuinn
{
    class ChampionInfo
    {
        public int NetworkId { get; set; }

        public Vector3 LastVisablePos { get; set; }
        public float LastVisableTime { get; set; }
        public Vector3 PredictedPos { get; set; }

        public float StartRecallTime { get; set; }
        public float AbortRecallTime { get; set; }
        public float FinishRecallTime { get; set; }

        public ChampionInfo()
        {
            LastVisableTime = Game.Time;
            StartRecallTime = 0;
            AbortRecallTime = 0;
            FinishRecallTime = 0;
        }
    }
    class HiddenObj
    {
        public int type;
        public float endTime { get; set; }
        public Vector3 pos { get; set; }
    }

    class WardUsing
    {
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private Menu Config = Program.Config;
        private bool rengar = false;
        Obj_AI_Hero Vayne = null;
        private static Spell Q, W, E, R;

        public static List<HiddenObj> HiddenObjList = new List<HiddenObj>();

        private Items.Item
            VisionWard = new Items.Item(2043, 550f),
            OracleLens = new Items.Item(3364, 550f),
            WardN = new Items.Item(2044, 600f),
            TrinketN = new Items.Item(3340, 600f),
            SightStone = new Items.Item(2049, 600f),
            EOTOasis = new Items.Item(2302, 600f),
            EOTEquinox = new Items.Item(2303, 600f),
            EOTWatchers = new Items.Item(2301, 600f),
            FarsightOrb = new Items.Item(3342, 4000f),
            ScryingOrb = new Items.Item(3363, 3500f);

        public void LoadWard()
        {
            Q = new Spell(SpellSlot.Q);
            E = new Spell(SpellSlot.E);
            W = new Spell(SpellSlot.W);
            R = new Spell(SpellSlot.R);

            Config.SubMenu("Misc").SubMenu("AutoWard OKTW©").AddItem(new MenuItem("AutoWard", "Auto Ward").SetValue(true));
            Config.SubMenu("Misc").SubMenu("AutoWard OKTW©").AddItem(new MenuItem("autoBuy", "Auto buy blue trinket after lvl 9").SetValue(false));
            Config.SubMenu("Misc").SubMenu("AutoWard OKTW©").AddItem(new MenuItem("AutoWardBlue", "Auto Blue Trinket").SetValue(true));
            Config.SubMenu("Misc").SubMenu("AutoWard OKTW©").AddItem(new MenuItem("AutoWardCombo", "Only combo mode").SetValue(true));
            Config.SubMenu("Misc").SubMenu("AutoWard OKTW©").AddItem(new MenuItem("AutoWardPink", "Auto VisionWard, OracleLens").SetValue(true));

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    if (hero.ChampionName == "Rengar")
                        rengar = true;
                    if (hero.ChampionName == "Vayne")
                        Vayne = hero;
                }
            }

            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (!Program.LagFree(0) || Player.IsRecalling() || Player.IsDead)
                return;

            foreach (var obj in HiddenObjList)
            {
                if (obj.endTime < Game.Time)
                {
                    HiddenObjList.Remove(obj);
                    return;
                }
            }

            if (Config.Item("autoBuy").GetValue<bool>() && Player.InFountain() && !ScryingOrb.IsOwned() && Player.Level >= 9)
                Player.BuyItem(ItemId.Farsight_Orb_Trinket);

            if (rengar && Player.HasBuff("rengarralertsound"))
                CastVisionWards(Player.ServerPosition);

            if (Vayne != null && Vayne.IsValidTarget(1000) && Vayne.HasBuff("vaynetumblefade"))
                CastVisionWards(Vayne.ServerPosition);

            AutoWardLogic();
        }

        private void AutoWardLogic()
        {
            foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValid && !enemy.IsVisible && !enemy.IsDead))
            {
                var need = Tracker.ChampionInfoList.Find(x => x.NetworkId == enemy.NetworkId);

                if (need == null || need.PredictedPos == null)
                    continue;

                var PPDistance = need.PredictedPos.Distance(Player.Position);

                if (PPDistance > 1400)
                    continue;

                var timer = Game.Time - need.LastVisableTime;

                if (timer > 1 && timer < 3)
                {
                    if (Program.Combo && PPDistance < 1500 && Player.ChampionName == "Quinn" && W.IsReady() && Config.Item("autoW", true).GetValue<bool>())
                    {
                        W.Cast();
                    }

                    if (Program.Combo && PPDistance < 900 && Player.ChampionName == "Karhus" && Q.IsReady() && Player.CountEnemiesInRange(900) == 0)
                    {
                        Q.Cast(need.PredictedPos);
                    }

                    if (Program.Combo && PPDistance < 1400 && Player.ChampionName == "Ashe" && E.IsReady() && Player.CountEnemiesInRange(800) == 0 && Config.Item("autoE", true).GetValue<bool>())
                    {
                        E.Cast(Player.Position.Extend(need.PredictedPos, 5000));
                    }

                    if (PPDistance < 800 && Player.ChampionName == "MissFortune" && E.IsReady() && Program.Combo && Player.Mana > 200)
                    {
                        E.Cast(Player.Position.Extend(need.PredictedPos, 800));
                    }

                    if (!Player.IsWindingUp && PPDistance < 800 && Player.ChampionName == "Caitlyn" && W.IsReady() && Player.Mana > 200f && Config.Item("bushW", true).GetValue<bool>() && Utils.TickCount - W.LastCastAttemptT > 2000)
                    {
                        W.Cast(need.PredictedPos);
                    }
                    if (!Player.IsWindingUp && PPDistance < 760 && Player.ChampionName == "Jhin" && E.IsReady() && Player.Mana > 200f && Config.Item("bushE", true).GetValue<bool>() && Utils.TickCount - E.LastCastAttemptT > 2000)
                    {
                        E.Cast(need.PredictedPos);
                    }
                }

                if (timer < 4)
                {
                    if (Config.Item("AutoWardCombo").GetValue<bool>() && !Program.Combo)
                        return;

                    if (NavMesh.IsWallOfGrass(need.PredictedPos, 0))
                    {
                        if (PPDistance < 600 && Config.Item("AutoWard").GetValue<bool>())
                        {
                            if (TrinketN.IsReady())
                            {
                                TrinketN.Cast(need.PredictedPos);
                                need.LastVisableTime = Game.Time - 5;
                            }
                            else if (SightStone.IsReady())
                            {
                                SightStone.Cast(need.PredictedPos);
                                need.LastVisableTime = Game.Time - 5;
                            }
                            else if (WardN.IsReady())
                            {
                                WardN.Cast(need.PredictedPos);
                                need.LastVisableTime = Game.Time - 5;
                            }
                            else if (EOTOasis.IsReady())
                            {
                                EOTOasis.Cast(need.PredictedPos);
                                need.LastVisableTime = Game.Time - 5;
                            }
                            else if (EOTEquinox.IsReady())
                            {
                                EOTEquinox.Cast(need.PredictedPos);
                                need.LastVisableTime = Game.Time - 5;
                            }
                            else if (EOTWatchers.IsReady())
                            {
                                EOTWatchers.Cast(need.PredictedPos);
                                need.LastVisableTime = Game.Time - 5;
                            }
                        }

                        if (Config.Item("AutoWardBlue").GetValue<bool>())
                        {
                            if (FarsightOrb.IsReady())
                            {
                                FarsightOrb.Cast(need.PredictedPos);
                                need.LastVisableTime = Game.Time - 5;
                            }
                            else if (ScryingOrb.IsReady())
                            {
                                ScryingOrb.Cast(need.PredictedPos);
                                need.LastVisableTime = Game.Time - 5;
                            }
                        }
                    }
                }
            }
        }

        private void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!sender.IsEnemy || sender.IsAlly)
                return;

            if (sender is MissileClient)
            {
                var missile = (MissileClient)sender;

                if (!missile.SpellCaster.IsVisible)
                {

                    if ((missile.SData.Name == "BantamTrapShort" || missile.SData.Name == "BantamTrapBounceSpell") && !HiddenObjList.Exists(x => missile.EndPosition == x.pos))
                        AddWard("teemorcast", missile.EndPosition);
                }
            }
            else if (sender.Type == GameObjectType.obj_AI_Minion)
            {
                if ((sender.Name.ToLower() == "visionward" || sender.Name.ToLower() == "sightward") && !HiddenObjList.Exists(x => x.pos.Distance(sender.Position) < 100))
                {
                    foreach (var obj in HiddenObjList)
                    {
                        if (obj.pos.Distance(sender.Position) < 400)
                        {
                            if (obj.type == 0)
                            {
                                HiddenObjList.Remove(obj);
                                return;
                            }
                        }
                    }

                    var dupa = (Obj_AI_Minion)sender;
                    if (dupa.Mana == 0)
                        HiddenObjList.Add(new HiddenObj() { type = 2, pos = sender.Position, endTime = float.MaxValue });
                    else
                        HiddenObjList.Add(new HiddenObj() { type = 1, pos = sender.Position, endTime = Game.Time + dupa.Mana });
                }
            }
            else if (rengar && sender.Position.Distance(Player.Position) < 800)
            {
                switch (sender.Name)
                {
                    case "Rengar_LeapSound.troy":
                        CastVisionWards(sender.Position);
                        break;
                    case "Rengar_Base_R_Alert":
                        CastVisionWards(sender.Position);
                        break;
                }
            }
        }

        private void GameObject_OnDelete(GameObject sender, EventArgs args)
        {

            if (!sender.IsEnemy || sender.IsAlly || sender.Type != GameObjectType.obj_AI_Minion)
                return;

            foreach (var obj in HiddenObjList)
            {
                if (obj.pos == sender.Position)
                {
                    HiddenObjList.Remove(obj);
                    return;
                }
                else if (obj.type == 3 && obj.pos.Distance(sender.Position) < 100)
                {
                    HiddenObjList.Remove(obj);
                    return;
                }
                else if (obj.pos.Distance(sender.Position) < 400)
                {
                    if (obj.type == 2 && sender.Name.ToLower() == "visionward")
                    {
                        HiddenObjList.Remove(obj);
                        return;
                    }
                    else if ((obj.type == 0 || obj.type == 1) && sender.Name.ToLower() == "sightward")
                    {
                        HiddenObjList.Remove(obj);
                        return;
                    }
                }
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && !sender.IsMinion && sender.Type == GameObjectType.obj_AI_Hero)
            {
                if (args.Target == null)
                    AddWard(args.SData.Name.ToLower(), args.End);

                if ((OracleLens.IsReady() || VisionWard.IsReady()) && sender.Distance(Player.Position) < 800)
                {
                    switch (args.SData.Name.ToLower())
                    {
                        case "akalismokebomb":
                            CastVisionWards(sender.ServerPosition);
                            break;
                        case "deceive":
                            CastVisionWards(sender.ServerPosition);
                            break;
                        case "khazixr":
                            CastVisionWards(sender.ServerPosition);
                            break;
                        case "khazixrlong":
                            CastVisionWards(sender.ServerPosition);
                            break;
                        case "talonshadowassault":
                            CastVisionWards(sender.ServerPosition);
                            break;
                        case "monkeykingdecoy":
                            CastVisionWards(sender.ServerPosition);
                            break;
                        case "RengarR":
                            CastVisionWards(sender.ServerPosition);
                            break;
                        case "TwitchHideInShadows":
                            CastVisionWards(sender.ServerPosition);
                            break;
                    }
                }
            }
        }

        private void AddWard(string name, Vector3 posCast)
        {
            switch (name)
            {
                //PINKS
                case "visionward":
                    HiddenObjList.Add(new HiddenObj() { type = 2, pos = posCast, endTime = float.MaxValue });
                    break;
                case "trinkettotemlvl3B":
                    HiddenObjList.Add(new HiddenObj() { type = 1, pos = posCast, endTime = Game.Time + 180 });
                    break;
                //SIGH WARD
                case "itemghostward":
                    HiddenObjList.Add(new HiddenObj() { type = 1, pos = posCast, endTime = Game.Time + 180 });
                    break;
                case "wrigglelantern":
                    HiddenObjList.Add(new HiddenObj() { type = 1, pos = posCast, endTime = Game.Time + 180 });
                    break;
                case "sightward":
                    HiddenObjList.Add(new HiddenObj() { type = 1, pos = posCast, endTime = Game.Time + 180 });
                    break;
                case "itemferalflare":
                    HiddenObjList.Add(new HiddenObj() { type = 1, pos = posCast, endTime = Game.Time + 180 });
                    break;
                //TRINKET
                case "trinkettotemlvl1":
                    HiddenObjList.Add(new HiddenObj() { type = 1, pos = posCast, endTime = Game.Time + 60 });
                    break;
                case "trinkettotemlvl2":
                    HiddenObjList.Add(new HiddenObj() { type = 1, pos = posCast, endTime = Game.Time + 120 });
                    break;
                case "trinkettotemlvl3":
                    HiddenObjList.Add(new HiddenObj() { type = 1, pos = posCast, endTime = Game.Time + 180 });
                    break;
                //others
                case "teemorcast":
                    HiddenObjList.Add(new HiddenObj() { type = 3, pos = posCast, endTime = Game.Time + 300 });
                    break;
                case "noxious trap":
                    HiddenObjList.Add(new HiddenObj() { type = 3, pos = posCast, endTime = Game.Time + 300 });
                    break;
                case "JackInTheBox":
                    HiddenObjList.Add(new HiddenObj() { type = 3, pos = posCast, endTime = Game.Time + 100 });
                    break;
                case "Jack In The Box":
                    HiddenObjList.Add(new HiddenObj() { type = 3, pos = posCast, endTime = Game.Time + 100 });
                    break;
            }
        }

        private void CastVisionWards(Vector3 position)
        {
            if (Config.Item("AutoWardPink").GetValue<bool>())
            {
                if (OracleLens.IsReady())
                    OracleLens.Cast(Player.Position.Extend(position, OracleLens.Range));
                else if (VisionWard.IsReady())
                    VisionWard.Cast(Player.Position.Extend(position, VisionWard.Range));
            }
        }
    }
    class LoadDash
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Menu Config = Program.Config;
        private static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private static Spell DashSpell;

        public LoadDash(Spell qwer)
        {
            DashSpell = qwer;

            Config.SubMenu(qwer.Slot + " Config").AddItem(new MenuItem("DashMode", "Dash MODE", true).SetValue(new StringList(new[] { "Game Cursor", "Side", "Safe position" }, 2)));
            Config.SubMenu(qwer.Slot + " Config").AddItem(new MenuItem("EnemyCheck", "Block dash in x enemies ", true).SetValue(new Slider(3, 5, 0)));
            Config.SubMenu(qwer.Slot + " Config").AddItem(new MenuItem("WallCheck", "Block dash in wall", true).SetValue(true));
            Config.SubMenu(qwer.Slot + " Config").AddItem(new MenuItem("TurretCheck", "Block dash under turret", true).SetValue(true));
            Config.SubMenu(qwer.Slot + " Config").AddItem(new MenuItem("AAcheck", "Dash only in AA range", true).SetValue(true));

            Config.SubMenu(qwer.Slot + " Config").SubMenu("Gapcloser").AddItem(new MenuItem("GapcloserMode", "Gapcloser MODE", true).SetValue(new StringList(new[] { "Game Cursor", "Away - safe position" }, 1)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(qwer.Slot + " Config").SubMenu("Gapcloser").AddItem(new MenuItem("EGCchampion" + enemy.ChampionName, enemy.ChampionName, true).SetValue(true));

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (DashSpell.IsReady() && Config.Item("EGCchampion" + gapcloser.Sender.ChampionName, true).GetValue<bool>())
            {
                int GapcloserMode = Config.Item("GapcloserMode", true).GetValue<StringList>().SelectedIndex;
                if (GapcloserMode == 0)
                {
                    var bestpoint = Player.Position.Extend(Game.CursorPos, DashSpell.Range);
                    if (IsGoodPosition(bestpoint))
                        DashSpell.Cast(bestpoint);
                }
                else
                {
                    var points = OktwCommon.CirclePoints(10, DashSpell.Range, Player.Position);
                    var bestpoint = Player.Position.Extend(gapcloser.Sender.Position, -DashSpell.Range);
                    int enemies = bestpoint.CountEnemiesInRange(DashSpell.Range);
                    foreach (var point in points)
                    {
                        int count = point.CountEnemiesInRange(DashSpell.Range);
                        if (count < enemies)
                        {
                            enemies = count;
                            bestpoint = point;
                        }
                        else if (count == enemies && Game.CursorPos.Distance(point) < Game.CursorPos.Distance(bestpoint))
                        {
                            enemies = count;
                            bestpoint = point;
                        }
                    }
                    if (IsGoodPosition(bestpoint))
                        DashSpell.Cast(bestpoint);
                }
            }
        }

        public Vector3 CastDash(bool asap = false)
        {
            int DashMode = Config.Item("DashMode", true).GetValue<StringList>().SelectedIndex;

            Vector3 bestpoint = Vector3.Zero;
            if (DashMode == 0)
            {
                bestpoint = Player.Position.Extend(Game.CursorPos, DashSpell.Range);
            }
            else if (DashMode == 1)
            {
                var orbT = Orbwalker.GetTarget();
                if (orbT != null && orbT is Obj_AI_Hero)
                {
                    Vector2 start = Player.Position.To2D();
                    Vector2 end = orbT.Position.To2D();
                    var dir = (end - start).Normalized();
                    var pDir = dir.Perpendicular();

                    var rightEndPos = end + pDir * Player.Distance(orbT);
                    var leftEndPos = end - pDir * Player.Distance(orbT);

                    var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, Player.Position.Z);
                    var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, Player.Position.Z);

                    if (Game.CursorPos.Distance(rEndPos) < Game.CursorPos.Distance(lEndPos))
                    {
                        bestpoint = Player.Position.Extend(rEndPos, DashSpell.Range);
                    }
                    else
                    {
                        bestpoint = Player.Position.Extend(lEndPos, DashSpell.Range);
                    }
                }
            }
            else if (DashMode == 2)
            {
                var points = OktwCommon.CirclePoints(15, DashSpell.Range, Player.Position);
                bestpoint = Player.Position.Extend(Game.CursorPos, DashSpell.Range);
                int enemies = bestpoint.CountEnemiesInRange(350);
                foreach (var point in points)
                {
                    int count = point.CountEnemiesInRange(350);
                    if (!InAARange(point))
                        continue;
                    if (count < enemies)
                    {
                        enemies = count;
                        bestpoint = point;
                    }
                    else if (count == enemies && Game.CursorPos.Distance(point) < Game.CursorPos.Distance(bestpoint))
                    {
                        enemies = count;
                        bestpoint = point;
                    }
                }
            }

            if (bestpoint.IsZero)
                return Vector3.Zero;

            var isGoodPos = IsGoodPosition(bestpoint);

            if (asap && isGoodPos)
            {
                return bestpoint;
            }
            else if (isGoodPos && InAARange(bestpoint))
            {
                return bestpoint;
            }
            return Vector3.Zero;
        }

        public bool InAARange(Vector3 point)
        {
            if (!Config.Item("AAcheck", true).GetValue<bool>())
                return true;
            else if (Orbwalker.GetTarget() != null && Orbwalker.GetTarget().Type == GameObjectType.obj_AI_Hero)
            {
                return point.Distance(Orbwalker.GetTarget().Position) < Player.AttackRange;
            }
            else
            {
                return point.CountEnemiesInRange(Player.AttackRange) > 0;
            }
        }

        public bool IsGoodPosition(Vector3 dashPos)
        {
            if (Config.Item("WallCheck", true).GetValue<bool>())
            {
                float segment = DashSpell.Range / 5;
                for (int i = 1; i <= 5; i++)
                {
                    if (Player.Position.Extend(dashPos, i * segment).IsWall())
                        return false;
                }
            }

            if (Config.Item("TurretCheck", true).GetValue<bool>())
            {
                if (dashPos.UnderTurret(true))
                    return false;
            }

            var enemyCheck = Config.Item("EnemyCheck", true).GetValue<Slider>().Value;
            var enemyCountDashPos = dashPos.CountEnemiesInRange(600);

            if (enemyCheck > enemyCountDashPos)
                return true;

            var enemyCountPlayer = Player.CountEnemiesInRange(400);

            if (enemyCountDashPos <= enemyCountPlayer)
                return true;

            return false;
        }
    }

    class Tracker
    {
        public static List<ChampionInfo> ChampionInfoList = new List<ChampionInfo>();
        private Vector3 EnemySpawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy).Position;

        public void LoadTrack()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    ChampionInfoList.Add(new ChampionInfo() { NetworkId = hero.NetworkId, LastVisablePos = hero.Position });
                }
            }

            Game.OnUpdate += OnUpdate;
        }

        private void OnUpdate(EventArgs args)
        {
            if (!Program.LagFree(0))
                return;

            foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValid))
            {
                var ChampionInfoOne = ChampionInfoList.Find(x => x.NetworkId == enemy.NetworkId);
                if (enemy.IsDead)
                {
                    if (ChampionInfoOne != null)
                    {
                        ChampionInfoOne.NetworkId = enemy.NetworkId;
                        ChampionInfoOne.LastVisablePos = EnemySpawn;
                        ChampionInfoOne.LastVisableTime = Game.Time;
                        ChampionInfoOne.PredictedPos = EnemySpawn;
                    }
                }
                else if (enemy.IsVisible)
                {
                    Vector3 prepos = enemy.Position;

                    if (enemy.IsMoving)
                        prepos = prepos.Extend(enemy.GetWaypoints().Last().To3D(), 125);

                    if (ChampionInfoOne == null)
                    {
                        ChampionInfoList.Add(new ChampionInfo() { NetworkId = enemy.NetworkId, LastVisablePos = enemy.Position, LastVisableTime = Game.Time, PredictedPos = prepos });
                    }
                    else
                    {
                        ChampionInfoOne.NetworkId = enemy.NetworkId;
                        ChampionInfoOne.LastVisablePos = enemy.Position;
                        ChampionInfoOne.LastVisableTime = Game.Time;
                        ChampionInfoOne.PredictedPos = prepos;
                    }
                }

            }
        }
    }
}
