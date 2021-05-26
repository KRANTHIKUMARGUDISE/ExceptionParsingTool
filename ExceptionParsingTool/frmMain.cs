using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ExceptionParsingTool
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        #region Generate Report
        //Algo.
        //Perform initial validation to before parsing
        //Check Server Connectivity
        //Append internal Path
        //Connect to folder
        //Fetch Error directories
        //Loop each Error Directory
        //Get & Parse xml files
        //Prepare report
        private void btnGenerateReport_Click(object sender, EventArgs e)
        {
            try
            {
                #region Set to Start Process
                btnGenerateReport.Enabled = false;
                this.Cursor = Cursors.WaitCursor;
                progressBar1.Value = 0;
                lblStatus.Visible = false;
                txtLog.Clear();
                #endregion

                //...Perform initial validation to before parsing
                if (!PerformInititalValidations()) return;

                #region Source Dir verification
                string fullPath = txtServerName.Text.Trim();

                //Check Sever Connectivity
                //if (!CheckDirectory(fullPath)) return;

                //Get internal path from config
                string internalPath = ConfigurationManager.AppSettings.Get("InternalPath");

                //Append internal Path
                if (!string.IsNullOrEmpty(internalPath))
                {
                    fullPath = fullPath + internalPath;
                    //Check directory Connectivity
                    if (!CheckDirectory(fullPath)) return;
                }
                #endregion

                txtLog.AppendText("Server Full Path: " + fullPath);
                //Get Client directories
                DirectoryInfo mainDir = new DirectoryInfo(fullPath);
                IEnumerable<DirectoryInfo> clientDirectories = mainDir.GetDirectories()
                .Where(dr => dtpFrom.Value.Date <= dr.LastWriteTime.Date && dr.LastWriteTime.Date <= dtpTo.Value.Date).Select(dr => dr);

                //string[] clientDirectories = Directory.GetDirectories(fullPath);

                //Check directories existing or not
                if (clientDirectories == null && clientDirectories.Count() <= 0)
                {
                    ShowMessage("No Exception directories exists.");
                    return;
                }

                progressBar1.Maximum = clientDirectories.Count();
                progressBar1.Value = 0;

                #region Output Dir
                string outputDir = ConfigurationManager.AppSettings.Get("DestinationFolderPath");
                if (string.IsNullOrEmpty(outputDir))
                {
                    ShowMessage("Destination Folder Path not configured.");
                    return;
                }
                else if (!Directory.Exists(outputDir))
                {
                    ShowMessage("Destination Folder not available.");
                    return;
                }
                outputDir += "\\" + DateTime.Now.ToString("MM-dd-yyyy hh-mm-ss");
                Directory.CreateDirectory(outputDir);
                #endregion

                #region Create Detailed.csv file
                string detailedFile = outputDir + "\\Detailed.csv";
                //create and add headers
                using (StreamWriter sw = File.CreateText(detailedFile))
                {
                    sw.WriteLine("ClientShortName,VIN,AccountNumber,DFPROJ,FormName,BusinessProcessDomain,ServerName,DataFile,DateTime,Exception,OutputFolderPath,OutputFileName");
                }
                #endregion

                #region Variables Declaration
                string clientShortName = "", VIN = "", AccountNumber = "", FormName = "", BusinessProcessDomain = "", OutputFolderPath = "", OutputFileName = "";
                int formIndex = 0;
                XmlDocument xmlFile, xmlErrFile;
                XmlNodeList exceptionReportEleList, dataEleList;
                List<string> summaryData = new List<string>();
                StringBuilder strDetailed = null;
                string[] xmlFiles = null;
                bool flag = false;
                #endregion

                txtLog.AppendText("\nCount of Client Directories: " + clientDirectories.Count());
                foreach (DirectoryInfo clientDir in clientDirectories)
                {
                    progressBar1.Value += 1;
                    txtLog.AppendText("\nSTART - Client Directory: " + clientDir.FullName);

                    ////Check client dir last modified date
                    //if (!CheckDirectoryDate(clientDir)) continue;

                    #region Rest variables
                    strDetailed = new StringBuilder();
                    flag = false;
                    #endregion
                    DirectoryInfo dirs = new DirectoryInfo(clientDir.FullName);
                    IEnumerable<DirectoryInfo> logDires = dirs.GetDirectories()                    .Where(dr => dtpFrom.Value.Date <= dr.LastWriteTime.Date && dr.LastWriteTime.Date <= dtpTo.Value.Date).Select(dr => dr);

                    if (logDires.Count() == 0) continue;
                    txtLog.AppendText("\nCount of Exception Directories: " + logDires.Count());

                    foreach (DirectoryInfo logDir in logDires)//Directory.GetDirectories(clientDir))
                    {
                        txtLog.AppendText("\nSTART - Exception Directory: " + logDir.FullName);

                        //Check exception dir last modified date
                        //if (!CheckDirectoryDate(logDir.FullName)) continue;

                        //Get xml files
                        xmlFiles = Directory.GetFiles(logDir.FullName);

                        #region Parse
                        if (xmlFiles != null && xmlFiles.Length == 2)
                        {

                            #region Load xmls
                            xmlFile = new XmlDocument();
                            xmlFile.Load(xmlFiles.Where(file => !file.EndsWith(".err.xml")).First());

                            xmlErrFile = new XmlDocument();
                            xmlErrFile.Load(xmlFiles.Where(file => file.EndsWith(".err.xml")).First());
                            #endregion

                            #region err.xml Elements
                            exceptionReportEleList = xmlErrFile.GetElementsByTagName("ExceptionReport");
                            string dateTime = "", DataFile = "", Exception = "";
                            dateTime = exceptionReportEleList[0]["DateTime"].InnerText;
                            DataFile = exceptionReportEleList[0]["DataFile"].InnerText;
                            Exception = exceptionReportEleList[0]["Exception"].InnerText;
                            #endregion

                            #region Reset Variables
                            clientShortName = VIN = AccountNumber = FormName = BusinessProcessDomain = OutputFolderPath = OutputFileName = "";
                            formIndex = 0;
                            #endregion

                            #region Get Args Elements
                            dataEleList = xmlFile.GetElementsByTagName("Args");
                            if (dataEleList != null && dataEleList.Count > 0)
                            {
                                OutputFolderPath = dataEleList[0]["OutputFolderPath"].InnerText;
                                OutputFileName = dataEleList[0]["OutputFileName"].InnerText;
                            }
                            #endregion

                            #region Data Elements

                            dataEleList = xmlFile.GetElementsByTagName("Data");

                            if (dataEleList != null && dataEleList.Count > 0)
                            {
                                txtLog.AppendText("\nCount of Data elements: " + dataEleList.Count);

                                foreach (XmlNode dataEle in dataEleList)
                                {
                                    formIndex++;
                                    txtLog.AppendText("\nSTART - Parsing Data Element - " + formIndex);
                                    #region data
                                    clientShortName = dataEle["ClientShortName"].InnerText;
                                    VIN = dataEle["VIN"].InnerText;
                                    AccountNumber = dataEle["AccountNumber"].InnerText;

                                    FormName = dataEle["Form" + formIndex + "Name"].InnerText;
                                    BusinessProcessDomain = dataEle["BusinessProcessDomain"].InnerText;
                                    #endregion

                                    #region Build detailed report row
                                    strDetailed.Append(clientShortName);
                                    strDetailed.Append(",");
                                    strDetailed.Append(VIN);
                                    strDetailed.Append(",");
                                    strDetailed.AppendFormat("=\"{0}\"", AccountNumber);
                                    strDetailed.Append(",");
                                    strDetailed.Append(clientDir);//DFPROJ
                                    strDetailed.Append(",");
                                    strDetailed.Append(FormName);
                                    strDetailed.Append(",");
                                    strDetailed.Append(BusinessProcessDomain);
                                    strDetailed.Append(",");
                                    strDetailed.Append(txtServerName.Text);
                                    strDetailed.Append(",");
                                    strDetailed.Append(DataFile);
                                    strDetailed.Append(",");
                                    strDetailed.AppendFormat("=\"{0}\"", dateTime);
                                    strDetailed.Append(",");
                                    strDetailed.AppendFormat("\"" + Exception + "\"");
                                    strDetailed.Append(",");
                                    strDetailed.Append(OutputFolderPath);
                                    strDetailed.Append(",");
                                    strDetailed.Append(OutputFileName);
                                    strDetailed.AppendLine("");
                                    #endregion
                                    txtLog.AppendText("\nEND - Parsing Data Element - " + formIndex);

                                }
                                flag = true;
                                summaryData.Add(clientShortName);
                            }
                            #endregion

                        }
                        #endregion
                        txtLog.AppendText("\nEND - Exception Directory: " + logDir.FullName);

                    }

                    //write to detailed report file
                    if (flag) File.AppendAllText(detailedFile, strDetailed.ToString());

                    txtLog.AppendText("\nEND - Client Directory: " + clientDir.FullName);
                }

                #region Summary Report
                txtLog.AppendText("\nSTART - Summary Report");
                string summaryFile = outputDir + "\\Summary.csv";

                using (StreamWriter sw = File.CreateText(summaryFile))
                {
                    sw.WriteLine("ClientShortName,Count");
                    foreach (var line in summaryData.GroupBy(x => x)
                        .Select(group => new
                        {
                            ClientShortName = group.Key,
                            Count = group.Count()
                        }))
                        sw.WriteLine("{0},{1}", line.ClientShortName, line.Count);
                }
                txtLog.AppendText("\nEnd - Summary Report");
                #endregion

                lblStatus.Visible = true;

            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                Application.DoEvents();
                this.Cursor = Cursors.Default;
                btnGenerateReport.Enabled = true;
            }
        }
        #endregion

        #region "Validate Directory based on Date filter"
        bool CheckDirectoryDate(string dir)
        {
            DirectoryInfo dr = new DirectoryInfo(dir);
            return (dtpFrom.Value.Date <= dr.LastWriteTime.Date && dr.LastWriteTime.Date <= dtpTo.Value.Date);
        }
        #endregion
        #region Check Directory Connectivity
        bool CheckDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    return true;
                else
                {
                    txtServerName.Focus();
                    ShowMessage("Unable to connect to : " + path + ".");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            return false;
        }
        #endregion

        #region Validations
        bool PerformInititalValidations()
        {
            try
            {
                //check server name existing or not
                if (string.IsNullOrEmpty(txtServerName.Text))
                {
                    ShowMessage("Please enter Server Name.");
                    txtServerName.Focus();
                    return false;
                }
                //check date from selected or not
                if (dtpFrom.Value == null)
                {
                    ShowMessage("Please enter From Date.");
                    dtpFrom.Focus();
                    return false;
                }

                //check date to selected or not
                if (dtpTo.Value == null)
                {
                    ShowMessage("Please enter To Date.");
                    dtpTo.Focus();
                    return false;
                }
                //validate from & to dates
                if (dtpFrom.Value > dtpTo.Value)
                {
                    ShowMessage("From Date must be less than To Date.");
                    dtpFrom.Focus();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return false;
            }
            return true;
        }
        #endregion

        #region Show Exception
        void ShowError(Exception ex)
        {
            MessageBox.Show(ex.Message + "Log: " + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

        #region Show Information
        void ShowMessage(string msg)
        {
            MessageBox.Show(msg, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region OnLoad
        private void frmMain_Load(object sender, EventArgs e)
        {
            DateTime today = DateTime.Now;
            //Default settings
            //Set todate as today by default
            dtpTo.Value = today;
            //Setting 1 month interval
            dtpFrom.Value = today.AddMonths(-1);
            //Not alowing to select Future dates
            dtpTo.MaxDate = today;
            dtpFrom.MaxDate = today;
            string defaultServer = ConfigurationManager.AppSettings.Get("DefaultServer");
            if (!string.IsNullOrEmpty(defaultServer))
                txtServerName.Text = defaultServer;
            progressBar1.Value = progressBar1.Minimum = 0;

        }
        #endregion
    }
}
