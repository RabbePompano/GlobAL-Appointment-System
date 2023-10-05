using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Dunnett_GlobAL_Appointment_System
{
    public partial class logonForm : Form
    {
        public logonForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            //get the user table from the database, put it into a datatable
            String sqlStr = "SELECT * FROM user";
            var cmd = new MySqlCommand(sqlStr, DBConnection.conn);
            var adp = new MySqlDataAdapter(cmd);
            var dt = new DataTable();
            adp.Fill(dt);


            bool usernameAndPasswordMatch = false;
            int usernameAndPasswordMatchIndex = -1;
            int dtRowCount = dt.Rows.Count;
            string username = "";
            string password = "";
            int userId = -1;


            //check the username and password combinations in the datatable
            for (int i = 0; i < dtRowCount; i++)
            {  
                if (dt.Rows[i]["userName"].ToString() == txtUserName.Text)
                {                    
                    if (dt.Rows[i]["password"].ToString() == txtPassword.Text)
                    {
                        usernameAndPasswordMatchIndex = i;
                        usernameAndPasswordMatch = true;
                        username = dt.Rows[i]["userName"].ToString();
                        password = dt.Rows[i]["password"].ToString();
                        userId = (int)dt.Rows[i]["userId"];
                    }
                }
            }

            // if the username and password is a match, open the mainform and write to a successful logon log
            if (usernameAndPasswordMatch)
            {
                MainForm main = new MainForm(userId, username);
                main.Show();
                this.Hide();
                this.Enabled = false;

                //write logon to file
                StreamWriter stream = File.AppendText("successful_logon_audit.txt");
                stream.WriteLine($"successful logon by user: \"{username}\" at {DateTime.UtcNow} UTC");
                stream.Close();
            }
            else
            {
                //Show authentication failed message--use French if the regional format is set to French
                CultureInfo ci = CultureInfo.CurrentCulture;  //note to self--change back to UI

                if (ci.TwoLetterISOLanguageName == "fr")
                {
                    MessageBox.Show("Authentification échouée. Mauvaise combinaison de nom d'utilisateur et de mot de passe.");
                }
                else
                {
                    MessageBox.Show("Authentication failed. Wrong username and password combination.");
                }

                
            }

        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void LogonForm_OnLoad(object sender, EventArgs e)
        {
            //set labels to French if the regional format is French
            //note to self--remove for capstone and rework
            

            CultureInfo ci = CultureInfo.CurrentCulture;
            //ci = new CultureInfo("fr");

            if (ci.TwoLetterISOLanguageName == "fr")
            {
                passwordLbl.Text = "Mot de passe:";
                usernameLbl.Text = "Nom d'utilisateur:";
                btnExit.Text = "Sortie";
                btnLogin.Text = "Se connecter";
                this.Text = "GlobAL Connexion";
            }

        }
    }
}
