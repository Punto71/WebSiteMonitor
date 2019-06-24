using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WebSiteMonitor.Service.Models;
using WebSiteMonitor.Service.Support;

namespace WebSiteMonitor.Service {
    public class PingWorker {

        const int PING_TIMEOUT = 3000;
        const int MS_IN_SECOND = 1000;


        private static PingWorker _instance;

        public static PingWorker Instance {
            get {
                if (_instance == null) {
                    _instance = new PingWorker();
                }
                return _instance;
            }
        }

        ConcurrentDictionary<int, BackgroundWorker> _workers;

        public PingWorker() {
            _workers = new ConcurrentDictionary<int, BackgroundWorker>();
        }

        public bool Contains(WebSiteItem item) {
            return _workers.ContainsKey(item.Id);
        }

        public void AddWebSite(WebSiteItem item) {
            if (item.PingInterval > 0) {
                var worker = new BackgroundWorker();
                worker.DoWork += WorkerDoWork;
                worker.ProgressChanged += WorkerProgressChanged;
                worker.RunWorkerCompleted += RunWorkerCompleted;
                worker.WorkerSupportsCancellation = true;
                worker.WorkerReportsProgress = true;
                if (_workers.TryAdd(item.Id, worker)) {
                    worker.RunWorkerAsync(item);
                } else {
                    Logger.AddError("Can't init ping worker with web site " + item.Name);
                }
            } else {
                PingResult.SavePingResultState(item.Id, PingResult.WebSiteState.notChecked);
            }
        }

        void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                Logger.AddError(e.Error.Message);
            }
        }

        public void ReinitWorker(WebSiteItem item) {
            if (_workers.ContainsKey(item.Id)) {
                var worker = _workers[item.Id];
                worker.CancelAsync();
                if (_workers.TryRemove(item.Id, out worker)) {
                    if (item.PingInterval > 0)
                        AddWebSite(item);
                    else
                        PingResult.SavePingResultState(item.Id, PingResult.WebSiteState.notChecked);
                } else {
                    Logger.AddError("Can't reinit ping worker with web site " + item.Name);
                }
            }
        }

        void WorkerDoWork(object sender, DoWorkEventArgs e) {
            var worker = sender as BackgroundWorker;
            var data = e.Argument as WebSiteItem;
            if (worker != null && data != null) {
                while (worker.CancellationPending == false) {
                    var ping = new Ping();
                    try {
                        var result = ping.Send(data.Url, PING_TIMEOUT);
                        if (worker.CancellationPending)
                            continue;
                        if (result.Status == IPStatus.Success) {
                            worker.ReportProgress((int)result.RoundtripTime, data);
                        } else {
                            worker.ReportProgress(0, data);
                        }
                    } catch (PingException ex) {
                        if (ex.InnerException != null) {
                            throw new Exception(ex.Message + " " + ex.InnerException.Message + " " + data.Url);
                        }
                    }
                    if (worker.CancellationPending)
                        continue;
                    SupportUtils.DoEvents();
                    Thread.Sleep(data.PingInterval * MS_IN_SECOND);
                }
                e.Result = null;
            }
        }

        void WorkerProgressChanged(object sender, ProgressChangedEventArgs e) {
            var data = e.UserState as WebSiteItem;
            if (data != null) {
                PingResult.SavePingResult(data.Id, e.ProgressPercentage);
            }
        }

        public void StopAll() {
            foreach (var worker in _workers.Values) {
                worker.CancelAsync();
            }
            while (_workers.Values.Any(t => t.IsBusy)) {
                SupportUtils.DoEvents();
            }
        }

        public void StopWorker(int siteId) {
            if (_workers.ContainsKey(siteId)) {
                var worker = _workers[siteId];
                worker.CancelAsync();
                while (worker.IsBusy) {
                    SupportUtils.DoEvents();
                }
            }
        }
    }
}
