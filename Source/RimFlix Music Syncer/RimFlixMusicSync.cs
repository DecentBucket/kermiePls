using System.Collections.Generic;
using System.Linq;
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

    public class MusicTimerModExtention : DefModExtension
    {
        public float kermieTime;
        public float kermieDuration;
    }

    /*
    public class SongsToSync : DefModExtension 
    {
        public List<string> songs;
    }
    */
    
    [StaticConstructorOnStartup]
    [HarmonyPatch]
    public class RimFlixMusicSync
    {
        public static float musicClock = 0f;

        public enum SongShowState
        {
            Inactive,
            Primed,
            Active
        }
        public static SongShowState kermieMode;
        public static SongDef currentSong;

        static RimFlixMusicSync()
        {
            new Harmony("DecentBucket.RimFlix.Patch").PatchAll();
            musicClock = 0;
            kermieMode = SongShowState.Inactive;
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
                IEnumerable<Building> televisions;
                //yes i know how this looks
                televisions = Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("TubeTelevision"));
                televisions = televisions.Concat(Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("FlatscreenTelevision")));
                televisions = televisions.Concat(Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(DefDatabase<ThingDef>.GetNamed("MegascreenTelevision")));

                if (Time.time >= musicClock && kermieMode == SongShowState.Primed)
                {
                    kermieMode = SongShowState.Active;
                    //Log.Message("KermiePls activated!");
                    foreach (Building screen in televisions)
                    {
                        screen.TryGetComp<CompScreen>().ChangeShow(DefDatabase<ShowDef>.GetNamed("KermiePls_Universal"));
                    }
                }

                if(kermieMode == SongShowState.Active && currentSong.HasModExtension<MusicTimerModExtention>() && musicClock + currentSong.GetModExtension<MusicTimerModExtention>().kermieDuration <= Time.time)
                {
                    //Log.Message("Removing kermiePls!");
                    foreach(Building screen in televisions)
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
                    kermieMode = SongShowState.Inactive;
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
                        kermieMode = SongShowState.Primed;
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
