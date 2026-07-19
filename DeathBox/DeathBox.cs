using DeathBox.Managers;
using Rocket.Core.Plugins;
using SDG.Unturned;

namespace DeathBox
{
    public class DeathBoxPlugin : RocketPlugin<DeathBoxConfig>
    {
        public static DeathBoxPlugin? Instance { get; private set; }
        public static bool DebugMode { get; set; }

        public DeathBoxManager? Manager { get; private set; }

        protected override void Load()
        {
            Instance = this;
            Manager = new DeathBoxManager(this);
            Manager.Load();

            if (Level.isLoaded)
            {
                Manager.OnLevelLoaded(1);
            }
            else
            {
                Level.onLevelLoaded += Manager.OnLevelLoaded;
            }
        }

        protected override void Unload()
        {
            if (Manager != null)
            {
                Level.onLevelLoaded -= Manager.OnLevelLoaded;
                Manager.Unload();
                Manager = null;
            }

            Instance = null;
        }
    }
}
