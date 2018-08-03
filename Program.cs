using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Client_SCM_2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //SCMClient client = new SCMClient();
            SCMClient.SCMTimer.Elapsed += SCMClient.checkConnected;
            SCMClient.SCMTimer.Interval = 60000;
            SCMClient.SCMTimer.Enabled = true;


            Thread thread = new Thread(SCMClient.connect);
            thread.IsBackground = true;
            thread.Start();


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 form = new Form1();
            SCMClient.setform(form);
            Application.Run(form);


        }
    }
}
