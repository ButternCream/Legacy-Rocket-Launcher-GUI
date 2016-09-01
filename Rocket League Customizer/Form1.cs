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
        bool dllInjected = false;

        public RLCustomizer()
        {
            InitializeComponent();
            InitCustomBlog();
            CheckFirstTime();
             
        }

        //Start DLL Injection

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
            MessageBox.Show("DLL Injected");
            return;
        }
        //End DLL Injection


        //If its their first time running the program, tell them what to do.
        private void CheckFirstTime()
        {
            if (Properties.Settings.Default.FirstTime)
            {
                MessageBox.Show("Welcome! To get everything properly set up, please start Rocket League through steam and then press the \"Save\" button.", "Welcome");
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
                MessageBox.Show("Please have Rocket League running before you click Save.", "Error");
                return;
            }
            else
            {
                //Save in default settings
                try
                {
                    Properties.Settings.Default.RLPath = rlPath;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("Rocket League Path Saved as \n" + Properties.Settings.Default.RLPath, "Success");
                } catch (Exception e)
                {
                    MessageBox.Show("Unable to save Rocket League path.", "Error");
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
            //mmr_checkBox.Enabled = false;

            /*jump_text.Enabled = false;
            ball_text.Enabled = false;
            car_text.Enabled = false;
            goal_text.Enabled = false;
            unlJumps_checkBox.Enabled = false;
            zombieCheckBox.Enabled = false;
            Hidden_checkBox.Enabled = false;
            nameChange_CheckBox.Enabled = false;
            */
        }

        //Function get the rocket league path to exe
        public string GetProcessPath(string name)
        {
            Process[] processes = Process.GetProcessesByName(name);

            if (processes.Length > 0)
            {
                string rl = processes[0].MainModule.FileName;
                rl = rl.Replace("\\", "\\\\");
                return rl.Remove(rl.Length - 16);
            }
            else
            {
                return string.Empty;
            }
        }

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



        private void saveBtn_Click(object sender, EventArgs e)
        {
            //If path isn't set save it
            if (Properties.Settings.Default.RLPath == "")
            {
                SavePath();
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
                    writer.WriteLine(motd_textBox.Text);
                    writer.WriteLine(youtubeTitle_textBox.Text);
                    writer.WriteLine(youtubeURL_textBox.Text);

                } else {
                    writer.WriteLine("0");
                }
                MessageBox.Show("Settings Saved");
            }
            
        }

        private void startRocketLeagueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //If the path isn't set tell them
            if (Properties.Settings.Default.RLPath == "")
            {
                MessageBox.Show("Path not set. Please launch rocket league and press the \"Save\" button.", "Error");
                return;
            }
            System.Diagnostics.Process.Start(Properties.Settings.Default.RLPath + "RocketLeague.exe"); //To Add: ...Start(path,command line arguments)
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

        private void injectDLLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void howToUseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Step 1: Select/Edit the values you want and click Save.\n\nStep 2: Click the Inject DLL button (you only need to do this once).\n\nStep 3:Then hit the corresponding number on the numpad.\n\n\tNumpad 1 requires you to be in the main menu.\n\tNumpad 2 requires you to be in game.\n\nNote\nIf you enable Hidden Maps or In Game Name Change you must go into training and back out before they activate.", "Help");
        }

        private void dllButton_Click(object sender, EventArgs e)
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            exePath = exePath.Replace("\\", "\\\\");
            exePath = exePath.Remove(exePath.Length - 28);
            //MessageBox.Show(exePath);
            String strDLLName = exePath + "RocketLeagueTest.dll"; // here you put the dll you want, only the path.
            String strProcessName = "RocketLeague"; //here you will put the process name without ".exe"
            if (!dllInjected)
            {
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
                        InjectDLL(hProcess, strDLLName);
                        dllInjected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Dll Already Injected", "Error");
            }
        }
    }
}
