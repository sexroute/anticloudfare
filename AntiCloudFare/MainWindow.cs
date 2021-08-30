using CefSharp;
using CefSharp.WinForms;
using EricZhao.UiThread;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AntiCloudFare
{
    public partial class MainWindow : Form
    {

        public MainWindow()
        {
            this.loadSettings();
            InitializeComponent();
            InitializeAsync();
            this.webView21.NavigationCompleted += WebView21_NavigationCompleted;

        }

        IniFile lpIniFile = new IniFile("./settings.ini");
        String Url = "http://ai.bhxz.net:1988/floor_price/?";
        String proxyUrl = "http://ai.bhxz.net:38888";
        public void loadSettings()
        {
            Url = lpIniFile.IniReadStringValue("server", "url", "http://ai.bhxz.net:1988/floor_price/?", true);
            proxyUrl = lpIniFile.IniReadStringValue("proxy", "url", proxyUrl, true);

        }
        async private void WebView21_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            //this.webView21.CoreWebView2.ExecuteScriptAsync($"alert('test')')");
            /* string res = await webView21.ExecuteScriptAsync("document.getElementsByClassName(\"Overflowreact__OverflowContainer-sc-10mm0lu-0 fqMVjm Price--amount\")");
             MessageBox.Show(res);
            String lstrHTML = await webView21.ExecuteScriptAsync("document.documentElement.outerHTML");*/
            String lstrHTML = await webView21.ExecuteScriptAsync("document.documentElement.outerHTML");
            parseContect(lstrHTML);
        }

        void parseContect(String astrDoc)
        {
            try
            {
                string html = astrDoc;
                html = Regex.Unescape(html);
                String lstrKey = "Overflowreact__OverflowContainer-sc-10mm0lu-0 fqMVjm Price--amount";
                int lnIndex = html.IndexOf(lstrKey);
                if (lnIndex > 0)
                {
                    int lnNewStart = lstrKey.Length + lnIndex + 2;
                    int lnNewEnd = html.IndexOf("<span", lnNewStart + 2);
                    String lstrValue = html.Substring(lnNewStart, lnNewEnd - lnNewStart);
                    double ldblValue = 0;
                    if(Double.TryParse(lstrValue,out ldblValue))
                    {
                        lpTaskResults.Add(strCurrentTaskSlug, ldblValue);
                    }
                   // MessageBox.Show(lstrValue + "");
                }
            }
            catch (Exception e)
            {
                ThreadUiController.log(e.Message);
            }
            TaskRunning = 0;
        }

        async void InitializeAsync()
        {
           // await webView21.EnsureCoreWebView2Async(null);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeAsync();
            this.startUpdater();
        }

        private void InvokeUI(Action a)
        {
            this.BeginInvoke(new MethodInvoker(a));
        }

        int taskRunning = 0;
        String strCurrentTaskSlug = "";
        public System.String StrCurrentTaskSlug
        {
            get { lock (this) return strCurrentTaskSlug; }
            set { lock (this) strCurrentTaskSlug = value; }
        }
        Dictionary<String, Double> lpTaskResults = new Dictionary<string, double>();
        public int TaskRunning
        {
            get { lock (this) return taskRunning; }
            set { lock (this) taskRunning = value; }
        }
        async private void ThreadEntry()
        {
           while(true)
            {
                try
                {
                    this.ThreadTask();
                    Thread.Sleep(1*60 * 1000);
                }catch(Exception e)
                {
                    ThreadUiController.log(e.Message);
                }
            }
        }

        async private void ThreadTask()
        {
            try
            {
                //1.get task
                List<String> lpTaskSlugnames = new List<string>();
                using (var client = new WebClient())
                {
                    String lstrData = client.DownloadString(Url + "query");
                    if (null != lstrData)
                    {
                        String[] lpSplits = lstrData.Split(';');
                        for (int i = 0; i < lpSplits.Length; i++)
                        {
                            String lstrTask = lpSplits[i];
                            if (!String.IsNullOrWhiteSpace(lstrTask))
                            {
                                lpTaskSlugnames.Add(lstrTask);
                            }
                        }
                    }
                }

                //1.1 clear result
                lpTaskResults.Clear();

                //2.exec task and wait for task finished
                foreach (String lpTask in lpTaskSlugnames)
                {
                    TaskRunning = 1;
                    InvokeUI(() =>
                    {
                        StrCurrentTaskSlug = lpTask;
                        doLoadAsync(lpTask);
                    });

                    while (true)
                    {
                        int lnValue = 0;
                        lock (this)
                        {
                            lnValue = TaskRunning;
                        }
                        if (lnValue <= 0)
                        {
                            break;
                        }
                        
                        {
                            Thread.Sleep(3000);
                        }
                    }
                }

                //3.post response
                using (var client = new WebClient())
                {
                    StringBuilder loSb = new StringBuilder();

                    foreach (KeyValuePair<String, Double> lpResult in this.lpTaskResults)
                    {
                        loSb.Append(lpResult.Key + ":" + lpResult.Value);
                        loSb.Append(";");
                    }

                    String lstrData = client.DownloadString(Url + "set=" + loSb.ToString());

                }

            }
            catch (Exception e)
            {

            }
        }


        private async Task doLoadAsync(String lstrSlugName)
        {
            CoreWebView2EnvironmentOptions Options = new CoreWebView2EnvironmentOptions();
            Options.AdditionalBrowserArguments = "--proxy-server="+ proxyUrl;
            CoreWebView2Environment env =
            await CoreWebView2Environment.CreateAsync(null, null, Options);
          //  await webView21.EnsureCoreWebView2Async(env);
            this.webView21.Source = new Uri(String.Format("https://opensea.io/collection/{0}?search[sortAscending]=true&search[sortBy]=PRICE&search[toggles][0]=BUY_NOW", lstrSlugName));
        }

        Thread lpThread = null;
        public System.Threading.Thread LpThread
        {
            get { lock(this)return lpThread; }
            set { lock (this) lpThread = value; }
        }
        private void startUpdater()
        {
            if (LpThread == null)
            {
                LpThread = new Thread(this.ThreadEntry);
                LpThread.Start();
            }
        }
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.startUpdater();
        }

        async private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (LpThread != null)
            {
                try
                {
                    LpThread.Abort();

                }
                catch (Exception e1)
                {

                }
            }
            LpThread = null;
        }
    }
}
