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
using UnityEngine.Assertions.Must;
using Verse;

namespace RimFlix_Music_Syncer
{
    /*
     * Harmony patch into StartNewSong in game's music manager
     * Set a tick timer, and change show at the designated time
     */

    public class MusicTimerModExtention : DefModExtension
    {
        public float kermieTime;
        public float kermieDuration;
    }
    
    public class SongsToSync : DefModExtension 
    {
        public List<string> songs;
    }
    
    

    [StaticConstructorOnStartup]
    [HarmonyPatch]
    public class RimFlixMusicSync
    {
        public static float musicClock = 0f;
        //Should replace bools with an enum
        public static bool kermieActivated = true;
        public static bool kermieDeactivated = true;

        public static SongDef currentSong;

        static RimFlixMusicSync()
        {
            new Harmony("DecentBucket.RimFlix.Patch").PatchAll();
            musicClock = 0;
            kermieActivated = true;
            currentSong = DefDatabase<SongDef>.GetNamed("EntrySong");
            //Log.Message("" + currentSong);
            //Log.Message("Started! " + musicClock);
        }
        
        [HarmonyPatch(typeof(MusicManagerPlay), "StartNewSong")]
        [HarmonyPostfix]
        public static void MusicManagerPlayPostfix()
        {
            currentSong = Traverse.Create(Find.MusicManagerPlay).Field<SongDef>("lastStartedSong").Value;
            TryStartMusicTimer(currentSong);
        }
        
        [HarmonyPatch(typeof(MusicManagerPlay), "MusicUpdate")]
        [HarmonyPostfix]
        public static void MusicUpdatePostfix()
        {
            if (currentSong != null && Find.MusicManagerPlay.IsPlaying)
            {
                IEnumerable<Building> furniture;
                //yes i know how this looks
                furniture = Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("TubeTelevision"));
                furniture = furniture.Concat(Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("FlatscreenTelevision")));
                furniture = furniture.Concat(Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("MegascreenTelevision")));

                if (Time.time >= musicClock && !kermieActivated)
                {
                    kermieActivated = true;
                    kermieDeactivated = false;
                    //Log.Message("KermiePls activated!");
                    foreach (Building screen in furniture)
                    {
                        screen.TryGetComp<CompScreen>().ChangeShow(DefDatabase<ShowDef>.GetNamed("KermiePls_Universal"));
                    }
                }

                if(!kermieDeactivated && currentSong.HasModExtension<MusicTimerModExtention>() && musicClock + currentSong.GetModExtension<MusicTimerModExtention>().kermieDuration <= Time.time)
                {
                    //Log.Message("Removing kermiePls!");
                    foreach(Building screen in furniture)
                    {
                        //Log.Message("Removing kermiePls from: " + screen.ThingID);
                        CompScreen screenComp = screen.TryGetComp<CompScreen>();
                        Traverse compTraverse = Traverse.Create(screenComp);

                        while (DefDatabase<ShowDef>.GetNamed("KermiePls_Universal").Equals(compTraverse.Property<ShowDef>("Show").Value))
                        { 
                            screenComp.ChangeShow(DefDatabase<ShowDef>.GetRandom());
                            //Traverse.Create(screen.TryGetComp<CompScreen>()).Field("showUpdateTime").SetValue(0d);
                        }
                    }
                    kermieDeactivated = true;
                    //Log.Message("KermiePls Removed!");
                }
            }
        }
        
        public static void TryStartMusicTimer(SongDef song)
        {
            if (song != null)
            {
                //if statement could be expanded on by having a modextention for ShowDefs that contains the names of songs
                if (song.clipPath.Contains("Ceta") || song.clipPath.Contains("Alignment"))
                {
                    //Log.Message(song.defName + " detected!");
                    if (song.HasModExtension<MusicTimerModExtention>())
                    {
                        //Log.Message("Detected Mod Extension.");
                        musicClock = Time.time + song.GetModExtension<MusicTimerModExtention>().kermieTime;
                        kermieActivated = false;
                        //Log.Message("musicClock set to: " + musicClock);
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
