using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
namespace Rocket_League_Customizer
{
    public partial class RLCustomizer : Form
    {

        public RLCustomizer()
        {
            InitializeComponent();
            InitCustomBlog();
            CheckFirstTime();
            InitSavedSettings();
            WriteToLog("Initialized");
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

        public Int32 GetProcessId(String proc)
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

        public void InjectDLL(IntPtr hProcess, String strDLLName)
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
        private void SavePath()
        {
            string rlPath = GetProcessPath("RocketLeague");
            if (rlPath == string.Empty)
            {
                MessageBox.Show("Please have Rocket League running.", "Error");
                return;
            }
            else
            {
                //Save in default settings
                try
                {
                    Properties.Settings.Default.RLPath = rlPath;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("Rocket League path saved as \n" + Properties.Settings.Default.RLPath, "Success");
                    WriteToLog("Path Saved");
                } catch (Exception e)
                {
                    MessageBox.Show("Unable to save Rocket League path.", "Error");
                    WriteToLog("Path Not Saved");
                    return;
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
        }

        //Function get the rocket league path to exe
        public string GetProcessPath(string name)
        {
            Process[] processes = Process.GetProcessesByName(name);

            if (processes.Length > 0)
            {
                string rl = processes[0].MainModule.FileName;
                rl = rl.Replace("\\", "\\\\");
                WriteToLog("Path: " + rl);
                return rl.Remove(rl.Length - 16);
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
            if (Properties.Settings.Default.RLPath == "")
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
                MessageBox.Show("Settings Saved");
                
                WriteToLog("Settings Saved");
                writer.Close();
            }
            
        }

        //Load saved settings from settings file
        private void InitSavedSettings()
        {
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
                }
                count++;
                

            }
            reader.Close();
        }

        //Start rocket league button event
        private void startRocketLeagueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //If the path isn't set tell them
            if (Properties.Settings.Default.RLPath == "")
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
            MessageBox.Show("Step 1: Select/Edit the values you want and click Save.\n\nStep 2: Click the Load Mods button (you only need to do this once).\n\nStep 3: Then hit the corresponding key.\n\n\tF1 requires you to be in the main menu.\n\tF2 requires you to be in game.\n", "Help");
        }
        //Load mods button
        private void dllButton_Click(object sender, EventArgs e)
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            exePath = exePath.Replace("\\", "\\\\");
            string exeName = System.AppDomain.CurrentDomain.FriendlyName;
            WriteToLog(exeName);
            //MessageBox.Show(exeName);
            //Added dynamic file name check
            exePath = exePath.Remove(exePath.Length - exeName.Length);
            //MessageBox.Show(exePath);
            WriteToLog(exePath);
            
            String strDLLName = exePath + "RLM.dll"; // here you put the dll you want, only the path.
            String strProcessName = "RocketLeague"; //here you will put the process name without ".exe"
           
            Int32 ProcID = GetProcessId(strProcessName);
            if (ProcID >= 0)
            {
                IntPtr hProcess = (IntPtr)OpenProcess(0x1F0FFF, 1, ProcID);
                if (hProcess == null)
                {
                    MessageBox.Show("OpenProcess() Failed!");
                    return;
                }
                else
                {
                    if (!File.Exists(strDLLName))
                    {
                        WriteToLog("Missing DLL");
                        MessageBox.Show("DLL Missing");
                        return;
                    }
                    InjectDLL(hProcess, strDLLName);
                    
                }
            }
        }

     
        //Write to log function - Debugging
        private void WriteToLog(string text)
        {
            using (StreamWriter writer = new StreamWriter("log.txt"))
            {
                writer.WriteLine(text);
            }

        }

        //Set RL Path
        private void setRLPathToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SavePath();
        }
        //Reset settings
        private void resetToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Hidden_checkBox.Checked = false;
            nameChange_CheckBox.Checked = false;
            customBlog_checkBox.Checked = false;
            unlJumps_checkBox.Checked = false;
            zombieCheckBox.Checked = false;
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

    }
}
