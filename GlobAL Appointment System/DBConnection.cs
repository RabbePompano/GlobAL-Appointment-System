using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Windows.Forms;

namespace Dunnett_GlobAL_Appointment_System
{
    public static class DBConnection
    {
        public static MySqlConnection conn { get; set; }

        public static void startConnection()
        {
            string constr = ConfigurationManager.ConnectionStrings["localdb"].ConnectionString;

            try
            {
                conn = new MySqlConnection(constr);
                conn.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        public static void closeConnection()
        {
            try
            {
                if (conn != null)
                {
                    conn.Close();
                }

                conn = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
