using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Management;
using System.Collections.Generic;
using System.Net;

/*
 * When adding a new feature make sure to change:
 * 1. Writing it to the settings file
 * 2. Reading it from the settings file
 * 3. Reset button event
 */

namespace Rocket_League_Customizer
{
    public partial class RLCustomizer : Form
    {
 
        private static string exePath;
        Thread processWatcher = new Thread(new ThreadStart(CheckForProcess));

        private static bool isRunning = false;

        private WebServer ws;
        private static bool isClosing = false;

        public RLCustomizer()
        {
            InitializeComponent();

            // Since log now appends, clear log file on startup
            File.WriteAllText("log.txt", "");


            InitCustomBlog();
            CheckFirstTime();
            InitSavedSettings();
            InitMaps();
            WriteToLog("Initialized");

            // TU - Changed the method of grabbing the exe path to this...hopefully doesn't cause any issues.  Due to threading the other method was giving me problems.
            exePath = System.IO.Directory.GetCurrentDirectory() + "\\";
            WriteToLog(exePath);

            processWatcher.Start();

            // Start LAn redirect server
            if (Properties.Settings.Default.LanEnabled)
            {
                ws = new WebServer(SendResponse, "http://localhost:8080/Keys/GenerateKeys/", "http://localhost:8080/Services/", "http://localhost:8080/callproc105/", "http://localhost:8080/Population/UpdatePlayerCurrentGame/", "http://localhost:8080/auth/", "http://localhost:8080/Matchmaking/CheckReservation/");
                ws.Run();
            }

        }

