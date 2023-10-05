using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Dunnett_GlobAL_Appointment_System
{
    public partial class MainForm : Form
    {
        //global data
        string dbDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        int loggedInUserId = -1;
        string loggedInUserName = "";
        DataTable dtAppointments = new DataTable();
        DataTable dtCustomers = new DataTable();

        int appointmentIdToEdit = -1;
        int customerIdToEdit = -1;

        //modes and states
        const int Disable = 0;
        const int Enable = 1;
        const int Selection = 2;
        const int Neutral = -1;

        const int New = 0;
        const int Updating = 1;
        

        int appointmentTabState = Neutral;
        int customerTabState = Neutral;


        public MainForm(int userId, string userName)
        {
            InitializeComponent();

            loggedInUserId = userId;
            loggedInUserName = userName;
        }
        
        private void onFormLoad(object sender, EventArgs e)
        {
            lblCurrentLoggedIn.Text = "Currently logged in as: " + loggedInUserName;
            fillAppointmentDataTable();
            fillCustomerDataTable();

            //dashboard tab fill and setup
            btnDashboard.BackColor = Color.Goldenrod;
            fillUserSearchComboBox(cboxDashboardUsers);
            cboxDashboardUsers.SelectedIndex = 0;
            cboxDashboardMonths.SelectedIndex = 0;
            fillMonthlyAppointmentChart();
            fillOtherReports();

            //appointment tab fill and setup
            btnEditAppointment.Text = "Update Appointment";
            fillUserSelectComboBox(cboxAppointmentConsultant);
            fillCustomerSelectComboBox(cboxAppointmentCustomer);
            cboxAppointmentType.SelectedIndex = 0;
            fillAppointmentDataGridView(dgvAppointmentList);
            dgvAppointmentList.ClearSelection();
            btnDeleteAppointment.Enabled = false;
            btnEditAppointment.Enabled = false;
            btnEditAppointment.BackColor = Color.LightSlateGray;
            btnDeleteAppointment.BackColor = Color.LightSlateGray;

            //customers tab fill and setup
            CustomerTabControl(Disable, Enable);
            fillCustomerDataGridView(dgvCustomerList);


            //check appointment reminder alert
            AppointmentReminderAlert();
      
        }
        private void formClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }


        //****************  METHODS  ***************//
        private void fillAppointmentDataTable()
        {
            dtAppointments.Clear();

            string sqlstr = "SELECT appointment.appointmentId, customer.customerId, user.userID, customer.customerName AS Customer, appointment.type AS Type, " +
                "appointment.start AS Start, appointment.end AS End, user.userName AS Consultant " +
                "FROM appointment " +
                "INNER JOIN customer ON appointment.customerId = customer.customerId " +
                "INNER JOIN user ON appointment.userId = user.userId";

            MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
            MySqlDataAdapter adp = new MySqlDataAdapter(cmd);
            adp.Fill(dtAppointments);

            foreach (DataRow row in dtAppointments.Rows)
            {
               //convert the database dates to DateTime objects
                DateTime converterStart = (DateTime)row["Start"];
                DateTime.SpecifyKind(converterStart, DateTimeKind.Utc);
                DateTime converterEnd = (DateTime)row["End"];
                DateTime.SpecifyKind(converterEnd, DateTimeKind.Utc);

                //convert to Local Time
                converterStart = converterStart.ToLocalTime();
                row["Start"] = converterStart;
                converterEnd = converterEnd.ToLocalTime();
                row["End"] = converterEnd;
            }
            
        }
        private void fillCustomerDataTable()
        {
            dtCustomers.Clear();
            string sqlstr = "SELECT customer.customerId, address.addressId, city.cityId, country.countryId, " +
                "customer.customerName AS Customer, address.address AS Address, city.city AS City, country.country AS Country, " +
                "address.postalCode AS Postal_Code, address.phone AS Phone " +
                "FROM customer " +
                "INNER JOIN address ON customer.addressId = address.addressId " +
                "INNER JOIN city ON address.cityId = city.cityId " +
                "INNER JOIN country ON city.countryId = country.countryId";

            MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
            MySqlDataAdapter adp = new MySqlDataAdapter(cmd);
            adp.Fill(dtCustomers);
        }
        private void AppointmentReminderAlert()
        {
            int minutesAheadToCheck = 15;
            string now = DateTime.UtcNow.ToString(dbDateTimeFormat);
            string minutesFromNow = DateTime.UtcNow.AddMinutes(minutesAheadToCheck).ToString(dbDateTimeFormat);

            string sqlstr = $"SELECT * FROM appointment WHERE (userId = {loggedInUserId}) AND (start BETWEEN '{now}' AND '{minutesFromNow}')";

            MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
            MySqlDataAdapter adp = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            adp.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                MessageBox.Show($"Alert! You have an appointment within the next {minutesAheadToCheck} minutes!");
            }
        }
        private bool OverlappingAppointmentCheck(DataTable dt)
        {
            bool overlappingAppointment = false;

            DateTime start = new DateTime(dtpAppointmentDate.Value.Year, dtpAppointmentDate.Value.Month, 
                dtpAppointmentDate.Value.Day, dtpAppointmentTimeStart.Value.Hour, dtpAppointmentTimeStart.Value.Minute, 0);
            DateTime end = new DateTime(dtpAppointmentDate.Value.Year, dtpAppointmentDate.Value.Month,
                dtpAppointmentDate.Value.Day, dtpAppointmentTimeEnd.Value.Hour, dtpAppointmentTimeEnd.Value.Minute, 0);

             foreach (DataRow row in dt.Rows)
            {
                //if the user matches selected user or customer matches selected customer, we must check for overlap
                if ((int)row["userId"] == (int)cboxAppointmentConsultant.SelectedValue || (int)row["customerId"] == (int)cboxAppointmentCustomer.SelectedValue)
                {
                    //if we are editing an appointment, skip that appointment in the database, else check for overlap
                    if (appointmentTabState == Updating && appointmentIdToEdit == (int)row["appointmentId"])
                    {
                        //skip
                    }
                    else
                    {
                        if (start == (DateTime)row["Start"])
                        {
                            overlappingAppointment = true;
                        }
                        else if (start < (DateTime)row["Start"] && end > (DateTime)row["Start"])
                        {
                            overlappingAppointment = true;
                        }
                        else if (start > (DateTime)row["Start"] && start < (DateTime)row["End"])
                        {
                            overlappingAppointment = true;
                        }
                    }                   

                }

            }

            return overlappingAppointment;
        }
        private void fillAppointmentDataGridView(DataGridView dgv)
        {           
            dgv.DataSource = dtAppointments;
            dgv.Columns["appointmentId"].Visible = false;
            dgv.Columns["customerId"].Visible = false;
            dgv.Columns["userId"].Visible = false;
            dgv.ClearSelection();

        }
        private void fillCustomerDataGridView(DataGridView dgv)
        {
            dgv.DataSource = dtCustomers;
            dgv.Columns["customerId"].Visible = false;
            dgv.Columns["addressId"].Visible = false;
            dgv.Columns["cityId"].Visible = false;
            dgv.Columns["countryId"].Visible = false;
            dgv.ClearSelection();
        }
        private void fillOtherReports()
        {
            int totalAppointmentsCount = -1;
            int appointmentsRemainingToday = -1;
            int appointmentsRemainingThisWeek = -1;
            
            //get and show the amount of all appointments in the database
            string sqlstr = "SELECT COUNT(*) FROM appointment";

            MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
            totalAppointmentsCount = Convert.ToInt32(cmd.ExecuteScalar());
            lblTotalAppointmentsInDatabase.Text = "Total appointments found in the database: " + totalAppointmentsCount.ToString();

            //get and show the amount of all appointments remaining today that haven't ended yet
            string startPeriod = DateTime.UtcNow.ToString(dbDateTimeFormat);
            string endPeriod = DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59).ToUniversalTime().ToString(dbDateTimeFormat);

            sqlstr = $"SELECT COUNT(*) FROM appointment WHERE end BETWEEN '{startPeriod}' AND '{endPeriod}'"; 
            cmd = new MySqlCommand(sqlstr, DBConnection.conn);

            appointmentsRemainingToday = Convert.ToInt32(cmd.ExecuteScalar());
            lblApptsRemainingToday.Text = "Today: " + appointmentsRemainingToday;

            //get and show the amount of all appointments remaining this week that haven't ended yet
            int dayOfWeek = (int)DateTime.Today.DayOfWeek;            
            endPeriod = DateTime.Today.AddDays(6 - dayOfWeek).AddHours(23).AddMinutes(59).AddSeconds(59).ToUniversalTime().ToString(dbDateTimeFormat);
            
            sqlstr = $"SELECT COUNT(*) FROM appointment WHERE end BETWEEN '{startPeriod}' AND '{endPeriod}'";           
            cmd = new MySqlCommand(sqlstr, DBConnection.conn);

            appointmentsRemainingThisWeek = Convert.ToInt32(cmd.ExecuteScalar());
            lblApptsRemainingWeek.Text = "This week: " + appointmentsRemainingThisWeek;

        }
        private void fillMonthlyAppointmentChart()
        {
            ////fill the chart of appointments
            string sqlstr = "";
            DateTime startChartMonth = new DateTime();      //holds the first month we want to display on the chart
            DateTime endChartMonth = new DateTime();        //holds the last month we want to display on the chart

            if (rbtnDashboardChartThisYear.Checked)
            {
                startChartMonth = new DateTime(DateTime.Today.Year, 1, 1);
                endChartMonth = new DateTime(DateTime.Today.Year, 12, 31, 23, 59, 59);

            }
            else if (rbtnDashboardChartMonths.Checked)
            {
                startChartMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                endChartMonth = startChartMonth.AddMonths(12);          
                endChartMonth = endChartMonth.AddDays(DateTime.DaysInMonth(endChartMonth.Year, endChartMonth.Month)-1);
                endChartMonth = endChartMonth.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            //convert to UTC before communicating with SQL server
            startChartMonth = startChartMonth.ToUniversalTime();
            endChartMonth = endChartMonth.ToUniversalTime();

            DataTable dtApptsByMonth = new DataTable();

            //if the type check box is checked, sort by type
            if (chboxDashboardTypeChart.Checked)
            {
                //sqlstr = $"SELECT MONTH(start) AS Month, type AS Type, COUNT(appointmentId) AS Amount FROM appointment WHERE start " +
                //$"BETWEEN '{startChartMonth.ToString(dbDateTimeFormat)}' AND '{endChartMonth.ToString(dbDateTimeFormat)}' " +
                //$"GROUP BY MONTH(start), type ORDER BY MONTH(start)";
                //MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                //MySqlDataAdapter adp = new MySqlDataAdapter(cmd);
                //adp.Fill(dtApptsByMonth);
            }
            else
            {
                sqlstr = $"SELECT MONTH(start) Month, COUNT(appointmentId) Amount FROM appointment WHERE start " +
                $"BETWEEN '{startChartMonth.ToString(dbDateTimeFormat)}' AND '{endChartMonth.ToString(dbDateTimeFormat)}' " +
                $"GROUP BY MONTH(start) ORDER BY MONTH(start)";
                MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                MySqlDataAdapter adp = new MySqlDataAdapter(cmd);
                adp.Fill(dtApptsByMonth);
            }
            
            
            //go back to working with local time
            startChartMonth = startChartMonth.ToLocalTime();

            //add 12 DateTime objects representing each month to a list, start with startChartMonth
            List<DateTime> months = new List<DateTime>();

            for (int i = 0; i < 12; i++)
            {
                months.Add(startChartMonth.AddMonths(i));
            }

            //convert the month objects to a string with format "Jan 2023" to a new list
            List<string> monthStrings = new List<string>();

            foreach (DateTime date in months)
            {
                monthStrings.Add(date.ToString("MMM yyyy"));
            }

           
            if (chboxDashboardTypeChart.Checked)
            {
                

            }
            else
            {
                //create a list to hold the appointment count per month, starting with the first month, use long to match database type
                List<long> monthAmounts = new List<long>();

                //initialize the 12 month amounts to zero
                for (int i = 0; i < 12; i++)
                {
                    monthAmounts.Add(0);
                }

                //overwrite the appointment Amount data on the appointment amount list found from the SQL query datatable
                foreach (DateTime date in months)
                {
                    for (int i = 0; i < dtApptsByMonth.Rows.Count; i++)
                    {
                        if (date.Month == dtApptsByMonth.Rows[i].Field<int>("Month"))
                        {
                            monthAmounts[months.IndexOf(date)] = dtApptsByMonth.Rows[i].Field<long>("Amount");
                        }
                    }
                }

                chartApptsByMonth.Series["Appointments"].Points.DataBindXY(monthStrings, monthAmounts);
            }
            
        }
        private void fillCustomerSelectComboBox(ComboBox combo)
        {
            string sqlStr = "SELECT customerId, customerName FROM customer";
            var cmd = new MySqlCommand(sqlStr, DBConnection.conn);
            var adp = new MySqlDataAdapter(cmd);
            var dtCustomers = new DataTable();
            adp.Fill(dtCustomers);

            DataRow row = dtCustomers.NewRow();
            row["customerId"] = -1;
            row["customerName"] = "--Select--";
            dtCustomers.Rows.InsertAt(row, 0);

            combo.DataSource = dtCustomers;
            combo.ValueMember = "customerId";
            combo.DisplayMember = "customerName";
            combo.SelectedIndex = 0;

        }
        private void fillUserSelectComboBox(ComboBox combo)
        {
            string sqlStr = "SELECT userId, userName FROM user";
            var cmd = new MySqlCommand(sqlStr, DBConnection.conn);
            var adp = new MySqlDataAdapter(cmd);
            var dtUsers = new DataTable();
            adp.Fill(dtUsers);

            DataRow row = dtUsers.NewRow();
            row["userId"] = -1;
            row["userName"] = "--Select--";
            dtUsers.Rows.InsertAt(row, 0);

            combo.DataSource = dtUsers;
            combo.DisplayMember = "userName";
            combo.ValueMember = "userId";
            combo.SelectedIndex = 0;
        }
        private void fillUserSearchComboBox(ComboBox combo)
        {
            string sqlStr = "SELECT userId, userName FROM user";
            var cmd = new MySqlCommand(sqlStr, DBConnection.conn);
            var adp = new MySqlDataAdapter(cmd);
            var dtUsers = new DataTable();
            adp.Fill(dtUsers);

            DataRow row = dtUsers.NewRow();
            row["userId"] = -1;
            row["userName"] = "All Users";
            dtUsers.Rows.InsertAt(row, 0);

            combo.DataSource = dtUsers;
            combo.DisplayMember = "userName";
            combo.ValueMember = "userId";
            combo.SelectedIndex = 0;

        }
        private void DisableAndResetAllTabs()
        {
            panelDashboard.Hide();
            panelDashboard.Enabled = false;
            btnDashboard.BackColor = Color.CornflowerBlue;
            rbtnDashboardChartThisYear.Checked = true;
            rbtnDashboardToday.Checked = true;
            cboxDashboardUsers.SelectedIndex = 0;
            dgvScheduleCalendar.DataSource = null;

            panelAppointments.Hide();
            panelAppointments.Enabled = false;
            btnAppointments.BackColor = Color.CornflowerBlue;
            AppointmentEditDeleteSectionControl(Enable);
            ResetAppointmentTabFields();
            appointmentTabState = Neutral;

            panelCustomers.Hide();
            panelDashboard.Enabled = false;
            btnCustomers.BackColor = Color.CornflowerBlue;
            CustomerTabControl(Disable, Enable);
            dgvCustomerList.ClearSelection();
            customerTabState = Neutral;
        }    
        private string AppointmentSaveErrorCheck()
        {
            string errorMessage = "";
            int intErrors = 0;

            if (cboxAppointmentCustomer.Text == "--Select--")
            {
                errorMessage += "You must select a customer.\n";
                intErrors++;
            }
            if (cboxAppointmentType.Text == "--Select--")
            {
                errorMessage += "You must select an appointment type.\n";
                intErrors++;
            }
            if (dtpAppointmentDate.Value.DayOfWeek.ToString() == "Saturday" || dtpAppointmentDate.Value.DayOfWeek.ToString() == "Sunday")
            {
                errorMessage += "You cannot make an appointment for the weekend.\n";
                intErrors++;
            }
            else if (dtpAppointmentTimeStart.Value.Hour < 9 || dtpAppointmentTimeStart.Value.Hour > 16)
            {
                errorMessage += "An appointment must be during business hours 9am-5pm.\n";
                intErrors++;
            }
            else if (dtpAppointmentTimeEnd.Value.Hour < 9 || (dtpAppointmentTimeEnd.Value.Hour == 17 && dtpAppointmentTimeEnd.Value.Minute != 0) || dtpAppointmentTimeEnd.Value.Hour > 17)
            {
                errorMessage += "An appointment must be during business hours 9am-5pm.\n";
                intErrors++;
            }
            if (dtpAppointmentTimeStart.Value >= dtpAppointmentTimeEnd.Value)
            {
                errorMessage += "Your appointment end time must be after the start.\n";
                intErrors++;
            }
            if (OverlappingAppointmentCheck(dtAppointments))
            {
                errorMessage += "Your appointment overlaps with an existing appointment.\n";
                intErrors++;
            }
            if (cboxAppointmentConsultant.Text == "--Select--")
            {
                errorMessage += "You must select a consultant.\n";
                intErrors++;
            }


            if (intErrors == 1)
            {
                errorMessage += "\n" + "Please fix it and try again.";
            }
            else if (intErrors > 1)
            {
                errorMessage += "\n" + "Please fix them and try again.";
            }

            return errorMessage;
        }     //returns a string of appointment save errors        
        private bool CustomerSaveErrorCheck()
        {
            bool errorFound = false;

            if (
                String.IsNullOrWhiteSpace(txtCustomerAddress.Text) || String.IsNullOrWhiteSpace(txtCustomerCity.Text) 
                || String.IsNullOrWhiteSpace(txtCustomerCountry.Text) || String.IsNullOrWhiteSpace(txtCustomerName.Text) 
                || String.IsNullOrWhiteSpace(txtCustomerPhone.Text) || String.IsNullOrWhiteSpace(txtCustomerPostalCode.Text)
                )
            {
                errorFound = true;
            }

            return errorFound;

        }       //returns true if an error found
        
        private bool DoesRecordExist(string table, string column, string searchText)
        {
            bool exists = false;
            int count = -1;
            string sqlstr = $"SELECT COUNT(*) FROM {table} WHERE {column} = '{searchText}'";

            try
            {
                MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                count = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            //anonymous function with lambda operator shrinks the code and removes an if-then statement
            Func<int, bool> checkExists = x => x > 0;
            exists = checkExists(count);

            return exists;
        }   //returns true if a record exists, from table where column=searchText
        private bool DoesRecordExist(string table, string column, string searchText, string secondColumn, int id)
        {
            bool exists = false;
            int count = -1;
            string sqlstr = $"SELECT COUNT(*) FROM {table} WHERE {column} = '{searchText}' AND {secondColumn} = {id}";

            try
            {
                MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                count = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            //anonymous function with lambda operator shrinks the code and removes an if-then statement
            Func<int, bool> checkExists = x => x > 0;
            exists = checkExists(count);

            return exists;
        }   //returns true if a record exists from table where column=searchText and secondcolumn=id
        private int GetOrCreateCountryId(string countryName)
        {
            int id = -1;
            string sqlstr = "";
 
            //check if a country record does not exist, if so create a new country record
            if (!DoesRecordExist("country", "country", countryName))
            {
                //create new country
                sqlstr = $"INSERT INTO country (country, createDate, createdBy, lastUpdate, lastUpdateBy) " +
                    $"VALUES ('{countryName}', '{DateTime.UtcNow.ToString(dbDateTimeFormat)}', '{loggedInUserName}', '{DateTime.UtcNow.ToString(dbDateTimeFormat)}', '{loggedInUserName}')";

                try
                {
                    MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            }

            //get country Id
            sqlstr = $"SELECT countryId FROM country WHERE country = '{countryName}'";
            try
            {
                MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());               
            }

            return id;
        }       //finds or creates a country record and returns the countryId
        private int GetOrCreateCityId(string cityName, int countryId)
        {
            int cityId = -1;
            string sqlstr;

            //check if a city record does not exist in a country, if so create a new city record
            if (!DoesRecordExist("city", "city", cityName, "countryId", countryId))
            {
                //create new country
                sqlstr = $"INSERT INTO city (city, countryId, createDate, createdBy, lastUpdate, lastUpdateBy) " +
                    $"VALUES ('{cityName}', {countryId}, '{DateTime.UtcNow.ToString(dbDateTimeFormat)}', '{loggedInUserName}', '{DateTime.UtcNow.ToString(dbDateTimeFormat)}', '{loggedInUserName}')";

                try
                {
                    MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            }

            //get city Id
            sqlstr = $"SELECT cityId FROM city WHERE city = '{cityName}' AND countryId = {countryId}";
            try
            {
                MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                cityId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return cityId;

        }       //finds or creates a city record and returns the cityId
        private int GetOrCreateAddressId(string addressName, int cityId, string postal, string phoneNum)
        {
            int addressId = -1;
            string sqlstr;


            //if an address does not exist within that city, create a new address
            if (!DoesRecordExist("address", "address", addressName, "cityId", cityId))
            {
                sqlstr = $"INSERT INTO address (address, address2, cityId, postalCode, phone, createDate, createdBy, lastUpdate, lastUpdateBy) " +
                $"VALUES ('{addressName}', 'null', {cityId}, '{postal}', '{phoneNum}', '{DateTime.UtcNow.ToString(dbDateTimeFormat)}', '{loggedInUserName}', '{DateTime.UtcNow.ToString(dbDateTimeFormat)}', '{loggedInUserName}')";

                try
                {
                    MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            //get address Id
            sqlstr = $"SELECT addressId FROM address WHERE address = '{addressName}' AND cityId = {cityId}";
            try
            {
                MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                addressId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            //if updating a customer record, also update the address info
            if (customerTabState == Updating)
            {
                string now = DateTime.UtcNow.ToString(dbDateTimeFormat);
                sqlstr = $"UPDATE address SET postalCode = '{postal}', phone = '{phoneNum}', lastUpdate = '{now}', lastUpdateBy = '{loggedInUserName}' WHERE addressId = {addressId}";
                
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            }

            return addressId;
        }       //finds of creates an address record and returns an addressId
        private void AppointmentEditDeleteSectionControl(int state)
        {
   
            if (state == 1)
            {
                btnDeleteAppointment.Enabled = false;
                btnEditAppointment.Enabled = false;
                btnEditAppointment.BackColor = Color.LightSlateGray;
                btnDeleteAppointment.BackColor = Color.LightSlateGray;
                dgvAppointmentList.BackgroundColor = Color.WhiteSmoke;
                dgvAppointmentList.DefaultCellStyle.BackColor = Color.WhiteSmoke;

                lblAppointmentList.Visible = true;
                dgvAppointmentList.Enabled = true;
                dgvAppointmentList.ClearSelection();
            }
            else if(state == 0)
            {
                btnDeleteAppointment.Enabled = false;
                btnEditAppointment.Enabled = false;
                btnEditAppointment.BackColor = Color.LightSlateGray;
                btnDeleteAppointment.BackColor = Color.LightSlateGray;
                dgvAppointmentList.BackgroundColor = Color.LightSlateGray;
                dgvAppointmentList.DefaultCellStyle.BackColor = Color.LightSlateGray;

                lblAppointmentList.Visible = false;
                dgvAppointmentList.Enabled = false;
                dgvAppointmentList.ClearSelection();
            }
            else if(state == 2)
            {
                btnDeleteAppointment.Enabled = true;
                btnEditAppointment.Enabled = true;
                btnEditAppointment.BackColor = Color.CornflowerBlue;
                btnDeleteAppointment.BackColor = Color.CornflowerBlue;

                lblAppointmentList.Visible = false;
                dgvAppointmentList.Enabled = true;
            }

        }   //controls the right section of the appointment tab
        private void ResetAppointmentTabFields()
        {
            //reset buttons and fields
            btnCancelAppointment.Enabled = false;
            btnSaveAppointment.Enabled = false;
            btnSaveAppointment.BackColor = Color.LightSlateGray;
            btnCancelAppointment.BackColor = Color.LightSlateGray;
            btnNewAppointment.Enabled = true;
            btnNewAppointment.BackColor = Color.CornflowerBlue;

            cboxAppointmentConsultant.Enabled = false;
            cboxAppointmentCustomer.Enabled = false;
            cboxAppointmentType.Enabled = false;
            dtpAppointmentDate.Enabled = false;
            dtpAppointmentTimeStart.Enabled = false;
            dtpAppointmentTimeEnd.Enabled = false;

            cboxAppointmentConsultant.SelectedIndex = 0;
            cboxAppointmentCustomer.SelectedIndex = 0;
            cboxAppointmentType.SelectedIndex = 0;
            dtpAppointmentDate.Value = DateTime.Now;
            dtpAppointmentTimeStart.Value = DateTime.Today.AddHours(12);
            dtpAppointmentTimeEnd.Value = DateTime.Today.AddHours(12);

            //disable labels
            lblAppointmentConsultant.Enabled = false;
            lblAppointmentCustomer.Enabled = false;
            lblAppointmentDate.Enabled = false;
            lblAppointmentEnd.Enabled = false;
            lblAppointmentStart.Enabled = false;
            lblAppointmentType.Enabled = false;
        }   //resets the left section of appointment tab
        private string AppointmentSaveSqlString(int type)
        {
            bool newAppointment = false;
            bool editAppointment = false;

            if (type == 0)
            {
                newAppointment = true;
            }
            else if(type == 1)
            {
                editAppointment = true;
            }

            string sqlstr = "";

            DateTime date = dtpAppointmentDate.Value;
            DateTime startTime = new DateTime(date.Year, date.Month, date.Day, dtpAppointmentTimeStart.Value.Hour, dtpAppointmentTimeStart.Value.Minute, 0);
            DateTime endTime = new DateTime(date.Year, date.Month, date.Day, dtpAppointmentTimeEnd.Value.Hour, dtpAppointmentTimeEnd.Value.Minute, 0);

            string customerId = cboxAppointmentCustomer.SelectedValue.ToString();
            string userId = cboxAppointmentConsultant.SelectedValue.ToString();
            string appType = cboxAppointmentType.Text;
            string start = startTime.ToUniversalTime().ToString(dbDateTimeFormat);
            string end = endTime.ToUniversalTime().ToString(dbDateTimeFormat);
            string createDate = DateTime.UtcNow.ToString(dbDateTimeFormat);
            string createdBy = loggedInUserName;
            string lastUpdate = "";
            string lastUpdatedBy = "";

            if (newAppointment)
            {
                lastUpdate = createDate;
                lastUpdatedBy = createdBy;
                sqlstr = $"INSERT INTO appointment (customerId, userId, title, description, location, contact, type, url, start, end, createDate, createdBy, lastUpdate, lastUpdateBy) " +
                   $"VALUES ({customerId}, {userId}, 'null' , 'null', 'null', 'null', '{appType}', 'null', '{start}', '{end}', '{createDate}', '{createdBy}', '{lastUpdate}', '{lastUpdatedBy}')";
            }
            else if (editAppointment)
            {
                lastUpdate = DateTime.UtcNow.ToString(dbDateTimeFormat);
                lastUpdatedBy = loggedInUserName;

                sqlstr = $"UPDATE appointment " +
                    $"SET customerId = {customerId}, userId = {userId}, type = '{appType}', start = '{start}', end = '{end}', lastUpdate = '{lastUpdate}', lastUpdateBy = '{lastUpdatedBy}' " +
                    $"WHERE appointmentId = {appointmentIdToEdit}";
            }
            
            return sqlstr;
        }   //creates an appointment save sql string--new or update
        private void CustomerTabControl(int leftside, int rightside)
        {
            if (leftside == Enable)
            {
                lblCustomerAddress.Enabled = true;
                lblCustomerCity.Enabled = true;
                lblCustomerCountry.Enabled = true;
                lblCustomerName.Enabled = true;
                lblCustomerPhone.Enabled = true;
                lblCustomerPostalCode.Enabled = true;

                txtCustomerAddress.Enabled = true;
                txtCustomerCity.Enabled = true;
                txtCustomerCountry.Enabled = true;
                txtCustomerName.Enabled = true;
                txtCustomerPhone.Enabled = true;
                txtCustomerPostalCode.Enabled = true;

                btnNewCustomer.Enabled = false;
                btnNewCustomer.BackColor = Color.LightSlateGray;
                btnCancelCustomer.Enabled = true;
                btnCancelCustomer.BackColor = Color.CornflowerBlue;
                btnSaveCustomer.Enabled = true;
                btnSaveCustomer.BackColor = Color.CornflowerBlue;
            }
            else if (leftside == Disable)
            {
                lblCustomerAddress.Enabled = false;
                lblCustomerCity.Enabled = false;
                lblCustomerCountry.Enabled = false;
                lblCustomerName.Enabled = false;
                lblCustomerPhone.Enabled = false;
                lblCustomerPostalCode.Enabled = false;

                txtCustomerAddress.Enabled = false;
                txtCustomerCity.Enabled = false;
                txtCustomerCountry.Enabled = false;
                txtCustomerName.Enabled = false;
                txtCustomerPhone.Enabled = false;
                txtCustomerPostalCode.Enabled = false;

                txtCustomerAddress.Text = "";
                txtCustomerCity.Text = "";
                txtCustomerCountry.Text = "";
                txtCustomerName.Text = "";
                txtCustomerPhone.Text = "";
                txtCustomerPostalCode.Text = "";

                btnNewCustomer.Enabled = true;
                btnNewCustomer.BackColor = Color.CornflowerBlue;
                btnCancelCustomer.Enabled = false;
                btnCancelCustomer.BackColor = Color.LightSlateGray;
                btnSaveCustomer.Enabled = false;
                btnSaveCustomer.BackColor = Color.LightSlateGray;
            }

            if (rightside == Enable)
            {
                dgvCustomerList.Enabled = true;
                dgvCustomerList.ClearSelection();
                lblCustomerList.Show();
                btnDeleteCustomer.Enabled = false;
                btnUpdateCustomer.Enabled = false;
                btnDeleteCustomer.BackColor = Color.LightSlateGray;
                btnUpdateCustomer.BackColor = Color.LightSlateGray;
            }
            else if (rightside == Disable)
            {
                dgvCustomerList.Enabled = false;
                dgvCustomerList.ClearSelection();
                lblCustomerList.Hide();
                btnDeleteCustomer.Enabled = false;
                btnUpdateCustomer.Enabled = false;
                btnDeleteCustomer.BackColor = Color.LightSlateGray;
                btnUpdateCustomer.BackColor = Color.LightSlateGray;
            }
            else if (rightside == Selection)
            {
                lblCustomerList.Hide();
                btnDeleteCustomer.Enabled = true;
                btnUpdateCustomer.Enabled = true;
                btnDeleteCustomer.BackColor = Color.CornflowerBlue;
                btnUpdateCustomer.BackColor = Color.CornflowerBlue;
            }

        }   //manages the behavior of customer tab controls


        //*************** EVENTS ***************//
        private void btnAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"'About' button not implemented.");
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"'Help' button not implemented.");
        }

        private void btnLogOut_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"'Log Out' button not implemented.");
        }

        private void btnEmployees_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"'Employees' button not implemented.");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        //tab change buttons
        private void btnDashboard_Click(object sender, EventArgs e)
        {
            //hide and disable all tabs
            DisableAndResetAllTabs();

            //show and enable dashboard tab
            panelDashboard.Show();
            panelDashboard.Enabled = true;
            btnDashboard.BackColor = Color.Goldenrod;

            //update reports
            fillUserSearchComboBox(cboxDashboardUsers);
            fillMonthlyAppointmentChart();
            fillOtherReports();     

        }
        private void btnAppointments_Click(object sender, EventArgs e)
        {
            //hide and disable all tabs
            DisableAndResetAllTabs();

            //enable appointments tab
            panelAppointments.Show();
            panelAppointments.Enabled = true;
            btnAppointments.BackColor = Color.Goldenrod;

            //refresh appointment datatable and datagridview
            fillAppointmentDataTable();
            dgvAppointmentList.ClearSelection();
            fillCustomerSelectComboBox(cboxAppointmentCustomer);
        }
        private void btnCustomers_Click(object sender, EventArgs e)
        {
            //hide and disable all tabs
            DisableAndResetAllTabs();

            //enable appointments tab
            panelCustomers.Show();
            panelCustomers.Enabled = true;
            btnCustomers.BackColor = Color.Goldenrod;

            //refresh customer datatable and datagridview
            fillCustomerDataTable();
            dgvCustomerList.ClearSelection();

        }


        //dashboard tab events
        private void btnDashboardScheduleSearch_Click(object sender, EventArgs e)
        {
            string sqlstr = "SELECT user.userName AS Username, appointment.start AS Start, appointment.end AS End, appointment.type AS Type, " +
                "customer.customerName AS Customer_Name FROM appointment INNER JOIN customer ON appointment.customerId = customer.customerId " +
                "INNER JOIN user ON appointment.userId = user.userId";

            //append a username WHERE clause to the sql string if choosing a specific user's schedule
            //......

            if (cboxDashboardUsers.Text != "All Users")
            {
                //append a WHERE clause for the User selected
                sqlstr += $" WHERE user.userId = {cboxDashboardUsers.SelectedValue}";
            }

            //append a time WHERE BETWEEN clause if choosing a specific time range
            //the method depends on the time range and if a user was selected as well
            string timeRangeBeginning = "";
            string timeRangeEnd = "";
            


            DateTime startOfToday = DateTime.Today.ToUniversalTime();
            DateTime endOfToday = DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59).ToUniversalTime();

            DateTime startOfWeek = startOfToday.AddDays(-(int)DateTime.Today.DayOfWeek).ToUniversalTime();
            DateTime endOfWeek = endOfToday.AddDays(6 - (int)DateTime.Today.DayOfWeek).ToUniversalTime();

            int daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);

            DateTime startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToUniversalTime();
            DateTime endOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, daysInMonth, 23, 59, 59).ToUniversalTime();
         


            //if a user is selected and a time is selected
            if (cboxDashboardUsers.Text != "All Users" && !rbtnDashboardAll.Checked)
            {
                if (rbtnDashboardToday.Checked)
                {
                    timeRangeBeginning = startOfToday.ToString(dbDateTimeFormat);
                    timeRangeEnd = endOfToday.ToString(dbDateTimeFormat);
                }
                else if (rbtnDashboardThisWeek.Checked)
                {
                    timeRangeBeginning = startOfWeek.ToString(dbDateTimeFormat);
                    timeRangeEnd = endOfWeek.ToString(dbDateTimeFormat);
                }
                else if (rbtnDashboardThisMonth.Checked)
                {
                    timeRangeBeginning = startOfMonth.ToString(dbDateTimeFormat);
                    timeRangeEnd = endOfMonth.ToString(dbDateTimeFormat);
                }
                
                sqlstr += $" AND start BETWEEN '{timeRangeBeginning}' AND '{timeRangeEnd}'";
            }        
            //if a user is NOT selected and a time is selected
            else if (cboxDashboardUsers.Text == "All Users" && !rbtnDashboardAll.Checked)
            {
                if (rbtnDashboardToday.Checked)
                {
                    timeRangeBeginning = startOfToday.ToString(dbDateTimeFormat);
                    timeRangeEnd = endOfToday.ToString(dbDateTimeFormat);
                }
                else if (rbtnDashboardThisWeek.Checked)
                {
                    timeRangeBeginning = startOfWeek.ToString(dbDateTimeFormat);
                    timeRangeEnd = endOfWeek.ToString(dbDateTimeFormat);
                }
                else if (rbtnDashboardThisMonth.Checked)
                {  
                    timeRangeBeginning = startOfMonth.ToString(dbDateTimeFormat);
                    timeRangeEnd = endOfMonth.ToString(dbDateTimeFormat);
                }

                sqlstr += $" WHERE start BETWEEN '{timeRangeBeginning}' AND '{timeRangeEnd}'";

            }

            var cmd = new MySqlCommand(sqlstr, DBConnection.conn);
            var adp = new MySqlDataAdapter(cmd);
            var dtScheduleCalendar = new DataTable();

            adp.Fill(dtScheduleCalendar);

            foreach (DataRow row in dtScheduleCalendar.Rows)
            {
                DateTime converter = (DateTime)row["Start"];                
                DateTime.SpecifyKind(converter, DateTimeKind.Utc);      //make sure the kind is set to UTC
                converter = converter.ToLocalTime();    
                row["Start"] = converter;                               

                converter = (DateTime)row["End"];
                DateTime.SpecifyKind(converter, DateTimeKind.Utc);
                converter = converter.ToLocalTime();
                row["End"] = converter;
            }

            dgvScheduleCalendar.DataSource = dtScheduleCalendar;

        }
        private void rbtnDashboardChartThisYear_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnDashboardChartThisYear.Checked)
            {
                fillMonthlyAppointmentChart();
            }
            
        }
        private void rbtnDashboardChartMonths_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnDashboardChartMonths.Checked)
            {
                fillMonthlyAppointmentChart();
            }
            
        }
        private void btnCalendarTypeMonth_Click(object sender, EventArgs e)
        {
            DateTime yearStart = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime yearEnd = new DateTime(DateTime.Now.Year, 12, 31, 23, 59, 59);
            yearStart = yearStart.ToUniversalTime();
            yearEnd = yearEnd.ToUniversalTime();
            string month = cboxDashboardMonths.Text;

            string sqlstr = $"SELECT MONTHNAME(start) AS Month, type AS Type, COUNT(appointmentId) AS Amount FROM appointment WHERE start " +
                $"BETWEEN '{yearStart.ToString(dbDateTimeFormat)}' AND '{yearEnd.ToString(dbDateTimeFormat)}' AND MONTHNAME(start) = '{month}'" +
                $"GROUP BY MONTHNAME(start), type ORDER BY MONTHNAME(start)";
            MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
            MySqlDataAdapter adp = new MySqlDataAdapter(cmd);
            DataTable dtApptsByMonth = new DataTable();
            adp.Fill(dtApptsByMonth);

            dgvScheduleCalendar.DataSource = dtApptsByMonth;
            dgvScheduleCalendar.Columns["Month"].Visible = false;

        }


        //appointment tab events
        private void btnSaveAppointment_Click(object sender, EventArgs e)
        {            
            //if there were input errors, show a message, do NOT allow a save until there are no errors
            string errorMessage = AppointmentSaveErrorCheck();    //empty string if no errors
            string sqlstr = "";

            if (appointmentTabState == New)
            {
                sqlstr = AppointmentSaveSqlString(New);
            }
            else if(appointmentTabState == Updating)
            {
                sqlstr = AppointmentSaveSqlString(Updating);
            }
         
            if (errorMessage == "")
            {                
                bool exception = false;

                try
                {
                    MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    exception = true;
                    MessageBox.Show(ex.ToString());
                }

                if (!exception)
                {
                    //reset buttons and fields
                    ResetAppointmentTabFields();

                    //update the data table from the server
                    fillAppointmentDataTable();

                    AppointmentEditDeleteSectionControl(Enable);

                    if (appointmentTabState == New)
                    {
                        MessageBox.Show("Appointment added.");
                    }
                    else if (appointmentTabState == Updating)
                    {
                        MessageBox.Show("Appointment changed.");
                    }
                    
                }
            }
            else
            {
                MessageBox.Show(errorMessage, "Error Saving Appointment", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void btnNewAppointment_Click(object sender, EventArgs e)
        {
            appointmentTabState = New;

            //enable buttons and fields
            btnCancelAppointment.Enabled = true;
            btnSaveAppointment.Enabled = true;
            btnSaveAppointment.BackColor = Color.CornflowerBlue;
            btnCancelAppointment.BackColor = Color.CornflowerBlue;
            cboxAppointmentConsultant.Enabled = true;
            cboxAppointmentCustomer.Enabled = true;
            cboxAppointmentType.Enabled = true;
            dtpAppointmentDate.Enabled = true;
            dtpAppointmentTimeStart.Enabled = true;
            dtpAppointmentTimeEnd.Enabled = true;
            btnNewAppointment.Enabled = false;
            btnNewAppointment.BackColor = Color.LightSlateGray;

            AppointmentEditDeleteSectionControl(Disable);

            //enable labels
            lblAppointmentConsultant.Enabled = true;
            lblAppointmentCustomer.Enabled = true;
            lblAppointmentDate.Enabled = true;
            lblAppointmentEnd.Enabled = true;
            lblAppointmentStart.Enabled = true;
            lblAppointmentType.Enabled = true;

        }

        private void btnCancelAppointment_Click(object sender, EventArgs e)
        {
            ResetAppointmentTabFields();
            AppointmentEditDeleteSectionControl(Enable);
        }

        private void btnDeleteAppointment_Click(object sender, EventArgs e)
        {
            DataRow row = ((DataRowView)dgvAppointmentList.SelectedRows[0].DataBoundItem).Row;
            int appointmentIdToDelete = (int)row["appointmentId"];

            string sqlstr = $"DELETE FROM appointment WHERE appointmentId = {appointmentIdToDelete}";
            bool exception = false;

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete?", "Appointment Delete Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    exception = true;
                    MessageBox.Show(ex.ToString());
                }

                if (!exception)
                {
                    //reset buttons and fields
                    ResetAppointmentTabFields();

                    //update the data table from the server
                    fillAppointmentDataTable();
                    AppointmentEditDeleteSectionControl(Enable);


                }
            }
            else if (dialogResult == DialogResult.No)
            {
                //do nothing
            }

            
        }

        private void btnEditAppointment_Click(object sender, EventArgs e)
        {
            appointmentTabState = Updating;

            //populate data from selected row into fields
            DataRow row = ((DataRowView)dgvAppointmentList.SelectedRows[0].DataBoundItem).Row;
            appointmentIdToEdit = (int)row["appointmentId"];

            cboxAppointmentCustomer.SelectedIndex = cboxAppointmentCustomer.FindStringExact(row["Customer"].ToString());
            cboxAppointmentConsultant.SelectedIndex = cboxAppointmentConsultant.FindStringExact(row["Consultant"].ToString());
            cboxAppointmentType.SelectedIndex = cboxAppointmentType.FindStringExact(row["Type"].ToString());
            dtpAppointmentDate.Value = (DateTime)row["Start"];
            dtpAppointmentTimeStart.Value = (DateTime)row["Start"];
            dtpAppointmentTimeEnd.Value = (DateTime)row["End"];

            //enable the Save and Cancel buttons, disable new button
            btnCancelAppointment.Enabled = true;
            btnSaveAppointment.Enabled = true;
            btnSaveAppointment.BackColor = Color.CornflowerBlue;
            btnCancelAppointment.BackColor = Color.CornflowerBlue;
            btnNewAppointment.Enabled = false;
            btnNewAppointment.BackColor = Color.LightSlateGray;
            btnDeleteAppointment.Enabled = false;
            btnDeleteAppointment.BackColor = Color.LightSlateGray;
            btnEditAppointment.Enabled = false;
            btnEditAppointment.BackColor = Color.LightSlateGray;
            dgvAppointmentList.Enabled = false;
            dgvAppointmentList.ClearSelection();

            //enable fields
            cboxAppointmentConsultant.Enabled = true;
            cboxAppointmentCustomer.Enabled = true;
            cboxAppointmentType.Enabled = true;
            dtpAppointmentDate.Enabled = true;
            dtpAppointmentTimeStart.Enabled = true;
            dtpAppointmentTimeEnd.Enabled = true;

            //enable labels
            lblAppointmentConsultant.Enabled = true;
            lblAppointmentCustomer.Enabled = true;
            lblAppointmentDate.Enabled = true;
            lblAppointmentEnd.Enabled = true;
            lblAppointmentStart.Enabled = true;
            lblAppointmentType.Enabled = true;

        }

        private void dgvAppointment_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            AppointmentEditDeleteSectionControl(Selection);
        }


        //customer tab events
        private void btnNewCustomer_Click(object sender, EventArgs e)
        {
            customerTabState = New;
            CustomerTabControl(Enable, Disable);
        }

        private void btnUpdateCustomer_Click(object sender, EventArgs e)
        {
            //load the selected customer into memory
            DataRow row = ((DataRowView)dgvCustomerList.SelectedRows[0].DataBoundItem).Row;
            customerIdToEdit = (int)row["customerId"];

            //fill the text fields
            txtCustomerAddress.Text = row["Address"].ToString();
            txtCustomerCity.Text = row["City"].ToString();
            txtCustomerCountry.Text = row["Country"].ToString();
            txtCustomerName.Text = row["Customer"].ToString();
            txtCustomerPhone.Text = row["Phone"].ToString();
            txtCustomerPostalCode.Text = row["Postal_Code"].ToString();

            //change tab state
            customerTabState = Updating;
            CustomerTabControl(Enable, Disable);
        }

        private void btnDeleteCustomer_Click(object sender, EventArgs e)
        {
            //load the selected customer into memory
            DataRow row = ((DataRowView)dgvCustomerList.SelectedRows[0].DataBoundItem).Row;
            int customerIdToDelete = (int)row["customerId"];

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete?", "Customer Delete Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                //delete any associated appointments with the customer
                string sqlstr = $"DELETE FROM appointment WHERE customerId = {customerIdToDelete}";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                //delete customer record from database
                sqlstr = $"DELETE FROM customer WHERE customerId = {customerIdToDelete}";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }


                fillCustomerDataTable();
                customerTabState = Neutral;
                CustomerTabControl(Disable, Enable);

            }
           
        }

        private void btnSaveCustomer_Click(object sender, EventArgs e)
        {
            string customerName = txtCustomerName.Text;
            string addressName = txtCustomerAddress.Text;
            string cityName = txtCustomerCity.Text;
            string countryName = txtCustomerCountry.Text;
            string postalCode = txtCustomerPostalCode.Text;
            string phoneNumber = txtCustomerPhone.Text;
            int countryId;
            int cityId;
            int addressId;
            string sqlstr = "";
            string now = DateTime.UtcNow.ToString(dbDateTimeFormat);

            bool errorFound = CustomerSaveErrorCheck();

            if (errorFound)
            {
                MessageBox.Show("You must fill out all fields.", "Error Saving Customer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                //WARNING! SQL INJECTION SECURITY RISK!

                countryId = GetOrCreateCountryId(countryName);
                cityId = GetOrCreateCityId(cityName, countryId);
                addressId = GetOrCreateAddressId(addressName, cityId, postalCode, phoneNumber);

                if (customerTabState == New)
                {
                    sqlstr = $"INSERT INTO customer (customerName, addressId, active, createDate, createdBy, lastUpdate, lastUpdateBy ) " +
                    $"VALUES ( '{customerName}', {addressId}, 1, '{now}' , '{loggedInUserName}', '{now}', '{loggedInUserName}')";

                }
                else if (customerTabState == Updating)
                {
                    sqlstr = $"UPDATE customer SET customerName = '{customerName}', addressId = {addressId}, lastUpdate = '{now}', lastUpdateBy = '{loggedInUserName}' " +
                        $"WHERE customerId = {customerIdToEdit}";
                }

                bool exceptionFound = false;
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sqlstr, DBConnection.conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    exceptionFound = true;
                    MessageBox.Show(ex.ToString());
                }

                if (!exceptionFound)
                {
                    fillCustomerDataTable();
                    CustomerTabControl(Disable, Enable);                 

                    if (customerTabState == New)
                    {
                        MessageBox.Show("Customer added.");
                    }
                    else if(customerTabState == Updating)
                    {
                        MessageBox.Show("Customer updated.");
                    }

                    customerTabState = Neutral;

                }

            }
          
            
        }

        private void btnCancelCustomer_Click(object sender, EventArgs e)
        {
            CustomerTabControl(Disable, Enable);
            customerTabState = Neutral;
        }

        private void dgvCustomer_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            CustomerTabControl(Disable, Selection);
            customerTabState = Updating;
        }


    }
}

