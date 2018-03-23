using FastColoredTextBoxNS;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Test.Client
{
    public partial class MainForm : Form
    {
        Thread LinkThread;

        public MainForm()
        {
            InitializeComponent();
            InitCredentials();
        }

        void InitCredentials()
        {
            tbUserName.Text = Properties.Settings.Default.UserName;
            tbPassword.Text = SecurityUtilities.DecryptString(Properties.Settings.Default.Password);   
        }

        public void ReEnableStartClearButton()
        {
            btnStart.Enabled = true;
            btnClear.Enabled = true;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                //Kliknutím na tlačdilo 'Start', prebehne validácia vstupov 

                //V prípade nevalidných nastavení vráti null
                Settings settings = GetSettings();

                if (settings != null)
                {
                    btnStart.Enabled = false;
                    btnClear.Enabled = false;
                    progressBar1.Value = 0;

                    //Vytvorenie novej inštancie LinkGrabber
                    LinkGrabber Link = new LinkGrabber(settings);
                    LinkThread = new Thread(() => Link.Start());
                    LinkThread.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception Occured!" + Environment.NewLine + Environment.NewLine + "Details: " + Environment.NewLine + ex.Message);
            }
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            try
            {
                LinkThread.Abort();
                MessageBox.Show("Test aborted!");
                ReEnableStartClearButton();
                label3.Text = "Test Aborted!";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error test aborting " + Environment.NewLine + "Details: " + ex.Message);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                tbProgres.Clear();                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception Occured!" + Environment.NewLine + Environment.NewLine + "Details: " + Environment.NewLine + ex.Message);
            }
        }

        long fileCounter = 0;
        private Settings GetSettings()
        {
            //Meno a heslo - pamatat si
            Properties.Settings.Default.UserName = tbUserName.Text;
            Properties.Settings.Default.Password = SecurityUtilities.EncryptString(tbPassword.Text);
            Properties.Settings.Default.Save();

            //Kontrola ak vložená URL adresa je validna
            if (!Utilities.IsUrlValid(tbURL.Text))
            {
                UpdateStatusText("Invalid URL! Make sure you start with http://");
                return null;
            }

            string ReportDestinationFolder = Utilities.GetFolderSelection();
            if (ReportDestinationFolder == string.Empty || !System.IO.Directory.Exists(ReportDestinationFolder))
            {
                //Chyba neplatného adresára
                UpdateStatusText("Invalid Directory! Please try again and select a correct directory.");
                return null;
            }

            string fname = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_TestResult" + (fileCounter++);

            Settings Settings = new Settings(this);
            Settings.UserName = tbUserName.Text;
            Settings.Password = tbPassword.Text;
            Settings.Depth = Convert.ToInt16(tbDepth.Text);
            Settings.URL = tbURL.Text;
            Settings.ReportId = @"\TestResult.txt";
            Settings.ReportDestinationFolder = ReportDestinationFolder + @"\Report\" + fname;

            //Výstup do MainForm
            tbSavePath.Text = Settings.ReportDestinationFolder + Settings.ReportId;

            return Settings;
        }

        private void UpdateStatusText(string msg)
        {
            tbProgres.Text += msg + Environment.NewLine;
            tbProgres.SelectionStart = tbProgres.Text.Length;
            tbProgres.ScrollToCaret();
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void tbProgress_TextChanged(object sender, EventArgs e)
        {
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }
    }
}
