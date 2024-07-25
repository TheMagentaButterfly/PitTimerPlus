using System;
using Rage;
using LSPD_First_Response.Mod.API;
using IPT.Common.API;
using IPT.Common.Fibers;

[assembly: Rage.Attributes.Plugin("PitTimerPlus", Description = "A plugin to handle pit timer during pursuits", Author = "YourName")]

namespace PitTimerPlus
{
    public class Main : Plugin
    {
        private static Config config;
        private static bool isPursuitActive = false;
        private static bool isPitTimerExpired = false;
        private static DateTime pitStartTime;
        private static LHandle activePursuit;

        private PursuitMonitorFiber pursuitMonitorFiber;

        public override void Initialize()
        {
            try
            {
                Game.LogTrivial("PitTimerPlus initialized.");

                // Load configuration at startup
                config = new Config();
                config.Load();

                LSPD_First_Response.Mod.API.Functions.OnOnDutyStateChanged += OnDutyStateChanged;

                // Notify that the plugin has been loaded successfully
                GameFiber.StartNew(delegate
                {
                    GameFiber.Sleep(5000); // Wait for 5 seconds to ensure LSPDFR is fully loaded
                    Notifications.OfficialNotification("PitTimerPlus", "By Mag", "Has loaded successfully.");
                    Game.LogTrivial("PitTimerPlus plugin loaded successfully.");
                });
            }
            catch (Exception e)
            {
                Game.LogTrivial($"Exception in Initialize: {e.Message}");
            }
        }

        public override void Finally()
        {
            Game.LogTrivial("PitTimerPlus cleaned up.");
        }

        private void OnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                Game.LogTrivial("Player went on duty.");
                if (pursuitMonitorFiber == null)
                {
                    pursuitMonitorFiber = new PursuitMonitorFiber();
                    pursuitMonitorFiber.Start();
                }
                Game.LogTrivial("MonitorPursuit fiber started.");
            }
            else
            {
                Game.LogTrivial("Player went off duty.");
                isPursuitActive = false;
                isPitTimerExpired = false; // Reset this flag when going off duty
                pursuitMonitorFiber?.Stop();
                pursuitMonitorFiber = null;
            }
        }

        private class PursuitMonitorFiber : GenericFiber
        {
            public PursuitMonitorFiber()
                : base("PursuitMonitorFiber", 1000)
            {
            }

            protected override void DoSomething()
            {
                if (!isPursuitActive && !isPitTimerExpired)
                {
                    activePursuit = LSPD_First_Response.Mod.API.Functions.GetActivePursuit();
                    if (activePursuit != null && LSPD_First_Response.Mod.API.Functions.IsPursuitStillRunning(activePursuit))
                    {
                        var suspects = LSPD_First_Response.Mod.API.Functions.GetPursuitPeds(activePursuit);
                        if (suspects != null && suspects.Length > 0 && suspects[0].IsInAnyVehicle(false))
                        {
                            Game.LogTrivial("Pursuit started with suspect in vehicle.");
                            isPursuitActive = true;
                            pitStartTime = DateTime.Now;

                            // Notify the start of the Pit Timer and its duration
                            string formattedDuration = FormatTime(config.PitDurationSeconds.Value);
                            Notifications.OfficialNotification("PitTimerPlus", "~y~Pursuit Started", $"{config.PitStartMessage.Value} {formattedDuration}.");

                            GameFiber.StartNew(DisplayPitTimer);
                        }
                        else
                        {
                            Game.LogTrivial("Pursuit started with suspect on foot. Pit timer disregarded.");
                        }
                    }
                }
                else
                {
                    if (LSPD_First_Response.Mod.API.Functions.IsPursuitStillRunning(activePursuit))
                    {
                        if ((DateTime.Now - pitStartTime).TotalSeconds >= config.PitDurationSeconds.Value)
                        {
                            Notifications.OfficialNotification("PitTimerPlus", "", config.PitExpireMessage.Value);
                            Game.LogTrivial("Pit Timer has expired.");
                            GameFiber.Sleep(10000); // Display the message for 10 seconds after expiry
                            isPitTimerExpired = true;
                            isPursuitActive = false;
                        }
                    }
                    else
                    {
                        Game.LogTrivial("Pursuit ended.");
                        isPursuitActive = false;
                        isPitTimerExpired = false; // Reset the flag when the pursuit ends
                    }
                }
            }

            private void DisplayPitTimer()
            {
                bool halfTimeNotificationShown = false;
                bool tenSecondsNotificationShown = false;

                while (isPursuitActive)
                {
                    var suspects = LSPD_First_Response.Mod.API.Functions.GetPursuitPeds(activePursuit);
                    if (suspects == null || suspects.Length == 0 || !suspects[0].IsInAnyVehicle(false))
                    {
                        Game.LogTrivial("Suspect exited the vehicle. Pit timer disregarded.");
                        Notifications.OfficialNotification("PitTimerPlus", "", $"{config.SuspectExitVehicleMessage.Value}");
                        isPursuitActive = false;
                        isPitTimerExpired = false; // Reset the flag
                        return;
                    }

                    int remainingSeconds = config.PitDurationSeconds.Value - (int)(DateTime.Now - pitStartTime).TotalSeconds;
                    if (remainingSeconds > 0)
                    {
                        string formattedTime = FormatTime(remainingSeconds);
                        if (!halfTimeNotificationShown && remainingSeconds == config.PitDurationSeconds.Value / 2)
                        {
                            Notifications.OfficialNotification("PitTimerPlus", "", $"{config.PitRemainingMessage.Value} {formattedTime}");
                            halfTimeNotificationShown = true;
                        }
                        else if (!tenSecondsNotificationShown && remainingSeconds == 10)
                        {
                            Notifications.OfficialNotification("PitTimerPlus", "", $"{config.PitRemainingMessage.Value} {formattedTime}");
                            tenSecondsNotificationShown = true;
                        }
                    }
                    GameFiber.Sleep(1000);
                }
            }
        }

        private static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            if (minutes > 0)
            {
                return $"{minutes} minutes {seconds} seconds";
            }
            else
            {
                return $"{seconds}seconds";
            }
        }
    }
}
