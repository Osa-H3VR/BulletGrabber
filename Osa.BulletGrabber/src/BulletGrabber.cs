using System;
using Deli.Immediate;
using Deli.Setup;
using Deli.VFS;
using Deli.VFS.Disk;
using UnityEngine.SceneManagement;

namespace Osa.BulletGrabber
{
    // DeliBehaviours are just MonoBehaviours that get added to a global game object when the game first starts.
    public class BulletGrabber : DeliBehaviour
    {
        private Hooks _hooks;
        
        // All Deli properties can be accessed here, but don't use Unity's API until Awake.
        public BulletGrabber()
        {
            // There is 1 log message in each of the methods here.
            // Run this mod to see the order of execution logged to console.
            Logger.LogInfo("BulletGrabber loading...");

            // Hook to the setup stage (the first stage we can access)
            // Do not forget! Awake is still ran before ANY event.
            Stages.Setup += OnSetup;

            // Check if the mod is on disk (as opposed to a zip file)
            if (Resources is IDiskHandle resourcesOnDisk)
            {
                Logger.LogInfo($"The mod is on disk at: '{resourcesOnDisk.PathOnDisk}'");
                
                // Every time the scene changes, reload our resources to see if they changed.
                SceneManager.activeSceneChanged += (x, p) => resourcesOnDisk.Refresh();
            }
        }

        // Now you can use Unity's API
        private void Awake()
        {
            LoadCfg();
        }
        
        // The config system is identical to BepInEx, but here's a demo if you are unfamiliar.
        // They are good with simple types (primitives and small structs), but for larger sets of data you may want to
        // use an asset loader of some sort.
        private void LoadCfg()
        {
            var hand = Config.Bind("BulletGrabber", "HandMode", "", "Defines which hands can grab bullets. Valid options are: 'both', 'left' or 'right'.");
            var delay = Config.Bind("BulletGrabber", "Delay", 1000, "Defines delay (in milliseconds) between each bullet grab. To be safe, pick something higher than 200.");
            var range = Config.Bind("BulletGrabber", "Range", Hooks.MaxLegitRange, "Defines range (in meters) of grab. Setting it higher than 3.2 will disable TnH scoring.");
            var bulet = Config.Bind("BulletGrabber", "BulletGrabMode", "closest", "Defines how compatible bullets finder should work. Valid options are: 'onlyTheSame', 'firstTheSame' or 'closest'.");

            Logger.LogInfo($"Loaded in: '{hand.Value}' hand(s) mode");
            Logger.LogInfo($"Loaded in: '{delay.Value}' ms of delay");
            Logger.LogInfo($"Loaded in: '{range.Value}' range");
            Logger.LogInfo($"Loaded in: '{bulet.Value}' bullet detection mode");
            
            _hooks = new Hooks(delay.Value,range.Value,hand.Value.ToLowerInvariant(), bulet.Value.ToLowerInvariant(), Logger);
        }
        
        // And now you can access much more of Deli
        private void OnSetup(SetupStage stage)
        {

        }

        private void OnDestroy()
        {
            _hooks?.Dispose();
        }

        private void Start()
        {
            Logger.LogInfo("BulletGrabber loaded.");
        }
    }
}