using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ImpactElectronicInvoicing.Modules;

namespace ImpactElectronicInvoicing
{
    public partial class Service1 : ServiceBase
    {
        private MyDataTasks oTask = new MyDataTasks();

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            if (oTask == null)
                oTask = new MyDataTasks();

            oTask.Start();
        }

        protected override void OnStop()
        {
            if (oTask != null)
                oTask.Stop();
        }
    }
}