        /* START LOAD MODS INJECTION */

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
          IntPtr hProcess,
          IntPtr lpThreadAttributes,
          uint dwStackSize,
          UIntPtr lpStartAddress, // raw Pointer into remote process
          IntPtr lpParameter,
          uint dwCreationFlags,
          out IntPtr lpThreadId
        );

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            Int32 bInheritHandle,
            Int32 dwProcessId
            );

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(
        IntPtr hObject
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(
            IntPtr hModule,
            string procName
            );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect
            );

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            string lpBuffer,
            UIntPtr nSize,
            out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(
            string lpModuleName
            );

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern Int32 WaitForSingleObject(
            IntPtr handle,
            Int32 milliseconds
            );

        public static Int32 GetProcessId(String proc)
        {
            try
            {
                Process[] ProcList;
                ProcList = Process.GetProcessesByName(proc);
                return ProcList[0].Id;
            } catch(IndexOutOfRangeException e)
            {
                MessageBox.Show("Rocket League not running");
                return -1;
            }
        }

        public static string SendResponse(HttpListenerRequest request)
        {

            if (request.Url.AbsolutePath.Contains("/Keys/GenerateKeys"))
                return "Version=1&Key=ymaFdh03/Hw4rvHjr1zhlZVyNWQipDQqC1nzptiXfgE=&IV=nZ2e0bJY1YVZAgORhFbsEw==&HMACKey=Xv17y2p+hdaGbQgtnWAPbC58xeNGbNSDHr3wvODVsjE=&SessionID=9fhBAd0kBYFMMWmbA8GrkQ==";
            else
            {
                return "";
            }
        }

        public static void InjectDLL(IntPtr hProcess, String strDLLName, bool showMessages)
        {
            IntPtr bytesout;

            // Length of string containing the DLL file name +1 byte padding
            Int32 LenWrite = strDLLName.Length + 1;
            // Allocate memory within the virtual address space of the target process
            IntPtr AllocMem = (IntPtr)VirtualAllocEx(hProcess, (IntPtr)null, (uint)LenWrite, 0x1000, 0x40); //allocation pour WriteProcessMemory

            // Write DLL file name to allocated memory in target process
            WriteProcessMemory(hProcess, AllocMem, strDLLName, (UIntPtr)LenWrite, out bytesout);
            // Function pointer "Injector"
            UIntPtr Injector = (UIntPtr)GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (Injector == null)
            {
                MessageBox.Show(" Injector Error! \n ");
                // return failed
                return;
            }

            // Create thread in target process, and store handle in hThread
            IntPtr hThread = (IntPtr)CreateRemoteThread(hProcess, (IntPtr)null, 0, Injector, AllocMem, 0, out bytesout);
            // Make sure thread handle is valid
            if (hThread == null)
            {
                //incorrect thread handle ... return failed
                MessageBox.Show(" hThread [ 1 ] Error! \n ");
                return;
            }
            // Time-out is 10 seconds...
            int Result = WaitForSingleObject(hThread, 10 * 1000);
            // Check whether thread timed out...
            if (Result == 0x00000080L || Result == 0x00000102L || Result == 0xFFFFFFFF)
            {
                /* Thread timed out... */
                MessageBox.Show(" hThread [ 2 ] Error! \n ");
                // Make sure thread handle is valid before closing... prevents crashes.
                if (hThread != null)
                {
                    //Close thread in target process
                    CloseHandle(hThread);
                }
                return;
            }
            // Sleep thread for 1 second
            Thread.Sleep(1000);
            // Clear up allocated space ( Allocmem )
            VirtualFreeEx(hProcess, AllocMem, (UIntPtr)0, 0x8000);
            // Make sure thread handle is valid before closing... prevents crashes.
            if (hThread != null)
            {
                //Close thread in target process
                CloseHandle(hThread);
            }
            // return succeeded
            if(showMessages)
                MessageBox.Show("Mods Loaded\nPress F1 in the main menu to activate menu mods.\nPress F2 in a game to activate the in game mods.\nGo to help for more instructions.");
            WriteToLog("Mods successfully loaded.");
            return;
        }

        /* END LOAD MODS INJECTION */


        //If its their first time running the program, tell them what to do.
        private void CheckFirstTime()
        {
            if (Properties.Settings.Default.FirstTime)
            {
                MessageBox.Show("Welcome! To get everything properly set up, please start Rocket League through steam and then press the \"Set RL Path\" button under Settings.", "Welcome");
                Properties.Settings.Default.FirstTime = false;
                Properties.Settings.Default.Save();
            }
        }

        //Save the path of Rocket Leauge
        // TU - Added boolean for displaying error message.  If called by button press, save path and display errors.  If called by polling threads, silently update path if possible
        private bool SavePath(bool showSuccessOutput)
        {
            string rlPath = GetProcessPath("RocketLeague");
            if (rlPath == string.Empty)
            {
                MessageBox.Show("Please have Rocket League running.", "Error");
                return false;
            }
            else
            {
                //Save in default settings
                try
                {
                    Properties.Settings.Default.RLPath = rlPath;
                    Properties.Settings.Default.Save();
                    if (showSuccessOutput)
                    {
                        MessageBox.Show("Rocket League path saved as \n" + Properties.Settings.Default.RLPath, "Success");
                    }
                    WriteToLog("Path Saved");
                    return true;
                } catch (Exception e)
                {
                    MessageBox.Show("Unable to save Rocket League path.", "Error");
                    WriteToLog("Path Not Saved");
                    return false;
                }
            }
            
        }

        //Custom blog initially disabled
        private void InitCustomBlog()
        {
            title_textBox.Enabled = false;
            body_textBox.Enabled = false;
            motd_textBox.Enabled = false;
            youtubeTitle_textBox.Enabled = false;
            youtubeURL_textBox.Enabled = false;
            //loaderTab.Enabled = false;
        }

        //Function get the rocket league path to exe
        public static string GetProcessPath(string name)
        {
            Process[] processes = Process.GetProcessesByName(name);

            if (processes.Length > 0)
            {
               try
                {
                    string rl = processes[0].MainModule.FileName;
                    rl = rl.Replace("\\", "\\\\");
                    Console.WriteLine(rl);
                    // Add check to make sure correct process
                    if (rl.Contains("rocketleague"))
                    {
                        WriteToLog("Path: " + rl);
                        return rl.Remove(rl.Length - 16);
                    } else
                    {
                        return String.Empty;
                    }
                } catch(Exception e)
                {
                    WriteToLog(e.ToString());
                    return String.Empty;
                }
                
            }
            else
            {
                return string.Empty;
            }
        }

        //Custom blog checkbox event
        private void customBlog_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            //Check if they want custom blog or not
            if (customBlog_checkBox.Checked)
            {
                title_textBox.Enabled = true;
                body_textBox.Enabled = true;
                motd_textBox.Enabled = true;
                youtubeTitle_textBox.Enabled = true;
                youtubeURL_textBox.Enabled = true;
                //mmr_checkBox.Enabled = true;
            }
            else
            {
                InitCustomBlog();
            }
        }


        //Save button click event
        private void saveBtn_Click(object sender, EventArgs e)
        {
            //If path isn't set save it
            if (Properties.Settings.Default.RLPath == String.Empty)
            {
                MessageBox.Show("Please start Rocket League and click the \"Set RL Path\"");
                return;
            }
            //Write custom data to file
            using (StreamWriter writer = new StreamWriter(Properties.Settings.Default.RLPath + "settings.txt"))
            {
                writer.WriteLine(jump_text.Text);
                writer.WriteLine(ball_text.Text);
                writer.WriteLine(car_text.Text);
                writer.WriteLine(goal_text.Text);
                writer.WriteLine((unlJumps_checkBox.Checked) ? "1" : "0");
                writer.WriteLine((zombieCheckBox.Checked) ? "1" : "0");
                writer.WriteLine((Hidden_checkBox.Checked) ? "1" : "0");
                writer.WriteLine((nameChange_CheckBox.Checked) ? "1" : "0");
                if (customBlog_checkBox.Checked)
                {
                    writer.WriteLine("1");
                    writer.WriteLine(title_textBox.Text);
                    writer.WriteLine(body_textBox.Text);
                    //Stops the getline function in the dll when reading multi line body
                    writer.WriteLine("xxx");
                    writer.WriteLine(motd_textBox.Text);
                    writer.WriteLine(youtubeTitle_textBox.Text);
                    writer.WriteLine(youtubeURL_textBox.Text);

                } else {
                    writer.WriteLine("0");
                }
                writer.WriteLine(spinRateText.Text);
                writer.WriteLine(speedText.Text);
                writer.WriteLine((spiderManCheckBox.Checked) ? "1" : "0");
                writer.WriteLine((DemoOnOppCheckBox.Checked) ? "1" : "0");
                writer.WriteLine((randomSizeBotsCheckBox.Checked) ? "1" : "0");
                writer.WriteLine(ballGravityScaleText.Text);
                writer.WriteLine(bounceScaleText.Text);
                MessageBox.Show("Settings Saved");
                
                WriteToLog("Settings Saved");
                writer.Close();
            }
            
        }

        //Load saved settings from settings file
        private void InitSavedSettings()
        {
            // Initialize settings saved to application
            autoLoadModsToolStripMenuItem.Checked = Properties.Settings.Default.AutoLoadMods;
            

            if (!File.Exists(Properties.Settings.Default.RLPath + "settings.txt") || Properties.Settings.Default.RLPath == string.Empty)
            {
                WriteToLog("Settings do not exist.");
                return;
            }
                

            string line;
            int count = 0;
            System.IO.StreamReader reader = new System.IO.StreamReader(Properties.Settings.Default.RLPath + "settings.txt");
            WriteToLog("Settings do exist.");
            while ((line = reader.ReadLine()) != null)
            {
                switch (count)
                {
                    case 0:
                        jump_text.Text = line;
                        break;
                    case 1:
                        ball_text.Text = line;
                        break;
                    case 2:
                        car_text.Text = line;
                        break;
                    case 3:
                        goal_text.Text = line;
                        break;
                    case 4:
                        unlJumps_checkBox.Checked = (line == "1") ? true : false;
                        break;
                    case 5:
                        zombieCheckBox.Checked = (line == "1") ? true : false;
                        break;
                    case 6:
                        Hidden_checkBox.Checked = (line == "1") ? true : false;
                        break;
                    case 7:
                        nameChange_CheckBox.Checked = (line == "1") ? true : false;
                        break;
                    case 8:
                        if (line == "1")
                        {
                            customBlog_checkBox.Checked = true;
                            line = reader.ReadLine();
                            title_textBox.Text = line;
                            line = reader.ReadLine();
                            body_textBox.Text = line + " ";
                            line = reader.ReadLine();
                            while (line != "xxx")
                            {
                                body_textBox.Text += line + " ";
                                line = reader.ReadLine();
                            }
                            
                            line = reader.ReadLine();
                            motd_textBox.Text = line;
                            line = reader.ReadLine();
                            youtubeTitle_textBox.Text = line;
                            line = reader.ReadLine();
                            youtubeURL_textBox.Text = line;
                            break;
                        }
                        break;
                    case 9:
                        spinRateText.Text = line;
                        break;
                    case 10:
                        speedText.Text = line;
                        break;
                    case 11:
                        spiderManCheckBox.Checked = (line == "1") ? true : false;
                        break;
                    case 12:
                        DemoOnOppCheckBox.Checked = (line == "1") ? true : false;
                        break;
                    case 13:
                        randomSizeBotsCheckBox.Checked = (line == "1") ? true : false;
                        break;
                    case 14:
                        ballGravityScaleText.Text = line;
                        break;
                    case 15:
                        bounceScaleText.Text = line;
                        break;
                }
                count++;
                

            }
            reader.Close();
        }

        //Start rocket league button event
        private void startRocketLeagueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //If the path isn't set tell them
            if (Properties.Settings.Default.RLPath == String.Empty)
            {
                MessageBox.Show("Path not set. Please launch rocket league and press the \"Set RL Path\" button.", "Error");
                return;
            }
            else if (!(GetProcessPath("RocketLeague") == string.Empty))
            {
                MessageBox.Show("Rocket League already running.");
            }
            else
            {
                System.Diagnostics.Process.Start(Properties.Settings.Default.RLPath + "RocketLeague.exe"); //To Add: ...Start(path,command line arguments)
            }
           
        }
        //Go to our reddit page
        private void redditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.reddit.com/r/RocketLeagueMods");
        }
        //Our website
        private void websiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://rocketleaguemods.com/");
        }

        //Help button
        private void howToUseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Step 1: Select/Edit the values you want and click Save.\n\nStep 2: Click the Load Mods button (you only need to do this once)."+
                "\n\nStep 3: Then hit the corresponding key.\n\n\tF1 requires you to be in the main menu.\n\tF2 requires you to be in game.\n\nMap Loader\n\nTo load a map go to the map loader tab a choose the map and game type."
                + "\n\nThen click \"Save Map Settings\" and then \"Load Mods\".\n\nThen press F3 in the main menu to load the map.", "Help");
        }
        //Load mods button
        private void dllButton_Click(object sender, EventArgs e)
        {
            LoadMods(true);
        }

        private static bool LoadMods(bool showMessages)
        {
            String strDLLName = exePath + "RLM.dll"; // here you put the dll you want, only the path.
            String strProcessName = "RocketLeague"; //here you will put the process name without ".exe"

            Int32 ProcID = GetProcessId(strProcessName);
            if (ProcID >= 0)
            {
                IntPtr hProcess = (IntPtr)OpenProcess(0x1F0FFF, 1, ProcID);
                if (hProcess == null)
                {
                    if (showMessages)
                        MessageBox.Show("OpenProcess() Failed!");
                    return false;
                }
                else
                {
                    if (!File.Exists(strDLLName))
                    {
                        WriteToLog("Missing DLL");
                        if (showMessages)
                            MessageBox.Show("DLL Missing");
                        return false;
                    }
                    InjectDLL(hProcess, strDLLName, showMessages);

                }
            }
            return true;
        }

     
        // Write to log function - Debugging
        // TU - Fixed log so it appends each time.
        private static void WriteToLog(string text)
        {
            using (StreamWriter writer = new StreamWriter("log.txt", true))
            {
                writer.WriteLine(text);
            }

        }

        //Set RL Path
        private void setRLPathToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SavePath(true);
            InitMaps();
        }
        //Reset settings
        private void resetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Hidden_checkBox.Checked = false;
            nameChange_CheckBox.Checked = false;
            customBlog_checkBox.Checked = false;
            unlJumps_checkBox.Checked = false;
            zombieCheckBox.Checked = false;
            spiderManCheckBox.Checked = false;
            DemoOnOppCheckBox.Checked = false;
            randomSizeBotsCheckBox.Checked = false;
            title_textBox.Text = "Rocket League Mods";
            body_textBox.Text = "/r/RocketLeagueMods";
            motd_textBox.Text = "Rocket Launcher by ButterandCream";
            youtubeTitle_textBox.Text = "Youtube";
            youtubeURL_textBox.Text = "https://www.youtube.com/";
            jump_text.Text = "1.5";
            ball_text.Text = "1";
            car_text.Text = "1";
            goal_text.Text = "{Player} Scored!";
            spinRateText.Text = "5.5";
            speedText.Text = "2300.0";
            ballGravityScaleText.Text = "1";
            bounceScaleText.Text = "1";
            WriteToLog("Reset Settings.");
        }


        //To Implement
        //First
        private void spiderManCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }
        //Second
        private void DemoOnOppCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }
        //Third
        private void randomSizeBotsCheckBox_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void RLCustomizer_FormClosing(object sender, FormClosingEventArgs e)
        {
            isClosing = true;
        }

        private static void CheckForProcess()
        {
            while (!isClosing && Properties.Settings.Default.AutoLoadMods)
            {
                string rlPath = GetProcessPath("RocketLeague");
                if (Properties.Settings.Default.AutoLoadMods && rlPath != string.Empty && !isRunning)
                {
                    // Sleep enough to let process initialize
                    Thread.Sleep(1000);

                    WriteToLog("RocketLeague Start detected.");
                    // Update RL path
                    if (LoadMods(true))
                    {
                        WriteToLog("Auto Loaded mods, awesome.");
                        isRunning = true;

                    }
                    else
                    {
                        WriteToLog("Error auto loading mods.");
                        isRunning = false;

                    }

                }
                else if (rlPath == string.Empty)
                {
                    isRunning = false;
                }
                Thread.Sleep(1000);
            }

        }

        private void ProcessStarted(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            string processName = targetInstance.Properties["Name"].Value.ToString();

        }

        private void autoLoadModsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoLoadMods = autoLoadModsToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void autoLoadModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            autoLoadModsToolStripMenuItem.Checked = !autoLoadModsToolStripMenuItem.Checked;
        }

        /* All functions related to the map loader
         * 
         *  BEGIN
         *  
         */

         //Add Maps Button
        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = mapFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string filename = Path.GetFileName(mapFileDialog.FileName);
                AddMaps(filename);
                
            }
        }

        //Initiate the maps.txt file
        //Checks if it exists and if not create the map file
        //Otherwise just read the maps
        // TU - changed map names to have spaces to match dictionary
        private void InitMaps()
        {
            if (Properties.Settings.Default.RLPath == String.Empty)
            {
                WriteToLog("Path empty... returning from InitMaps()");
                return;
            }
            if (!File.Exists(Properties.Settings.Default.RLPath + "maps.txt"))
            {
                using (StreamWriter writer = new StreamWriter(Properties.Settings.Default.RLPath + "maps.txt", true))
                {
                    writer.WriteLine("Advanced Tutorial" + Environment.NewLine + "Basic Tutorial" + Environment.NewLine + "Beckwith Park" + Environment.NewLine + "Beckwith Park (Midnight)" + Environment.NewLine +
                        "Beckwith Park (Stormy)" + Environment.NewLine + "Cosmic (Rocket Labs)" + Environment.NewLine + "DFH Stadium"
                        + Environment.NewLine + "DFH Stadium (Snowy)" + Environment.NewLine + "Double Goal (Rocket Labs)" + Environment.NewLine + "Dunk House" + Environment.NewLine + "Mannfield" + Environment.NewLine +
                        "Mannfield (Stormy)" + Environment.NewLine + "Neo Tokyo" + Environment.NewLine + "Pillars (Rocket Labs)" + Environment.NewLine + "Test Volleyball" + Environment.NewLine + "Underpass (Rocket Labs)"
                         + Environment.NewLine + "Underpass V0 (Rocket Labs)" + Environment.NewLine + "Urban Central" + Environment.NewLine + "Urban Central (Night)" + Environment.NewLine +
                         "Utopia Coliseum" + Environment.NewLine + "Utopia Coliseum (Dusk)" + Environment.NewLine + "Utopia Retro (Rocket Labs)" + Environment.NewLine + "Wasteland" + Environment.NewLine + "[Custom Maps]");
                    writer.Close();
                }
                readMaps();

                WriteToLog("maps.txt created");
            }
            else
            {
                readMaps();
                WriteToLog("maps.txt already exists, reading maps...");
            }
        }

        //Add custom map to file and list box
        private void AddMaps(string mapName)
        {
            
            if (File.ReadAllText(Properties.Settings.Default.RLPath + "maps.txt").Contains(mapName))
            {
                MessageBox.Show("Map is already added");
                return;
            }
            using (StreamWriter writer = new StreamWriter(Properties.Settings.Default.RLPath + "maps.txt", true))
            {
                
                mapBoxList.Items.Add(mapName);
                writer.WriteLine(mapName);
                writer.Close();
                MessageBox.Show(mapName + " Added");
                WriteToLog(mapName + "added to the list.");
            }
        }

        //Reads the maps in from the file
        private void readMaps()
        {
            if (!File.Exists(Properties.Settings.Default.RLPath + "maps.txt"))
            {
                InitMaps();
                return;
            }
            string name;
            using (StreamReader reader = new StreamReader(Properties.Settings.Default.RLPath + "maps.txt"))
            {
                while((name = reader.ReadLine()) != null)
                {
                    mapBoxList.Items.Add(name);
                }
                reader.Close();
            }
            WriteToLog("Read maps.txt");
        }

        /// <summary>
        /// Information holder for a map and its hash value
        /// </summary>
        public struct MapInfo
        {
            public string filename; public string hash;

            public MapInfo(string file, string hash)
            {
                this.filename = file; this.hash = hash;
            }
        }

        /// <summary>
        /// Contains all the pre-existing maps in the game,
        /// associated with their name file and MD5 checksum
        /// </summary>
        public static Dictionary<string, MapInfo> Maps = new Dictionary<string, MapInfo>()
        {
            {"Beckwith Park",
                new MapInfo("Park_P.upk", "454386a16551d111da72d7654b87a325") },
            {"Beckwith Park (Stormy)",
                new MapInfo("Park_Rainy_P.upk", "12aceb944720f544ca2b03ad2204da49") },
            {"Beckwith Park (Midnight)",
                new MapInfo("Park_Night_P.upk", "36e05bf3ecc9da3b00e78b07978782be") },
            {"Mannfield",
                new MapInfo("EuroStadium_P.upk", "0527a5acd7661778fa7ff3e8a11c57ea") },
            {"Mannfield (Stormy)",
                new MapInfo("EuroStadium_Rainy_P.upk", "e1d9dc5ff839a44725d4b8c2e1a1df88") },
            {"DFH Stadium",
                new MapInfo("Stadium_P.upk", "0831c9ccd06df87262c78d39f624afa2") },
            {"DFH Stadium (Snowy)",
                new MapInfo("Stadium_Winter_P.upk", "30dee6b28fb79a4f71478bbaf8cb8007") },
            {"Urban Central",
                new MapInfo("TrainStation_P.upk", "44e9def6f85cef21bc8e33f9e9fd2698") },
            {"Urban Central (Night)",
                new MapInfo("TrainStation_Night_P.upk", "a84cc33435e278e2b914d0ea4c78ae1b") },
            {"Utopia Coliseum",
                new MapInfo("UtopiaStadium_P.upk", "7adf493dae2ad105c549774a1632c4c1") },
            {"Utopia Coliseum (Dusk)",
                new MapInfo("UtopiaStadium_Dusk_P.upk", "eb8fec01ced0f1a9b11e57396fb63dd7") },
            {"Wasteland",
                new MapInfo("Wasteland_P.upk", "9746df3e600b53f5a92f74546f134f52") },
            {"Neo Tokyo",
                new MapInfo("NeoTokyo_P.upk", "36391631356c52be0fb0012429b1a6be") },
            {"Dunk House",
                new MapInfo("HoopsStadium_P.upk", "86e7aa937bd1b695c9fb4059f3781676") },
            {"Pillars (Rocket Labs)",
                new MapInfo("Labs_CirclePillars_P.upk", "7542983ff992c8c4e10bbf92d60a5184") },
            {"Cosmic (Rocket Labs)",
                new MapInfo("Labs_Cosmic_P.upk", "014e1185bccb933aaab0ac43879e42ba") },
            {"Double Goal (Rocket Labs)",
                new MapInfo("Labs_DoubleGoal_P.upk", "cb573372da30131c8228f059f7568bdd") },
            {"Underpass (Rocket Labs)",
                new MapInfo("Labs_Underpass_P.upk", "812dbd0ebbc6ef05801768daa9a011f1") },
            {"Underpass V0 (Rocket Labs)",
                new MapInfo("Labs_Underpass_v0_p.upk", "ae429dc339c00c5c0b304123aad0cd73") },
            {"Utopia Retro (Rocket Labs)",
                new MapInfo("Labs_Utopia_P.upk", "2ee88af78786fee2091699e5bed979ac") },
            {"Test Volleyball",
                new MapInfo("test_Volleyball.upk", "99b6c052e8ac1527104445908903245f") },
            {"Basic Tutorial",
                new MapInfo("TutorialTest.upk", "8f05dc2abd1ccc5a350ed682cf89ad74") },
            {"Advanced Tutorial",
                new MapInfo("TutorialAdvanced.upk", "8223b670168244c5e7e6eb7e5e3e5acf") }
        };


        //Save Maps Settings
        private void saveMapsSettingsBtn_Click(object sender, EventArgs e)
        {
            string mapName = mapBoxList.Text;
            string gameType = gameTypeComboBox.Text;
            if (mapName == String.Empty || gameType == String.Empty || mapName == "[Official Maps]" || mapName == "[Custom Maps]")
            {
                MessageBox.Show("Please select a valid setting for all fields.");
                return;
            }
            else if (Properties.Settings.Default.RLPath == String.Empty)
            {
                MessageBox.Show("Please set your Rocket League Path");
            }


            String upkName = "Park_P";
            // Get map file name from combobox
            if (Maps.ContainsKey(mapName))
            {
                upkName = Maps[mapName].filename.Replace(".upk", "");
            }
            else
            {
                upkName = mapName.Replace(".upk", "");

            }

            String formattedGameType = "Game=TAGame.GameInfo_Tutorial_TA?Freeplay?";
            // Formatted gametype from combobox
            if (gameType.Equals("Freeplay"))
            {
                formattedGameType = "Game=TAGame.GameInfo_Tutorial_TA?Freeplay?";
            }
            else if (gameType.Equals("Exhibition"))
            {
                formattedGameType = "Game=TAGame.GameInfo_Soccar_TA?";
            }

            using (StreamWriter writer = new StreamWriter(Properties.Settings.Default.RLPath + "map_settings.txt"))
            {
                writer.WriteLine("open " + upkName + "?" + formattedGameType);

                writer.Close();
                
            }
            MessageBox.Show("Map loader settings saved.");
            WriteToLog("Map loader settings saved.");
        }

        private void joinServerButton_Click(object sender, EventArgs e)
        {
            // For now change map_settings.txt to match LAN Format
            using (StreamWriter writer = new StreamWriter(Properties.Settings.Default.RLPath + "map_settings.txt"))
            {
                writer.WriteLine("open " + joinIPBox.Text);

                writer.Close();

            }
            MessageBox.Show("Now press F3 in game to join the Server.");
            WriteToLog("Join Server settings saved.");
        }

        private void startServerButton_Click(object sender, EventArgs e)
        {
            // For now change map_settings.txt to match LAN Format
            using (StreamWriter writer = new StreamWriter(Properties.Settings.Default.RLPath + "map_settings.txt"))
            {
                writer.WriteLine("open " + "Park_P?listen?onlineprivate?");

                writer.Close();

            }
            MessageBox.Show("Now press F3 in game to join the Server.");
            WriteToLog("Create Server settings saved.");
        }
    }
}
