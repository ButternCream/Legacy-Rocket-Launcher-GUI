using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rocket_League_Customizer
{
    public partial class Twitch : Form
    {
        public Twitch()
        {
            InitializeComponent();
        }

        private void authTokenLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.twitchapps.com/tmi/");
        }

        //Save button click
        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.twitchUsername = usernameText.Text;
            Properties.Settings.Default.twitchAuth = authText.Text;
            Properties.Settings.Default.Save();
            Close();
        }
    }
}
