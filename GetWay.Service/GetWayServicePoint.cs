using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace GetWay.Service
{
    partial class GetWayServicePoint : ServiceBase
    {
        public GetWayServicePoint()
        {
            InitializeComponent();
            this.EventLog.Source = "Gateway_log";
            this.EventLog.Log = "Application";
        }
       
        protected override void OnStart(string[] args)
        {

            if (!EventLog.SourceExists(this.EventLog.Source))
            {
                EventLog.CreateEventSource(this.EventLog.Source, this.EventLog.Log);
            }

            ListenerATC.EscribirMensaje = EscribirLog;
            Task.Factory.StartNew(
                () => { ListenerATC.IniciarListenerINAC(); }
                );
                        
            this.EventLog.WriteEntry("INAC activo", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            this.EventLog.WriteEntry("GateWay Iniciado", EventLogEntryType.Information);
        }

        private void EscribirLog(String mensaje)
        {

#if DEBUG
            this.EventLog.WriteEntry(mensaje, EventLogEntryType.Information);
#endif
        }
    }
}
