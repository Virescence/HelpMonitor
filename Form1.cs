using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using System.Windows;
using System.Globalization;
using System.Timers;


namespace Client_SCM_2
{

    public partial class Form1 : Form
    {
        public SortableBindingList<Record> help_list = new SortableBindingList<Record> { };
        public List<Record> record_list = new List<Record> { };
        public List<string> user_list = new List<string> { };
        public Dictionary<string, string> user_dictionary = new Dictionary<string, string> { };
        public static DataGridViewCellStyle style = new DataGridViewCellStyle();
        public bool global_strobe = false;
        public TimeSpan yellowThreshold = TimeSpan.FromMinutes(1);
        public TimeSpan redThreshold = TimeSpan.FromMinutes(2);
        public Point MouseDownLocation;
        public static bool clickCoolDown = false;
        public static System.Timers.Timer coolDownTimer;

        public Form1()
        {
            
            InitializeComponent();
            dataGridView.DataSource = help_list;
            dataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.cell_formatting);
            dataGridView.SelectionChanged += new EventHandler(selectionChanged);
            dataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(cellSelected);


            this.Controls.Add(dataGridView);
            //this.dataGridView.Columns["Station"].Visible = false;
            this.dataGridView.RowHeadersVisible = false;
            //this.dataGridView.Sort(this.dataGridView.Columns["UserName"], ListSortDirection.Descending);
            this.dataGridView.Columns["NeedsHelp"].Width = 0;
            this.dataGridView.Columns["NeedsHelp"].DisplayIndex = 0;
            this.dataGridView.Columns["DisplayTime"].DisplayIndex = 2;
            this.dataGridView.Columns["APInteracted"].DisplayIndex = 1;
            this.dataGridView.Columns["APInteracted"].HeaderText = "AP Assisting?";
            this.dataGridView.Columns["UserName"].HeaderText = "TSR";
            this.dataGridView.Columns["DisplayTime"].HeaderText = "Timer";
            this.dataGridView.Columns["LocCode"].HeaderText = "Location";
            this.dataGridView.Columns["UserName"].HeaderCell.Style.Padding = new Padding(5, 5, 5, 5);
            this.dataGridView.Columns["LocCode"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.dataGridView.Columns["UserName"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //this.dataGridView.Columns["TimerString"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.dataGridView.Columns["UserName"].Width = 160;
            this.dataGridView.Columns["LocCode"].Width = 125;
            this.dataGridView.Columns["DisplayTime"].Width = 100;
            this.dataGridView.Columns["NeedsHelp"].Visible = true;
            //this.dataGridView.Columns["APInteracted"].Visible = false;
            this.dataGridView.Columns["StartTime"].Visible = false;
            this.dataGridView.Columns["IPAddress"].Visible = false;
            this.dataGridView.Columns["DisplayIP"].Visible = false;
            this.dataGridView.Columns["APInteracted"].Visible = true;
            this.dataGridView.Columns["APInteractTime"].Visible = false;
            this.dataGridView.Columns["APName"].Visible = false;




            start_timers();   
        }

        void start_timers()
        {
            System.Windows.Forms.Timer clock1 = new System.Windows.Forms.Timer();
            clock1.Interval = 1000;
            clock1.Tick += new EventHandler(userDictHandler);
            clock1.Enabled = true;

            System.Windows.Forms.Timer clock2 = new System.Windows.Forms.Timer();
            clock2.Interval = 1000;
            clock2.Tick += new EventHandler(UpdateHelpList);
            clock2.Enabled = true;
        }

        public void resetCoolDown(Object source, System.Timers.ElapsedEventArgs e)
        {
            clickCoolDown = false;
        }

        void cellSelected(object sender, DataGridViewCellEventArgs e)
        {
            if (!clickCoolDown)
            {
                clickCoolDown = true;
                coolDownTimer = new System.Timers.Timer(1000);
                coolDownTimer.Elapsed += resetCoolDown;
                coolDownTimer.AutoReset = false;
                coolDownTimer.Enabled = true;

                try
                {

                    var rowClicked = dataGridView.Rows[e.RowIndex];
                    var record = rowClicked.DataBoundItem as Record;
                    if (record.APInteracted)
                    {
                        RemoveRecord(record);
                        RemoveHelpRecord(record);
                        new Thread(() =>
                        {
                            Thread.CurrentThread.IsBackground = true;
                            SCMClient.sendCancel(record);
                        }).Start();
                        
                    }
                    else
                    {
                        record.APInteracted = true;
                        record.APName = Environment.UserName.ToUpper();
                        record.APInteractTime = DateTime.Now;
                        new Thread(() =>
                        {
                            Thread.CurrentThread.IsBackground = true;
                            SCMClient.sendAPI(record);
                        }).Start();


                    }

                }
                catch
                {

                }
            }
        }

        void selectionChanged(object sender, EventArgs e)
        {
            dataGridView.CurrentCell.Selected = false;
            dataGridView.ClearSelection();
        }

        public void userDictHandler(object sender, EventArgs e)
        {
            if (user_dictionary.Count > 0)
            {
                user_dictionary.Clear();
            }
            foreach (Record r in record_list)
            {
                user_dictionary.Add(r.UserName, r.LocCode);
            }
            SCMClient.sendDict(user_dictionary);
        }

        public void userDictHandler()
        {
            if (user_dictionary.Count > 0)
            {
                user_dictionary.Clear();
            }
            foreach (Record r in record_list)
            {
                user_dictionary.Add(r.UserName, r.LocCode);
            }
            SCMClient.sendDict(user_dictionary);
        }

        public void cell_formatting(object sender, System.Windows.Forms.DataGridViewCellFormattingEventArgs e)
        {
            lock (SCMClient._help_list_sync)
            {
                try
                {
                    dataGridView.ClearSelection();
                    foreach (DataGridViewRow row in dataGridView.Rows)
                    {

                        var r = row.DataBoundItem as Record;
                        if (r.APInteracted)
                        {
                            row.DefaultCellStyle.BackColor = Color.Green;
                            row.DefaultCellStyle.ForeColor = Color.White;
                        }

                    }

                    if (dataGridView.Columns[e.ColumnIndex].Name.Equals("NeedsHelp"))
                    {
                        if (e.Value.Equals(false))
                        {

                            CurrencyManager cm = (CurrencyManager)dataGridView.BindingContext[dataGridView.DataSource];


                            this.BeginInvoke(new MethodInvoker(() =>
                            {
                                cm.SuspendBinding();
                                dataGridView.Rows[e.RowIndex].Visible = false;
                                //RemoveRecord(dataGridView.Rows[e.RowIndex].DataBoundItem as Record);
                                cm.ResumeBinding();
                            }));


                        }
                        else
                        {
                            CurrencyManager cm = (CurrencyManager)dataGridView.BindingContext[dataGridView.DataSource];


                            this.BeginInvoke(new MethodInvoker(() =>
                            {
                                cm.SuspendBinding();
                                dataGridView.Rows[e.RowIndex].Visible = true;
                                //RemoveRecord(dataGridView.Rows[e.RowIndex].DataBoundItem as Record);
                                cm.ResumeBinding();
                            }));

                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Cell Formatting ERROR");
                }
            }
        }

        public void AddRecord(Record r)
        {
            
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => this.AddRecord(r)));
            }
            else
            {
                if (!record_list.Any(record => record.UserName == r.UserName))
                {
                    lock (SCMClient._record_list_sync)
                    {
                        record_list.Add(r);
                    }
                }
                else
                {
                    var record = record_list.FirstOrDefault(x => x.UserName == r.UserName);
                    lock (SCMClient._record_list_sync)
                    {
                        if (help_list.Contains(record))
                        {
                            RemoveHelpRecord(record);
                        }
                        record_list.Remove(record);
                        record = null;
                        record_list.Add(r);
                    }

                }
            
            }
        }

