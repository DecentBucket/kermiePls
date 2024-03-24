using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using HarmonyLib;
using RimFlix;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimFlix_Music_Syncer
{
    /*
     * Harmony patch into StartNewSong in game's music manager
     * Set a tick timer, and change show at the designated time
     */
    [StaticConstructorOnStartup]
    [HarmonyPatch]
    public static class RimFlixMusicSync
    {
        
        static RimFlixMusicSync()
        {
            new Harmony("DecentBucket.RimFlix.Patch").PatchAll();
            Log.Message("Started!");
            float kermieClock = Time.time;
        }

        [HarmonyPatch(typeof(MusicManagerPlay), "StartNewSong")]
        [HarmonyPostfix]
        public static void MusicManagerPlayPostfix()
        {
            SongDef lastSong = Traverse.Create(Find.MusicManagerPlay).Field<SongDef>("lastStartedSong").Value;
            TryStartMusicTimer(lastSong);
        }

        public static void TryStartMusicTimer(SongDef song)
        {
            if (song != null)
            {
                //if statement could be expanded on by having a modextention for ShowDefs that contains the names of songs
                if (song.clipPath.Contains("Ceta") || song.clipPath.Contains("Alignment"))
                {
                    
                    //Log.Message(song.defName + " detected!");
                    IEnumerable<Building> furniture = Enumerable.Empty<Building>();
                    //yes i know how this looks
                    furniture = Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("TubeTelevision"));
                    furniture = furniture.Concat(Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("FlatscreenTelevision")));
                    furniture = furniture.Concat(Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("MegascreenTelevision")));
                    foreach (Building screen in furniture)
                    {
                        screen.TryGetComp<CompScreen>().ChangeShow(DefDatabase<ShowDef>.GetNamed("KermiePls_Universal"));
                    }
                }
            }
        }

        //debug action to start a song
        [DebugAction("Music", "Start Song", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static List<DebugActionNode> DebugActionStartSong()
        {
            List<DebugActionNode> list = new List<DebugActionNode>();
            foreach (SongDef song in DefDatabase<SongDef>.AllDefs)
            {
                SongDef asong = song;
                list.Add(new DebugActionNode(asong.defName, DebugActionType.Action, delegate
                {
                    Find.MusicManagerPlay.ForceStartSong(asong, false);
                }));
            }
            return list;
        }

    }
}
