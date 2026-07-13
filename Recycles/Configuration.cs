using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

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

        /// <summary>
        /// When true, dying in a vehicle skips the death box so vanilla loot drop applies.
        /// </summary>
        public bool SkipDeathBoxInVehicle { get; set; } = true;

        /// <summary>
        /// When true, dying in a safezone skips the death box so vanilla loot drop applies
        /// (e.g. suicide menu in a safezone).
        /// </summary>
        public bool SkipDeathBoxInSafezone { get; set; } = true;

        /// <summary>
        /// Item IDs that are not put into the death box. XML format:
        /// &lt;BlacklistedItemIds&gt;&lt;ushort&gt;363&lt;/ushort&gt;&lt;ushort&gt;364&lt;/ushort&gt;&lt;/BlacklistedItemIds&gt;
        /// </summary>
        [XmlArrayItem("ushort")]
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
            SkipDeathBoxInVehicle = true;
            SkipDeathBoxInSafezone = true;
            BlacklistedItemIds = new List<ushort> { 1441 };
        }
    }
}