        public void AddHelpRecord(Record r)
        {

            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => this.AddHelpRecord(r)));
            }
            else
            {
                lock (SCMClient._help_list_sync)
                {
                    if (!help_list.Any(record => record.UserName == r.UserName))
                    {
                        r.StartTime = DateTime.Now;
                        r.DisplayTime = "00:00:00";
                        help_list.Add(r);
                    }
                }
            }
        }

        public void RemoveRecord(Record r)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => this.RemoveRecord(r)));
            }
            else
            {
                lock (SCMClient._record_list_sync)
                {                
                    record_list.Remove(r);
                }

            }
        }

        public void RemoveHelpRecord(Record r)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => this.RemoveHelpRecord(r)));
            }
            else
            {
                lock (SCMClient._help_list_sync)
                {
                    help_list.Remove(r);
                    help_list.ResetBindings();
                }
            }
        }

        public void UpdateGrid()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => this.UpdateGrid()));
            }
            else
            {
                lock (SCMClient._help_list_sync)
                {
                    help_list.ResetBindings();
                    this.dataGridView.Sort(this.dataGridView.Columns["DisplayTime"], ListSortDirection.Descending);
                }
            }            
        }

        public void UpdateHelpList(object sender, EventArgs e)
        {
            lock (SCMClient._record_list_sync)
            {
                lock (SCMClient._help_list_sync)
                {
                    try
                    {
                        foreach (Record r in record_list)
                        {
                            r.DisplayTime = (DateTime.Now - r.StartTime).ToString(@"hh\:mm\:ss");
                            if (r.NeedsHelp && !help_list.Contains(r))
                            {
                                AddHelpRecord(r);
                            }
                            else if (!r.NeedsHelp && help_list.Contains(r))
                            {
                                RemoveHelpRecord(r);
                                RemoveRecord(r);
                            }
                        }
                        UpdateGrid();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        public void UpdateHelpList()
        {
            lock (SCMClient._record_list_sync)
            {
                lock (SCMClient._help_list_sync)
                {
                    try
                    {
                        foreach (Record r in record_list)
                        {
                            r.DisplayTime = (DateTime.Now - r.StartTime).ToString(@"hh\:mm\:ss");
                            if (r.NeedsHelp && !help_list.Contains(r))
                            {
                                AddHelpRecord(r);
                            }
                            else if (!r.NeedsHelp && help_list.Contains(r))
                            {
                                RemoveHelpRecord(r);
                                RemoveRecord(r);
                            }
                        }
                        UpdateGrid();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        private void Form_Load(object sender, EventArgs e)
        {
            this.dataGridView.DefaultCellStyle.SelectionBackColor = this.dataGridView.DefaultCellStyle.BackColor;
            this.dataGridView.DefaultCellStyle.SelectionForeColor = this.dataGridView.DefaultCellStyle.ForeColor;
        }

        private void dataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                MouseDownLocation = e.Location;
            }
        }

        private void dataGridView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.Left = e.X + this.Left - MouseDownLocation.X;
                this.Top = e.Y + this.Top - MouseDownLocation.Y;
            }
        }
    }

    public static class SCMClient
    {
        public static Form1 form;
        public static UdpClient listener = new UdpClient(0);
        //public static UdpClient listener;
        public static System.Timers.Timer SCMTimer = new System.Timers.Timer();
        public static bool connected = false;
        public static bool aliveReceived = false;
        public static bool packetAcknowledged = false;
        public static bool APIAcknowledged = false;
        public static IPAddress server_address = IPAddress.Parse("10.168.146.255"); //10.168.146.255 for Ops || 10.168.25.12 for Support-28
        public static IPEndPoint server_ip = new IPEndPoint(server_address, 5684);
        


        public static readonly object _record_list_sync = new object();
        public static readonly object _help_list_sync = new object();

        public static void connect()
        {
            while (!connected)
            {
                try
                {

                    listener = new UdpClient(0);                    
                    listener.Connect(server_ip);                   
                    byte[] send_data = Encoding.ASCII.GetBytes("LIST");
                    listener.Send(send_data, send_data.Length);
                    Thread.Sleep(1000);
                    if (listener.Available > 0)
                    {
                        byte[] received_bytes = listener.Receive(ref server_ip);
                        string received_data = Encoding.ASCII.GetString(received_bytes);
                        connected = true;

                        aliveReceived = true;

                        listen();                    
                    }
                }

                catch
                {
                    connected = false;
                    connect();
                }
                Thread.Sleep(3000);
            }
        }

        public static void listen()
        {
            form.userDictHandler();

            while (connected)
            {
                if (listener.Available > 0)
                {
                    byte[] received_bytes = listener.Receive(ref server_ip);
                    connected = true;
                    string received_data = Encoding.ASCII.GetString(received_bytes);

                    if (received_data.Substring(0,3) == "API")
                    {
                        try
                        {
                            string userName = received_data.Substring(3);
                            form.record_list.Where(x => x.UserName == userName).First().APInteracted = true;
                        }
                        catch
                        {
                        }

                    }

                    if (received_data == "ACK")
                    {

                        packetAcknowledged = true;
                    }

                    if (received_data == "ACKAPI")
                    {

                        APIAcknowledged = true;
                    }

                    if (received_data == "ALIVE")
                    {
                        listener.Send(received_bytes, received_bytes.Length);
                        aliveReceived = true;

                    }

                    if (received_data == "SCMALIVE")
                    {
                        aliveReceived = true;
                    }

                    if (received_data.Substring(0, 2) == "HB")
                    {

                        var simple_dict = JsonConvert.DeserializeObject<Dictionary<string, bool>>(received_data.Substring(2));

                        updateNeedsHelp(simple_dict);
                    }

                    if (received_data.Length > 50)
                    {
                        Record record = (Record)JsonConvert.DeserializeObject<Record>(received_data);
                        form.AddRecord(record);

                    }
                }

                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public static void sendCancel(Record record)
        {
            string serializedRecord = "HELPO" + JsonConvert.SerializeObject(record);
            byte[] data = Encoding.ASCII.GetBytes(serializedRecord);
            packetAcknowledged = false;
            while (!packetAcknowledged)
            {
                try
                {
                    listener.Send(data, data.Length);
                }
                catch
                {

                }
                Thread.Sleep(200);
            }
        }

        public static void sendAPI(Record record)
        {
            byte[] data = Encoding.ASCII.GetBytes("API" + record.UserName);
            APIAcknowledged = false;
            while (!APIAcknowledged)
            {
                try
                {
                    listener.Send(data, data.Length);
                }
                catch
                {

                }
                Thread.Sleep(200);
            }
        }

        public static void sendDict(Dictionary<string, string> d)
        {
            try
            {
                byte[] send_data = Encoding.ASCII.GetBytes("LIST" + JsonConvert.SerializeObject(d));
                listener.Send(send_data, send_data.Length);
            }
            catch
            {
                Console.WriteLine("KHOVOSTOV");
            }

        }

        public static void checkConnected(object sender, ElapsedEventArgs e)
        {
            byte[] send_data = Encoding.ASCII.GetBytes("SCMALIVE");

            if (!aliveReceived)
            {              
                //listener.Send(send_data, send_data.Length);
                listener.Close();
                connected = false;

                connect();
            }

            else
            {
                listener.Send(send_data, send_data.Length);
                aliveReceived = false;

            }
        }

        public static void setform(Form1 f)
        {
            SCMClient.form = f;
        }

        public static string getTempPath()
        {
            string path = System.Environment.GetEnvironmentVariable("TEMP");
            if (!path.EndsWith("\\")) path += "\\";
            return path;
        }

        public static void updateNeedsHelp(Dictionary<string, bool> simple_dict)
        {
            lock (_record_list_sync)
            {
                foreach(Record r in form.record_list)
                {
                    bool helpneeded;
                    if(simple_dict.TryGetValue(r.UserName, out helpneeded))
                    {
                        r.NeedsHelp = helpneeded;
                    }
                }
            }
        }

        public static void logMessage(string msg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(getTempPath() + "SCMLOG.txt");
            try
            {
                string logMsg = System.String.Format("{0:G}: {1}.", System.DateTime.Now, msg);
                sw.WriteLine(logMsg);
            }
            finally
            {
                sw.Close();
            }
        }
    }

    public class Record
    {

 
        public string UserName { get; set; }
        public bool NeedsHelp { get; set; }
        public DateTime StartTime { get; set; }
        public string LocCode { get; set; }
        public IPEndPoint IPAddress { get; set; }
        public string APName { get; set; }
        public DateTime APInteractTime { get; set; }
        public bool APInteracted { get; set; }

        public string DisplayTime { get; set; }
        public string DisplayIP { get; set; }



        public Record(string UserName, bool NeedsHelp, DateTime StartTime, string LocCode, IPEndPoint IPAddress)
        {
            this.NeedsHelp = NeedsHelp;
            this.UserName = UserName;            
            this.StartTime = DateTime.Now;
            this.LocCode = LocCode;
            this.IPAddress = IPAddress;
            this.APName = "";
            this.APInteractTime = DateTime.MinValue;
            this.DisplayTime = "00:00:00";
            this.APInteracted = false;

        }
    }

    public class SortableBindingList<T> : BindingList<T> //BindingList<T> alone cannot be "sortable", this is a custom class taken from: http://stackoverflow.com/questions/23661195/datagridview-using-sortablebindinglist
    {
        private bool isSortedValue;
        ListSortDirection sortDirectionValue;
        PropertyDescriptor sortPropertyValue;

        public SortableBindingList()
        {
        }

        public SortableBindingList(IList<T> list)
        {
            foreach (object o in list)
            {
                this.Add((T)o);
            }
        }



        protected override void ApplySortCore(PropertyDescriptor prop,
            ListSortDirection direction)
        {
            Type interfaceType = prop.PropertyType.GetInterface("IComparable");

            if (interfaceType == null && prop.PropertyType.IsValueType)
            {
                Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);

                if (underlyingType != null)
                {
                    interfaceType = underlyingType.GetInterface("IComparable");
                }
            }

            if (interfaceType != null)
            {
                sortPropertyValue = prop;
                sortDirectionValue = direction;

                IEnumerable<T> query = base.Items;

                if (direction == ListSortDirection.Ascending)
                {
                    query = query.OrderBy(i => prop.GetValue(i));
                }
                else
                {
                    query = query.OrderByDescending(i => prop.GetValue(i));
                }

                int newIndex = 0;
                foreach (object item in query)
                {
                    this.Items[newIndex] = (T)item;
                    newIndex++;
                }

                isSortedValue = true;
                this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }
            else
            {
                throw new NotSupportedException("Cannot sort by " + prop.Name +
                    ". This" + prop.PropertyType.ToString() +
                    " does not implement IComparable");
            }
        }

        protected override PropertyDescriptor SortPropertyCore
        {
            get { return sortPropertyValue; }
        }

        protected override ListSortDirection SortDirectionCore
        {
            get { return sortDirectionValue; }
        }

        protected override bool SupportsSortingCore
        {
            get { return true; }
        }

        protected override bool IsSortedCore
        {
            get { return isSortedValue; }
        }
    }
}
