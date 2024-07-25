using IPT.Common.User;
using IPT.Common.User.Settings;
using Rage;

namespace PitTimerPlus
{
    internal class Config : Configuration
    {
        public SettingInt PitDurationSeconds = new SettingInt("PitTimerSettings", "PitDurationSeconds", "The duration of the pit timer in seconds.", 360, 60, 36000, 1);
        public SettingString PitStartMessage = new SettingString("PitTimerSettings", "PitStartMessage", "Message displayed when the pit timer starts.", "~g~Pit Timer started. ~b~Duration:");
        public SettingString PitRemainingMessage = new SettingString("PitTimerSettings", "PitRemainingMessage", "Message displayed during the pursuit", "~b~Time remaining:");
        public SettingString PitExpireMessage = new SettingString("PitTimerSettings", "PitExpireMessage", "Message displayed when the pit timer expires.", "~r~Pit timer has expired.");
        public SettingString SuspectExitVehicleMessage = new SettingString("PitTimerSettings", "SuspectExitVehicleMessage", "Message displayed when the suspect exits the vehicle.", "Suspect exited the vehicle. Pit timer ~y~disregarded.");
        public override void Load()
        {
            LoadINI("Plugins/LSPDFR/PitTimerPlusSettings.ini");
        }
    }
}