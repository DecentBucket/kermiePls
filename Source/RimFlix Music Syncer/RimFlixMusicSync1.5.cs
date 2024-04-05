using System.Collections.Generic;
using HarmonyLib;
using RimFlix;
using RimWorld;
using UnityEngine;
using Verse;
using LudeonTK;

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
        
        [HarmonyPatch(typeof(MusicManagerPlay), "PlaySong")]
        [HarmonyPostfix]
        public static void MusicManagerPlayPostfix()
        {
            currentSong = Traverse.Create(Find.MusicManagerPlay).Field<SongDef>("currentSong").Value;
            TryStartMusicTimer(currentSong);
        }
        
        [HarmonyPatch(typeof(MusicManagerPlay), "MusicUpdate")]
        [HarmonyPostfix]
        public static void MusicUpdatePostfix()
        {
            if (kermieMode != SongShowState.Inactive)
            {
                if (Time.time >= musicClock && kermieMode == SongShowState.Primed)
                {

                    foreach(ThingDef TV in DefDatabase<ShowDef>.GetNamed("KermiePls_Universal").televisionDefs)
                    {
                        foreach(Building screen in Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(TV))
                        {
                            screen.TryGetComp<CompScreen>().ChangeShow(DefDatabase<ShowDef>.GetNamed("KermiePls_Universal"));
                        }
                    }

                    kermieMode = SongShowState.Active;
                    //Log.Message("KermiePls activated!");
                }

                if(kermieMode == SongShowState.Active && currentSong.HasModExtension<MusicTimerModExtention>() && musicClock + currentSong.GetModExtension<MusicTimerModExtention>().kermieDuration <= Time.time)
                {
                   
                    //Log.Message("Removing kermiePls!");
                    foreach (ThingDef TV in DefDatabase<ShowDef>.GetNamed("KermiePls_Universal").televisionDefs)
                    {
                        foreach (Building screen in Find.AnyPlayerHomeMap.listerBuildings.AllBuildingsColonistOfDef(TV))
                        {
                            Log.Message("Removing kermiePls from: " + screen.ThingID);
                            CompScreen screenComp = screen.TryGetComp<CompScreen>();
                            Traverse compTraverse = Traverse.Create(screenComp);

                            while (DefDatabase<ShowDef>.GetNamed("KermiePls_Universal").Equals(compTraverse.Property<ShowDef>("Show").Value))
                            {
                                screenComp.ChangeShow(DefDatabase<ShowDef>.GetRandom());
                            }
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
                if (song.HasModExtension<MusicTimerModExtention>())
                {
                    //Log.Message("Detected Mod Extension.");
                    musicClock = Time.time + song.GetModExtension<MusicTimerModExtention>().kermieTime;
                    kermieMode = SongShowState.Primed;
                    //Log.Message("musicClock set to: " + musicClock);
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
                    Find.MusicManagerPlay.ForcePlaySong(asong, false);
                }));
            }
            return list;
        }

    }
}
