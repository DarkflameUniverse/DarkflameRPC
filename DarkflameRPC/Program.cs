using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace DiscordRPC.Example
{
    partial class Program
    {
        /// <summary>
        /// The level of logging to use.
        /// </summary>
        private static Logging.LogLevel logLevel = Logging.LogLevel.Error;

        /// <summary>
        /// The pipe to connect too.
        /// </summary>
        private static int discordPipe = -1;

        /// <summary>
        /// The current presence to send to discord.
        /// </summary>
        private static RichPresence presence = new RichPresence()
        {
            Details = "LEGO Universe",
            State = "Exploring the Universe!",
            Assets = new Assets()
            {
                LargeImageKey = "image_large",
                LargeImageText = "Put cool text here",
                SmallImageKey = "image_small"
            }
        };

        /// <summary>
        /// The discord client
        /// </summary>
        private static DiscordRpcClient client;

        /// <summary>
        /// Is the main loop currently running?
        /// </summary>
        private static bool isRunning = true;

        /// <summary>
        /// The string builder for the command
        /// </summary>
        private static StringBuilder word = new StringBuilder();

        /// <summary>
        /// Client log path.
        /// </summary>
        private static string clientLogLocation;

        /// <summary>
        /// Full list of worlds to check for when comparing LOAD ZONE messages.
        /// </summary>
        private static List<World> worldsList;

        /// <summary>
        /// Current world ID that the player is at.
        /// </summary>
        private static string currentWorld;

        /// <summary>
        /// Stores timestamps of each world transfer so we don't accidentally loop through them.
        /// </summary>
        private static List<string> worldTransferTimestamps;

        /// <summary>
        /// Tracks total time played in this session.
        /// </summary>
        private static Timestamps currentSessionTime;


        //Main Loop
        static void Main(string[] args)
        {
            clientLogLocation = (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\LEGO Software\\LEGO Universe\\Log Files\\");
            worldTransferTimestamps = new List<string>();
            //Reads the arguments for the pipe
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-pipe":
                        discordPipe = int.Parse(args[++i]);
                        break;

                    default: break;
                }
            }
            // Populate our worlds list first
            PopulateWorlds();
            // Setup the actual rich presence and wait for changes
            DarkflameClient();

            // Close the program once this is done.
            Environment.Exit(0);
        }

        static void PopulateWorlds()
        {
            // Maps not listed will default to a temp description and name.
            worldsList = new List<World>();
            worldsList.Add(new World("Frostburgh", "98:2", "Spreading holiday cheer!", "frostburgh_large")); // Set to 98:2 since that's what the 4 characters will be when we grab 'em, at least using the DLU spliced ID for the world
            worldsList.Add(new World("Venture Explorer", "1000", "Starting my journey!", "ve_large"));
            worldsList.Add(new World("Return to Venture Explorer", "1001", "Reclaiming the ship!", "rve_large"));
            worldsList.Add(new World("Avant Gardens", "1100", "Investigating the Maelstrom!", "ag_large"));
            worldsList.Add(new World("Avant Gardens Survival", "1101", "Surviving the horde!", "ags_large"));
            worldsList.Add(new World("Spider Queen Battle", "1102", "Purifying Block Yard!", "spiderqueen_large"));
            worldsList.Add(new World("Block Yard", "1150", "Visiting a property!", "blockyard_large"));
            worldsList.Add(new World("Avant Grove", "1151", "Visiting a property!", "avantgrove_large"));
            worldsList.Add(new World("Nimbus Station", "1200", "Hanging out in the Plaza!", "nimbusplaza"));
            worldsList.Add(new World("Pet Cove", "1201", "Taming pets!", "petcove_large"));
            worldsList.Add(new World("Vertigo Loop Racetrack", "1203", "Racing across Nimbus Station!", "vertigoloop_large"));
            worldsList.Add(new World("Battle of Nimbus Station", "1204", "Fighting through time!", "bons_large"));
            worldsList.Add(new World("Nimbus Rock", "1250", "Visiting a property!", "nimbusrock_large"));
            worldsList.Add(new World("Nimbus Isle", "1251", "Visiting a property!", "nimbusisle_large"));
            worldsList.Add(new World("Gnarled Forest", "1300", "Clearing the Maelstrom from Brig Rock!", "gf_large"));
            worldsList.Add(new World("Gnarled Forest Shooting Gallery", "1302", "Going for a high score!", "gfsg_large"));
            worldsList.Add(new World("Keelhaul Canyon Racetrack", "1303", "Racing across Gnarled Forest!", "keelhaul_large"));
            worldsList.Add(new World("Chantey Shanty", "1350", "Visiting a property!", "chanteyshanty_large"));
            worldsList.Add(new World("Forbidden Valley", "1400", "Learning the ways of the Ninja!", "fv_large"));
            worldsList.Add(new World("Forbidden Valley Dragon Battle", "1402", "Defeating dragons!", "dragonbattle_large"));
            worldsList.Add(new World("Dragonmaw Chasm Racetrack", "1403", "Racing across Forbidden Valley!", "dragonmaw_large"));
            worldsList.Add(new World("Raven Bluff", "1450", "Visiting a property!", "ravenbluff_large"));
            worldsList.Add(new World("Starbase 3001", "1600", "Visiting the WBL worlds!", "starbase_large"));
            worldsList.Add(new World("DeepFreeze", "1601", "Luptario's world!", "deepfreeze_large"));
            worldsList.Add(new World("Robot City", "1602", "Deerbite's world!", "robotcity_large"));
            worldsList.Add(new World("MoonBase", "1603", "A-Team's world!", "moonbase_large"));
            worldsList.Add(new World("Portabello", "1604", "Brickazon's world!", "jaedoria_large"));
            worldsList.Add(new World("LEGO Club", "1700", "Saying hi to Max!", "legoclub_large"));
            worldsList.Add(new World("Crux Prime", "1800", "Pushing back the Maelstrom!", "crux_large"));
            worldsList.Add(new World("Nexus Tower", "1900", "Relaxing by the Nexus!", "nexustower_large"));
            worldsList.Add(new World("Ninjago Monastery", "2000", "Learning Spinjitzu!", "ninjago_large"));
            worldsList.Add(new World("Battle Against Frakjaw", "2001", "Defeating the Skulkin threat!", "frakjaw_large"));
        }

        static void DarkflameClient()
        {
            // == Create the client
            client = new DiscordRpcClient("734924666086359110", pipe: discordPipe)
            {
                Logger = new Logging.ConsoleLogger(logLevel, true)
            };

            // == Subscribe to some events
            client.OnReady += (sender, msg) =>
            {
                //Create some events so we know things are happening
                Console.WriteLine("Connected to discord with user {0}", msg.User.Username);
            };

            client.OnPresenceUpdate += (sender, msg) =>
            {
                //The presence has updated
                Console.WriteLine("Presence has been updated! ");
            };

            // == Initialize
            client.Initialize();

            // Start our session timer
            currentSessionTime = Timestamps.Now;

            // == Set the presence
            client.SetPresence(new RichPresence()
            {
                Details = "Login Screen",
                State = "Preparing to explore the Universe!",
                Timestamps = currentSessionTime,
                Assets = new Assets()
                {
                    LargeImageKey = "login",
                    LargeImageText = "Login Screen",
                    SmallImageKey = "logo"
                },
                Buttons = new Button[]
                {
                    new Button() { Label = "Website", Url = "https://darkflameuniverse.org/" },
                    new Button() { Label = "Twitter", Url = "https://twitter.com/darkflameuniv" }
                }
            });


            //Enter our main loop
            MainLoop();

            // == At the very end we need to dispose of it
            client.Dispose();
        }

        public static FileInfo GetNewestFile(DirectoryInfo directory)
        {
            return directory.GetFiles()
                .Union(directory.GetDirectories().Select(d => GetNewestFile(d)))
                .OrderByDescending(f => (f == null ? DateTime.MinValue : f.LastWriteTime))
                .FirstOrDefault();
        }

        static void UpdateWorldPresence(string worldID, DiscordRpcClient client)
        {
            if (currentWorld == worldID)
                return;
            currentWorld = worldID;

            string worldName = "Testmap";
            string worldDescription = "Where am I??";
            string worldLargeImage = "nimbusplaza";
            string worldSmallImage = "happyflower_small"; // figured it'd be amusing to default the icon to the happy flower face

            foreach (World wrld in worldsList)
            {
                if (wrld.worldID == worldID)
                {
                    worldName = wrld.worldName;
                    worldDescription = wrld.worldDescription;
                    worldLargeImage = wrld.worldLargeIcon;
                    worldSmallImage = "logo";
                    break;
                }
            }

            // Now, check if it's a LUP world
            if (worldID == "1600" || worldID == "1601" || worldID == "1602" || worldID == "1603" || worldID == "1604")
                worldSmallImage = "lup_small";

            client.SetPresence(new RichPresence()
            {
                Details = worldName,
                State = worldDescription,
                Timestamps = currentSessionTime,
                Assets = new Assets()
                {
                    LargeImageKey = worldLargeImage,
                    LargeImageText = worldName,
                    SmallImageKey = worldSmallImage
                },
                Buttons = new Button[]
                {
                    new Button() { Label = "Website", Url = "https://darkflameuniverse.org/" },
                    new Button() { Label = "Twitter", Url = "https://twitter.com/darkflameuniv" }
                }
            });
        }

        static void MainLoop()
        {
            /*
			 * Enter a infinite loop, polling the Discord Client for events.
			 * In game termonology, this will be equivalent to our main game loop. 
			 * If you were making a GUI application without a infinite loop, you could implement
			 * this with timers.
			*/
            isRunning = true;
            while (client != null && isRunning)
            {
                //Check if the game is still open:
                isRunning = Process.GetProcessesByName("legouniverse").Length > 0;

                //We will invoke the client events. 
                // In a game situation, you would do this in the Update.
                // Not required if AutoEvents is enabled.
                //if (client != null && !client.AutoEvents)
                //	client.Invoke();

                // Look for a log file
                if (Directory.GetFiles(clientLogLocation) != null)
                {
                    // Sometimes a user can have more than one log; in that case, grab the latest file
                    FileInfo logPath = GetNewestFile(new DirectoryInfo(clientLogLocation));

                    // Open the log and start reading
                    FileStream logFileStream = new FileStream(clientLogLocation + logPath.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    StreamReader logFileReader = new StreamReader(logFileStream);
                    string line;
            
                    while ((line = logFileReader.ReadLine()) != null)
                    {
                        // If we've got a load zone message and it wasn't one we've seen before, update presence
                        if (line != null && line.Contains("MSG_LOAD_ZONE") && !(worldTransferTimestamps.Contains(line.Substring(0,7))))
                        {
                            // Update presence
                            worldTransferTimestamps.Add(line.Substring(0, 7));
                            UpdateWorldPresence(line.Substring(57, 4), client);
                        }
                    }
                }
                // Genuinely not sure how this case could happen if you have LU installed and have launched it, but hey, who knows.
                else
                {
                    Console.WriteLine("No log folder found!");
                    client.Dispose();
                }

                //Try to read any keys if available
                if (Console.KeyAvailable)
                    ProcessKey();

                //This can be what ever value you want, as long as it is faster than 30 seconds.
                //Console.Write("+");
                Thread.Sleep(250);

            }
        }

        static int cursorIndex = 0;
        static string previousCommand = "";
        static void ProcessKey()
        {
            //Read they key
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    //Write the new line
                    Console.WriteLine();
                    cursorIndex = 0;

                    //The enter key has been sent, so send the message
                    previousCommand = word.ToString();
                    ExecuteCommand(previousCommand);

                    word.Clear();
                    break;

                case ConsoleKey.Backspace:
                    word.Remove(cursorIndex - 1, 1);
                    Console.Write("\r                                         \r");
                    Console.Write(word);
                    cursorIndex--;
                    break;

                case ConsoleKey.Delete:
                    if (cursorIndex < word.Length)
                    {
                        word.Remove(cursorIndex, 1);
                        Console.Write("\r                                         \r");
                        Console.Write(word);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    cursorIndex--;
                    break;

                case ConsoleKey.RightArrow:
                    cursorIndex++;
                    break;

                case ConsoleKey.UpArrow:
                    word.Clear().Append(previousCommand);
                    Console.Write("\r                                         \r");
                    Console.Write(word);
                    break;

                default:
                    if (!Char.IsControl(key.KeyChar))
                    {
                        //Some other character key was sent
                        Console.Write(key.KeyChar);
                        word.Insert(cursorIndex, key.KeyChar);
                        Console.Write("\r                                         \r");
                        Console.Write(word);
                        cursorIndex++;
                    }
                    break;
            }

            if (cursorIndex < 0) cursorIndex = 0;
            if (cursorIndex >= Console.BufferWidth) cursorIndex = Console.BufferWidth - 1;
            Console.SetCursorPosition(cursorIndex, Console.CursorTop);
        }

        static void ExecuteCommand(string word)
        {
            //Trim the extra spacing
            word = word.Trim();

            //Prepare the command and its body
            string command = word;
            string body = "";

            //Split the command and the values.
            int whitespaceIndex = word.IndexOf(' ');
            if (whitespaceIndex >= 0)
            {
                command = word.Substring(0, whitespaceIndex);
                if (whitespaceIndex < word.Length)
                    body = word.Substring(whitespaceIndex + 1);
            }

            //Parse the command
            switch (command.ToLowerInvariant())
            {
                case "close":
                    client.Dispose();
                    break;

                #region State & Details
                case "state":
                    //presence.State = body;
                    presence.State = body;
                    client.SetPresence(presence);
                    break;

                case "details":
                    presence.Details = body;
                    client.SetPresence(presence);
                    break;
                #endregion

                #region Asset Examples
                case "large_key":
                    //If we do not have a asset object already, we must create it
                    if (!presence.HasAssets())
                        presence.Assets = new Assets();

                    //Set the key then send it away
                    presence.Assets.LargeImageKey = body;
                    client.SetPresence(presence);
                    break;

                case "large_text":
                    //If we do not have a asset object already, we must create it
                    if (!presence.HasAssets())
                        presence.Assets = new Assets();

                    //Set the key then send it away
                    presence.Assets.LargeImageText = body;
                    client.SetPresence(presence);
                    break;

                case "small_key":
                    //If we do not have a asset object already, we must create it
                    if (!presence.HasAssets())
                        presence.Assets = new Assets();

                    //Set the key then send it away
                    presence.Assets.SmallImageKey = body;
                    client.SetPresence(presence);
                    break;

                case "small_text":
                    //If we do not have a asset object already, we must create it
                    if (!presence.HasAssets())
                        presence.Assets = new Assets();

                    //Set the key then send it away
                    presence.Assets.SmallImageText = body;
                    client.SetPresence(presence);
                    break;
                #endregion

                case "help":
                    Console.WriteLine("Available Commands: state, details, large_key, large_text, small_key, small_text");
                    break;

                default:
                    Console.WriteLine("Unkown Command '{0}'. Try 'help' for a list of commands", command);
                    break;
            }

        }

    }
}
