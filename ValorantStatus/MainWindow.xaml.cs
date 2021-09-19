using System;
using System.Windows;
using ValAPINet;
using DiscordRPC;
using System.Diagnostics;
using Hardcodet.Wpf.TaskbarNotification;
using System.Reflection;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using RestSharp;
using System.Globalization;
using System.Configuration;

namespace ValorantRPC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public DiscordRpcClient rpcclient;
        public Auth auth;
        public MainWindow()
        {
            InitializeComponent();
            icon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().ManifestModule.Name);
            Process p = new Process();
            p.StartInfo.FileName = "C:\\Riot Games\\Riot Client\\RiotClientServices.exe";
            p.StartInfo.Arguments = "--launch-product=valorant --launch-patchline=live";
            p.Start();
        }

        private void Rpcclient_OnJoin(object sender, DiscordRPC.Message.JoinMessage args)
        {
            string partyid = Secure.Decrypt(args.Secret);
            IRestClient postClient = new RestClient(new Uri($"https://glz-" + auth.region + "-1." + auth.region + ".a.pvp.net/parties/v1/players/" + auth.subject + "/joinparty/" + partyid));
            IRestRequest postRequest = new RestRequest(Method.POST);
            postRequest.AddHeader("Authorization", $"Bearer {auth.AccessToken}");
            postRequest.AddHeader("X-Riot-Entitlements-JWT", auth.EntitlementToken);
            postRequest.AddHeader("X-Riot-ClientPlatform", "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9");
            postRequest.AddHeader("X-Riot-ClientVersion", auth.version);

            IRestResponse postResp = postClient.Post(postRequest);
        }

        public string GetMapName(string mapid)
        {
            if (mapid == "/Game/Maps/Ascent/Ascent")
            {
                return "Ascent";
            }
            else if (mapid == "/Game/Maps/Bonsai/Bonsai")
            {
                return "Split";
            }
            else if (mapid == "/Game/Maps/Duality/Duality")
            {
                return "Bind";
            }
            else if (mapid == "/Game/Maps/Port/Port")
            {
                return "Icebox";
            }
            else if (mapid == "/Game/Maps/Triad/Triad")
            {
                return "Haven";
            }
            else if (mapid == "/Game/Maps/Foxtrot/Foxtrot")
            {
                return "Breeze";
            }
            else if (mapid == "/Game/Maps/Canyon/Canyon")
            {
                return "Fracture";
            }
            return null;
        }
        public static string GetModeName(string mode)
        {
            if (mode == "/Game/GameModes/Bomb/BombGameMode.BombGameMode_C")
            {
                return "Standard";
            }
            else if (mode == "/Game/GameModes/Deathmatch/DeathmatchGameMode.DeathmatchGameMode_C")
            {
                return "Deathmatch";
            }
            else if (mode == "/Game/GameModes/GunGame/GunGameTeamsGameMode.GunGameTeamsGameMode_C")
            {
                return "Escalation";
            }
            else if (mode == "/Game/GameModes/OneForAll/OneForAll_GameMode.OneForAll_GameMode_C")
            {
                return "Replication";
            }
            else if (mode == "/Game/GameModes/QuickBomb/QuickBombGameMode.QuickBombGameMode_C")
            {
                return "Spike Rush";
            }
            return null;
        }
        public void Quit()
        {
            rpcclient.Deinitialize();
            icon.Visibility = Visibility.Collapsed;
            Environment.Exit(0);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Quit();
        }
        public class Secure
        {
            public static string encrypt(string encryptString)
            {
                string EncryptionKey = ConfigurationManager.AppSettings.Get("EncryptionKey");
                byte[] clearBytes = Encoding.Unicode.GetBytes(encryptString);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(clearBytes, 0, clearBytes.Length);
                            cs.Close();
                        }
                        encryptString = Convert.ToBase64String(ms.ToArray());
                    }
                }
                return encryptString;
            }

            public static string Decrypt(string cipherText)
            {
                string EncryptionKey = ConfigurationManager.AppSettings.Get("EncryptionKey");
                cipherText = cipherText.Replace(" ", "+");
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
                return cipherText;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hide();
            auth = Websocket.GetAuthLocal();
            rpcclient = new DiscordRpcClient(ConfigurationManager.AppSettings.Get("DiscordKey"));
            rpcclient.SkipIdenticalPresence = true;
            rpcclient.RegisterUriScheme();
            rpcclient.OnJoin += Rpcclient_OnJoin;
            rpcclient.OnJoinRequested += Rpcclient_OnJoinRequested;
            rpcclient.Initialize();
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
            await Task.Run(async () =>
            {
                Process[] started = new Process[0];
                while (started.Length == 0)
                {
                    started = Process.GetProcessesByName("VALORANT-Win64-Shipping");
                    await Task.Delay(3000);
                }
                while (1 == 1)
                {
                    UserPresence.Presence presence = UserPresence.GetPresence(auth.subject);
                    rpcclient.Invoke();
                    if (presence == null || presence.privinfo.sessionLoopState == "MENUS")
                    {
                        Process[] pname = Process.GetProcessesByName("VALORANT-Win64-Shipping");
                        if (pname.Length == 0)
                        {
                            break;
                        }
                        if (presence != null && presence.privinfo.partyAccessibility == "OPEN")
                        {
                            if (presence.privinfo.partyState == "MATCHMAKING")
                            {
                                rpcclient.SetPresence(new RichPresence()
                                {
                                    Details = "Menus",
                                    State = $"Searching ({myTI.ToTitleCase(presence.privinfo.queueId)})",
                                    Assets = new Assets()
                                    {
                                        LargeImageKey = "logo"
                                    },
                                    Party = new Party
                                    {
                                        ID = presence.privinfo.partyId,
                                        Max = presence.privinfo.maxPartySize,
                                        Privacy = Party.PrivacySetting.Public,
                                        Size = presence.privinfo.partySize
                                    },
                                    Secrets = new Secrets()
                                    {
                                        JoinSecret = Secure.encrypt(presence.privinfo.partyId),
                                        SpectateSecret = "1dfdfgdfgsfdgdfgsdf"
                                    }
                                });
                            }
                            else
                            {
                                rpcclient.SetPresence(new RichPresence()
                                {
                                    Details = "Menus",
                                    State = $"Waiting ({myTI.ToTitleCase(presence.privinfo.queueId)})",
                                    Assets = new Assets()
                                    {
                                        LargeImageKey = "logo"
                                    },
                                    Party = new Party
                                    {
                                        ID = presence.privinfo.partyId,
                                        Max = presence.privinfo.maxPartySize,
                                        Privacy = Party.PrivacySetting.Private,
                                        Size = presence.privinfo.partySize
                                    },
                                    Secrets = new Secrets()
                                    {
                                        JoinSecret = Secure.encrypt(presence.privinfo.partyId),
                                        SpectateSecret = "1dfdfgdfgsfdgdfgsdf"
                                    }
                                });
                            }
                        }
                        else if(presence != null)
                        {
                            if (presence.privinfo.partyState == "MATCHMAKING")
                            {
                                rpcclient.SetPresence(new RichPresence()
                                {
                                    Details = "Menus",
                                    State = $"Searching ({myTI.ToTitleCase(presence.privinfo.queueId)}",
                                    Assets = new Assets()
                                    {
                                        LargeImageKey = "logo"
                                    },
                                    Party = new Party
                                    {
                                        ID = presence.privinfo.partyId,
                                        Max = presence.privinfo.maxPartySize,
                                        Privacy = Party.PrivacySetting.Private,
                                        Size = presence.privinfo.partySize
                                    },
                                    Secrets = null
                                });
                            }
                            else
                            {
                                rpcclient.SetPresence(new RichPresence()
                                {
                                    Details = "Menus",
                                    State = $"Waiting ({myTI.ToTitleCase(presence.privinfo.queueId)})",
                                    Assets = new Assets()
                                    {
                                        LargeImageKey = "logo"
                                    },
                                    Party = new Party
                                    {
                                        ID = presence.privinfo.partyId,
                                        Max = presence.privinfo.maxPartySize,
                                        Privacy = Party.PrivacySetting.Private,
                                        Size = presence.privinfo.partySize
                                    },
                                    Secrets = null
                                });
                            }
                        }
                        await Task.Delay(5000);
                    }
                    else
                    {
                        rpcclient.SetPresence(new RichPresence()
                        {
                            Details = "Playing " + myTI.ToTitleCase(presence.privinfo.queueId) + " on " + GetMapName(presence.privinfo.matchMap),
                            State = presence.privinfo.partyOwnerMatchScoreAllyTeam + "-" + presence.privinfo.partyOwnerMatchScoreEnemyTeam,
                            Assets = new Assets()
                            {
                                LargeImageKey = GetMapName(presence.privinfo.matchMap).ToLower().Replace(" ", "_"),
                                LargeImageText = GetMapName(presence.privinfo.matchMap)
                            },
                            Party = new Party
                            {
                                ID = presence.privinfo.partyId,
                                Max = presence.privinfo.maxPartySize,
                                Privacy = Party.PrivacySetting.Private,
                                Size = presence.privinfo.partySize
                            },
                            Timestamps = new Timestamps()
                            {
                                Start = null
                            },
                            Secrets = new Secrets()
                            {
                                JoinSecret = null,
                                SpectateSecret = null
                            }
                        });
                        await Task.Delay(10000);
                    }
                }
            });
            Quit();
        }

        private void Rpcclient_OnJoinRequested(object sender, DiscordRPC.Message.JoinRequestMessage args)
        {
            DiscordRpcClient client = (DiscordRpcClient)sender;
            MessageBoxResult result = MessageBox.Show($"{args.User.Username} would like to join your party.", "ValorantStatus", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    client.Respond(args, true);
                    break;
                case MessageBoxResult.No:
                    client.Respond(args, false);
                    break;
            }
        }
    }
}
