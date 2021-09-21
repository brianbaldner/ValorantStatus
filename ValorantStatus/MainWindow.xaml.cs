using System;
using System.Windows;
using ValAPINet;
using DiscordRPC;
using System.Diagnostics;
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
        public string RiotPath = "Riot Client\\RiotClientServices.exe";
        //Auth doesn't need to be updated
        public Auth auth;
        public string mapName;
        public string gameMode;

        //Does stuff that the async function didn't like
        public MainWindow()
        {
            InitializeComponent();
            icon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().ManifestModule.Name);
            //Gets Riot Client Location
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Riot Games\\Metadata\\valorant.live\\valorant.live.product_settings.yaml";
            string data = File.ReadAllText(filepath);
            //The last thing I would ever do is install a package
            string path = data.Split("product_install_root: \"")[1].Split("\"")[0];
            Process p = new Process();
            p.StartInfo.FileName = path + RiotPath;
            p.StartInfo.Arguments = "--launch-product=valorant --launch-patchline=live";
            p.Start();
        }

        //The meat of the script
        private async void MainScript(object sender, RoutedEventArgs e)
        {
            Hide();

            //ValAPI.Net for documentation
            auth = Websocket.GetAuthLocal();

            //Discord RPC Stuff

            //rpcclient = new DiscordRpcClient("Application ID");
            rpcclient = new DiscordRpcClient(ConfigurationManager.AppSettings.Get("DiscordKey"));
            rpcclient.SkipIdenticalPresence = true;
            rpcclient.RegisterUriScheme();
            rpcclient.OnJoin += Rpcclient_OnJoin;
            rpcclient.OnJoinRequested += Rpcclient_OnJoinRequested;
            rpcclient.Initialize();

            //So I can do title case
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

            //In task so I can update icon's menu
            await Task.Run(async () =>
            {
                Process[] started = new Process[0];
                //Waits until game has started, checks every 3 sec
                while (started.Length == 0)
                {
                    started = Process.GetProcessesByName("VALORANT-Win64-Shipping");
                    await Task.Delay(3000);
                }

                //Until break
                while (true)
                {
                    //ValAPI.Net
                    UserPresence.Presence presence = UserPresence.GetPresence(auth.subject);

                    //Checks for Discord events
                    rpcclient.Invoke();

                    //If presence hasn't started or in menus
                    if (presence == null || presence.privinfo.sessionLoopState == "MENUS")
                    {
                        //If game closed, stop the program
                        Process[] pname = Process.GetProcessesByName("VALORANT-Win64-Shipping");
                        if (pname.Length == 0)
                        {
                            break;
                        }

                        //If party is open
                        if (presence != null && presence.privinfo.partyAccessibility == "OPEN")
                        {
                            //Get match map and real mode from presence
                            mapName = presence.privinfo.matchMap;
                            gameMode = presence.privinfo.queueId;
                            //If looking for match
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
                                //If not waiting in match
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
                        else if (presence != null)
                        {
                            //If matchmaking and party is closed
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
                                        Privacy = Party.PrivacySetting.Private,
                                        Size = presence.privinfo.partySize
                                    },
                                    Secrets = null
                                });
                            }
                            else
                            {
                                //If waiting and party is private
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
                        //While in menus, update every 5 sec
                        await Task.Delay(5000);
                    }
                    else
                    {
                        if (mapName == null) mapName = "/Game/Maps/Poveglia/Range";
                        if (presence.privinfo.provisioningFlow == "ShootingRange") gameMode = "Shooting Range";

                        //One size fits all in game presence
                        rpcclient.SetPresence(new RichPresence()
                        {
                            Details = "Playing " + myTI.ToTitleCase(gameMode) + " on " + GetMapName(mapName),
                            State = presence.privinfo.partyOwnerMatchScoreAllyTeam + "-" + presence.privinfo.partyOwnerMatchScoreEnemyTeam,
                            Assets = new Assets()
                            {
                                LargeImageKey = GetMapName(mapName).ToLower().Replace(" ", "_"),
                                LargeImageText = GetMapName(mapName)
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
                        //When in match, update every 10 sec
                        await Task.Delay(10000);
                    }
                }
            });
            Quit();
        }

        //If someone presses "Ask to Join" on profile
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
        //If someone accepts Discord invite or you approve them
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

        //Should prob change to dictionary
        public string GetMapName(string mapid)
        {
            string displayName;
            switch (mapid)
            {
                case "/Game/Maps/Ascent/Ascent":
                    displayName = "Ascent";
                    break;
                case "/Game/Maps/Bonsai/Bonsai":
                    displayName = "Split";
                    break;
                case "/Game/Maps/Duality/Duality":
                    displayName = "Bind";
                    break;
                case "/Game/Maps/Port/Port":
                    displayName = "Icebox";
                    break;
                case "/Game/Maps/Triad/Triad":
                    displayName = "Haven";
                    break;
                case "/Game/Maps/Foxtrot/Foxtrot":
                    displayName = "Breeze";
                    break;
                case "/Game/Maps/Canyon/Canyon":
                    displayName = "Fracture";
                    break;
                case "/Game/Maps/Poveglia/Range":
                    displayName = "The Range";
                    break;
                default:
                    displayName = "Unknown Map";
                    break;
            }
            return displayName;
        }
        //Same as above
        public static string GetModeName(string mode)
        {
            string displayName;
            switch(mode)
            {
                case "/Game/GameModes/Bomb/BombGameMode.BombGameMode_C":
                    displayName = "Standard";
                    break;
                case "/Game/GameModes/Deathmatch/DeathmatchGameMode.DeathmatchGameMode_C":
                    displayName = "Deathmatch";
                    break;
                case "/Game/GameModes/GunGame/GunGameTeamsGameMode.GunGameTeamsGameMode_C":
                    displayName = "Escalation";
                    break;
                case "/Game/GameModes/OneForAll/OneForAll_GameMode.OneForAll_GameMode_C":
                    displayName = "Replication";
                    break;
                case "/Game/GameModes/QuickBomb/QuickBombGameMode.QuickBombGameMode_C":
                    displayName = "Spike Rush";
                    break;
                case "/Game/GameModes/ShootingRange/ShootingRangeGameMode.ShootingRangeGameMode_C":
                    displayName = "Shooting Range";
                    break;
                default:
                    displayName = "Unknown Mode";
                    break;
            }
            return displayName;
            
        }
        //Stops the rpc, removes the icon, and shuts down
        public void Quit()
        {
            rpcclient.Deinitialize();
            icon.Visibility = Visibility.Collapsed;
            Environment.Exit(0);
        }
        //If user presses quit
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Quit();
        }

        //AES encryption because Discord said so
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

        
    }
    public class yamlstuff
    {
        public string product_install_root;
    }
}
