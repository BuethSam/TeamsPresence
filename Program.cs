﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TeamsPresence
{
    class Program
    {
        private static HomeAssistantService HomeAssistantService;
        private static TeamsLogService TeamsLogService;
        private static CameraDetectionService CameraDetectionService;
        private static TeamsPresenceConfig Config;

        private static NotifyIcon NotifyIcon;
        private static string ConfigDirectory;

        static void Main(string[] args)
        {
            SetupNotifyIcon();

            var configFile = "config.json";
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Teams Presence");
            var configFilePath = Path.Combine(configPath, configFile);

            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);

            ConfigDirectory = configPath;

            if (File.Exists(configFilePath))
            {
                Console.WriteLine("Config file found!");

                try
                {
                    Config = JsonConvert.DeserializeObject<TeamsPresenceConfig>(File.ReadAllText(configFilePath));
                    if (Config.FriendlyEntityNames == null)
                    {
                        Config.FriendlyEntityNames = new Dictionary<TeamsEntity, string>()
                        {
                            { TeamsEntity.StatusEntity, "Teams Status" },
                            { TeamsEntity.ActivityEntity, "Teams Activity" },
                            { TeamsEntity.CameraStatusEntity, "Teams Camera Status" },
                            { TeamsEntity.CameraAppEntity, "Teams Camera App" }
                        };

                        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(Config, new JsonSerializerSettings()
                        {
                            Formatting = Formatting.Indented
                        }));

                        NotifyIcon.BalloonTipText = "Fixed config. Fill out and restart this application.";

                        NotifyIcon.ShowBalloonTip(2000);

                        OpenConfigDirectory();
                    }
                }
                catch
                {
                    NotifyIcon.BalloonTipText = "Config file error. Please fix or recreate.";

                    NotifyIcon.ShowBalloonTip(2000);

                    OpenConfigDirectory();

                    return;
                }
            }
            else
            {
                Console.WriteLine("Config file doesn't exist. Creating...");

                Config = new TeamsPresenceConfig()
                {
                    HomeAssistantUrl = "https://yourha.duckdns.org",
                    HomeAssistantToken = "eyJ0eXAiOiJKV1...",
                    AppDataRoamingPath = "",
                    StatusEntity = "sensor.teams_presence_status",
                    ActivityEntity = "sensor.teams_presence_activity",
                    CameraAppEntity = "sensor.teams_presence_camera_app",
                    CameraStatusEntity = "sensor.teams_presence_camera_status",
                    CameraStatusPollingRate = 1000,
                    FriendlyEntityNames = new Dictionary<TeamsEntity, string>()
                    {
                        { TeamsEntity.StatusEntity, "Teams Status" },
                        { TeamsEntity.ActivityEntity, "Teams Activity" },
                        { TeamsEntity.CameraStatusEntity, "Teams Camera Status" },
                        { TeamsEntity.CameraAppEntity, "Teams Camera App" }
                    },
                    FriendlyStatusNames = new Dictionary<TeamsStatus, string>()
                    {
                        { TeamsStatus.Available, "Available" },
                        { TeamsStatus.Busy, "Busy" },
                        { TeamsStatus.OnThePhone, "On the phone" },
                        { TeamsStatus.Away, "Away" },
                        { TeamsStatus.BeRightBack, "Be right back" },
                        { TeamsStatus.DoNotDisturb, "Do not disturb" },
                        { TeamsStatus.Presenting, "Presenting" },
                        { TeamsStatus.Focusing, "Focusing" },
                        { TeamsStatus.InAMeeting, "In a meeting" },
                        { TeamsStatus.Offline, "Offline" },
                        { TeamsStatus.Unknown, "Unknown" }
                    },
                    FriendlyActivityNames = new Dictionary<TeamsActivity, string>()
                    {
                        { TeamsActivity.InACall, "In a call" },
                        { TeamsActivity.NotInACall, "Not in a call" },
                        { TeamsActivity.Unknown, "Unknown" }
                    },
                    FriendlyCameraStatusNames = new Dictionary<CameraStatus, string>()
                    {
                        { CameraStatus.Inactive, "Inactive" },
                        { CameraStatus.Active, "Active" }
                    },
                    ActivityIcons = new Dictionary<TeamsActivity, string>()
                    {
                        { TeamsActivity.InACall, "mdi:phone-in-talk-outline" },
                        { TeamsActivity.NotInACall, "mdi:phone-off" },
                        { TeamsActivity.Unknown, "mdi:phone-cancel" }
                    },
                    CameraStatusIcons = new Dictionary<CameraStatus, string>()
                    {
                        { CameraStatus.Inactive, "mdi:webcam-off" },
                        { CameraStatus.Active, "mdi:webcam" }
                    },
                    CameraAppIcon = "mdi:application"
                };

                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(Config, new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented
                }));

                NotifyIcon.BalloonTipText = "Generated blank config. Fill out and restart this application.";

                NotifyIcon.ShowBalloonTip(2000);

                OpenConfigDirectory();

                return;
            }

            TeamsLogService = new TeamsLogService();
            CameraDetectionService = new CameraDetectionService(Config.CameraStatusPollingRate);
            HomeAssistantService = new HomeAssistantService(Config.HomeAssistantUrl, Config.HomeAssistantToken);

            TeamsLogService.StatusChanged += Service_StatusChanged;
            TeamsLogService.ActivityChanged += Service_ActivityChanged;

            CameraDetectionService.StatusChanged += Camera_StatusChanged;

            Thread presenceDetectionThread = new Thread(
                delegate ()
                {
                    TeamsLogService.Start();
                });

            Thread cameraDetectionThread = new Thread(
                delegate ()
                {
                    CameraDetectionService.Start();
                });

            NotifyIcon.BalloonTipText = "Service started. Waiting for Teams updates...";

            NotifyIcon.ShowBalloonTip(2000);

            presenceDetectionThread.Start();
            cameraDetectionThread.Start();

            Application.Run();
        }

        private static void SetupNotifyIcon()
        {
            NotifyIcon = new NotifyIcon()
            {
                Icon = Resources.Icon,
                Visible = true,
                Text = "Teams Presence",
                BalloonTipTitle = "Teams Presence",
                BalloonTipIcon = ToolTipIcon.Info,
                ContextMenu = new ContextMenu()
            };

            var exitMenuItem = new MenuItem()
            {
                Text = "Quit",
                Index = 0,
            };

            exitMenuItem.Click += Program.Quit;

            var openConfigFolderMenuItem = new MenuItem()
            {
                Text = "Open Config Folder",
                Index = 1
            };

            openConfigFolderMenuItem.Click += OpenConfigDirectory;

            NotifyIcon.ContextMenu.MenuItems.AddRange(new MenuItem[] {
                exitMenuItem,
                openConfigFolderMenuItem
            });
        }

        private static void OpenConfigDirectory()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = ConfigDirectory,
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }

        private static void Service_StatusChanged(object sender, TeamsStatus status)
        {
            
            HomeAssistantService.UpdateEntity(Config.StatusEntity, Config.FriendlyStatusNames[status], Config.FriendlyEntityNames[TeamsEntity.StatusEntity], "mdi:microsoft-teams", Config.FriendlyStatusNames.Values.ToArray());

            Console.WriteLine($"Updated status to {Config.FriendlyEntityNames[TeamsEntity.StatusEntity]} ({status})");
        }

        private static void Service_ActivityChanged(object sender, TeamsActivity activity)
        {
            HomeAssistantService.UpdateEntity(Config.ActivityEntity, Config.FriendlyActivityNames[activity], Config.FriendlyEntityNames[TeamsEntity.ActivityEntity], Config.ActivityIcons[activity], Config.FriendlyActivityNames.Values.ToArray());

            Console.WriteLine($"Updated activity to {Config.FriendlyEntityNames[TeamsEntity.ActivityEntity]} ({activity})");
        }

        private static void Camera_StatusChanged(object sender, CameraStatusChangedEventArgs args)
        {
            HomeAssistantService.UpdateEntity(Config.CameraStatusEntity, Config.FriendlyCameraStatusNames[args.Status], Config.FriendlyEntityNames[TeamsEntity.CameraStatusEntity], Config.CameraStatusIcons[args.Status], Config.FriendlyCameraStatusNames.Values.ToArray());

            Console.WriteLine($"Updated camera status to {args.Status}");

            HomeAssistantService.UpdateEntity(Config.CameraAppEntity, args.AppName, Config.FriendlyEntityNames[TeamsEntity.CameraAppEntity], Config.CameraAppIcon);

            Console.WriteLine($"Updated camera app to {args.AppName}");
        }

        private static void OpenConfigDirectory(object sender, EventArgs e)
        {
            OpenConfigDirectory();
        }

        private static void Quit(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
