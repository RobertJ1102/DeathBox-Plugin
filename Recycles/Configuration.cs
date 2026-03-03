using Rocket.API;
using System.Collections.Generic;

namespace DeathBox
{
    public class Configuration : IRocketPluginConfiguration
    {
        public byte InitialDeathBoxHSize { get; set; }
        public ushort DeathBoxID { get; set; }
        public bool CanDamageDeathBox { get; set; }
        public int DisappearCooldownAfterShutdown { get; set; }
        public int NormalDisappearCooldown { get; set; }
        public bool PunchUtil { get; set; }
        public bool PunchUtil_DropWhenItemsDoesntFit { get; set; }
        public List<ushort> BlacklistedItemIds { get; set; }

        public void LoadDefaults()
        {
            DeathBoxID = 366;
            InitialDeathBoxHSize = 10;
            CanDamageDeathBox = false;
            DisappearCooldownAfterShutdown = 40;
            NormalDisappearCooldown = 10;
            PunchUtil = true;
            PunchUtil_DropWhenItemsDoesntFit = true;
            BlacklistedItemIds = new List<ushort>();
        }
    }
}
