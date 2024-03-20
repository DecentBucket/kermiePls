using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimFlix_Music_Syncer
{
    /*
     * Harmony patch into StartNewSong in game's music manager
     * Set a tick timer, and change show at the designated time
     */
    [StaticConstructorOnStartup]
    public class RimFlixMusicSync
    {
        public Traverse musicTraverse = new Traverse(typeof(MusicManagerPlay));
        public RimFlixMusicSync()
        {
            new Harmony("DecentBucket.RimFlix.Patch").PatchAll();
        }

        //
        [HarmonyPatch(typeof(MusicManagerPlay), "StartNewSong")]
        [HarmonyPostfix]
        public static void MusicManagerPlayPostfix()
        {
            if ()
            {

            }
        }


    }
}
