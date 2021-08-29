using System.Diagnostics;
namespace EricZhao.UiThread
{

    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using System.Net;
    using System.Net.Sockets;
 


    public class ThreadUiController
    {
        private static StreamWriter m_logger = null;
        private static int m_nLogFileSizeLimit = 0x300000;
        private static FileInfo m_pFeedFileInfo = null;
        private static string m_strFeedLock = "lock";
        private static string m_strFeedName = null;
        private static string m_strLogFileName = null;
        private static string m_strLogLocker = "LogLocker";
        private static int m_nlogLevel = 0;
        private static String m_strSettingFile = "./settings.ini";

        public static System.String SettingFile
        {
            get
            {
                String lstrFile = "";
                lock (m_strLogLocker)
                {
                    lstrFile = m_strSettingFile;
                }
                return lstrFile;
            }
            set
            {
                lock (m_strLogLocker)
                {
                    m_strSettingFile = value;
                }
            }
        }
        private static void _disableControl(CallerParameter args)
        {
            if (args != null && args.Control != null) args.Control.Enabled = false;
        }

        private static void _enableControl(CallerParameter args)
        {
            if (args != null && args.Control != null) args.Control.Enabled = true;
        }

        private static void _getControlText(CallerParameter args)
        {
            if (args != null && args.Control != null) args.Out_value = args.Control.Text;
        }

        private static void _HideControl(CallerParameter args)
        {
            if (args != null && args.Control != null) args.Control.Hide();
        }

        private static void _InvokeControlCustomMethod(CallerParameter args)
        {
            if (args != null && args.Control != null && args.In_value != null && args.in_value is MethodCallCaller) ((MethodCallCaller)args.In_value)(args);
        }

        private static void _InvokeControlCustomMethod2(CallerParameter args)
        {
            if (args != null && args.Control != null && args.In_value != null && args.in_value is CustomMethodCallInvoker) ((CustomMethodCallInvoker)args.In_value)(args.Control);
        }

        private static void _setControlText(CallerParameter args)
        {
            if (args != null && args.Control != null && args.In_value != null) args.Control.Text = args.in_value.ToString();
        }

        private static void _ShowControl(CallerParameter args)
        {
            if (args != null && args.Control != null) args.Control.Show();
        }

        public static void DisableControl(Control argsControl)
        {
            CallerParameter args = new CallerParameter();
            args.Control = argsControl;
            new ControlCaller(args.Control).Invoke(new MethodCallCaller(ThreadUiController._disableControl), args);
        }

        public static void EnableControl(Control argsControl)
        {
            CallerParameter args = new CallerParameter();
            args.Control = argsControl;
            new ControlCaller(args.Control).Invoke(new MethodCallCaller(ThreadUiController._enableControl), args);
        }

        public static void Feed()
        {
            string str;
            Monitor.Enter(str = m_strFeedLock);
            try
            {
                if (m_strFeedName == null)
                {
                    m_strFeedName = Path.GetFileName(Application.ExecutablePath);
                    int length = m_strFeedName.LastIndexOf('.');
                    if (length > 0) m_strFeedName = m_strFeedName.Substring(0, length);
                    m_strFeedName = m_strFeedName + ".txt";
                    m_pFeedFileInfo = new FileInfo(m_strFeedName);
                }
                if (m_pFeedFileInfo != null)
                {
                    try
                    {
                        if (!m_pFeedFileInfo.Exists)
                        {
                            FileStream stream = m_pFeedFileInfo.Create();
                            if (stream != null) stream.Close();
                        }
                        m_pFeedFileInfo.LastWriteTime = DateTime.Now;
                    }
                    catch (Exception exception)
                    {
                        log(exception.Message, LOG_LEVEL.FATAL);
                    }
                }
            }
            catch (Exception exception2)
            {
                log(exception2.Message, LOG_LEVEL.FATAL);
            }
            finally
            {
                Monitor.Exit(str);
            }
        }

        public static string getControlText(Control argsControl)
        {
            CallerParameter args = new CallerParameter();
            args.Control = argsControl;
            new ControlCaller(args.Control).Invoke(new MethodCallCaller(ThreadUiController._getControlText), args);
            return args.Out_value.ToString();
        }

        public static void HideControl(Control argsControl)
        {
            CallerParameter args = new CallerParameter();
            args.Control = argsControl;
            new ControlCaller(args.Control).Invoke(new MethodCallCaller(ThreadUiController._HideControl), args);
        }

        public static bool InitLog(String astrFile = "./settings.ini")
        {
            bool flag;
            try
            {
                SettingFile = astrFile;

                lock (m_strLogLocker)
                {
                    if (m_logger != null)
                    {
                        try
                        {
                            m_logger.Flush();
                            m_logger.Close();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (!Directory.Exists("log"))
                    {
                        try
                        {
                            Directory.CreateDirectory("log");

                        }
                        catch (Exception)
                        {
                        }
                    }
                    IniFile loFile = new IniFile(ThreadUiController.SettingFile);
                    m_nlogLevel = loFile.IniReadIntValue("log", "log_level", (int)LOG_LEVEL.ERROR, true);
                    m_nLogFileSizeLimit = loFile.IniReadIntValue("log", "file_length_limit", m_nLogFileSizeLimit, true);
                    flag = true;
                }
            }
            catch (Exception)
            {
                flag = false;
            }
            return flag;
        }

        public static String GetlogFileName()
        {
            lock (m_strLogLocker)
            {
                FileInfo info = null;

                Boolean lbFileExist = false;

                try
                {
                    if (m_strLogFileName != null)
                    {
                        info = new FileInfo(m_strLogFileName);

                        lbFileExist = true;
                    }

                }
                catch (Exception)
                {
                    
                }

                string str = DateTime.Now.ToString("yyyy'-'MM'-'dd");

                string fileName = "log/" + str + ".log";

                if (lbFileExist)
                {
                    fileName = m_strLogFileName;
                }

                int num = 0;

            Label_006F:
                num++;

                try
                {
                    info = new FileInfo(fileName);
                }
                catch (Exception)
                {
                    return "";
                }
                if (info.Exists && info.Length >= m_nLogFileSizeLimit)
                {
                    fileName = string.Concat(new object[] { "log/", str, "-", num, ".log" });
                    goto Label_006F;
                }
                try
                {

                    m_strLogFileName = fileName;
                }
                catch (Exception)
                {
                    m_logger = null;
                    return "";
                }

                return m_strLogFileName;
            }

        }

        public static void InvokeControlCustomMethod(Control argsControl, MethodCallCaller aoCustomMethod)
        {
            CallerParameter args = new CallerParameter();
            args.Control = argsControl;
            args.In_value = aoCustomMethod;
            new ControlCaller(args.Control).Invoke(new MethodCallCaller(ThreadUiController._InvokeControlCustomMethod), args);
        }

        public static void InvokeControlCustomMethod2(Control argsControl, CustomMethodCallInvoker aoCustomMethod)
        {
            CallerParameter args = new CallerParameter();
            args.Control = argsControl;
            args.In_value = aoCustomMethod;
            new ControlCaller(args.Control).Invoke(new MethodCallCaller(ThreadUiController._InvokeControlCustomMethod2), args);
        }

        public static void InvokeControlCustomMethod3(Control argsControl, MethodCallCaller aoCustomMethod, object[] args)
        {
            CallerParameter parameter = new CallerParameter();
            parameter.Control = argsControl;
            parameter.In_value = args;
            new ControlCaller(parameter.Control).Invoke(aoCustomMethod, parameter);
        }

        private static bool IsLogExceedLength()
        {
            if (m_strLogFileName == null) return false;
            FileInfo info = null;
            try
            {
                info = new FileInfo(m_strLogFileName);
            }
            catch (Exception)
            {
                return false;
            }
            return (info.Length >= m_nLogFileSizeLimit);
        }

        public static void log(string astrlog, LOG_LEVEL aenumLogLevel = LOG_LEVEL.INFO,Boolean abPrintStack = true)
        {
            try
            {
                lock (m_strLogLocker)
                {
                    if (ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None) != null
                     && (ThreadUiController.m_nlogLevel <= (int)aenumLogLevel)
                     && astrlog != null
                     && (InitLog()||!IsLogExceedLength() )
                     )
                    {
                        FileInfo info = new FileInfo(GetlogFileName());
                        m_logger = new StreamWriter(info.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                        string format = "{0} | [{1}] | {2} \r\n";
                        StringBuilder builder = new StringBuilder();
                        string str2 = DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
                        builder.AppendFormat(format, str2, aenumLogLevel, astrlog);

                        
                        if (aenumLogLevel == LOG_LEVEL.FATAL || aenumLogLevel == LOG_LEVEL.ERROR || abPrintStack)
                        {
                            var l_CurrentStack = new System.Diagnostics.StackTrace(true);
                            String lstrData = l_CurrentStack.ToString();
                            builder.Append(lstrData);                          
                          
                        }
                        m_logger.Write(builder.ToString());
                        m_logger.Flush();
                        m_logger.Close();
                        m_logger = null;
                    }
                }

            }
            catch (Exception)
            {
            }
        }

        public static void SetControlText(Control argsControl, string astrValue)
        {
            CallerParameter args = new CallerParameter();
            args.Control = argsControl;
            args.In_value = astrValue;
            new ControlCaller(args.Control).Invoke(new MethodCallCaller(ThreadUiController._setControlText), args);
        }

        public static void ShowControl(Control argsControl)
        {
            CallerParameter args = new CallerParameter();
            args.Control = argsControl;
            new ControlCaller(args.Control).Invoke(new MethodCallCaller(ThreadUiController._ShowControl), args);
        }

        public class CallerParameter
        {
            public System.Windows.Forms.Control control;
            public object in_value;
            public object out_value;

            public System.Windows.Forms.Control Control
            {
                get
                {
                    return this.control;
                }
                set
                {
                    this.control = value;
                }
            }

            public object In_value
            {
                get
                {
                    return this.in_value;
                }
                set
                {
                    this.in_value = value;
                }
            }

            public object Out_value
            {
                get
                {
                    return this.out_value;
                }
                set
                {
                    this.out_value = value;
                }
            }
        }

        private class ConfigSectionData : ConfigurationSection
        {
            [ConfigurationProperty("log_level")]
            public ThreadUiController.LOG_LEVEL log_level
            {
                get
                {
                    return (ThreadUiController.LOG_LEVEL)base["log_level"];
                }
                set
                {
                    base["log_level"] = value;
                }
            }
        }

        public class ControlCaller
        {
            private Queue<MethodCall> argumentInvokeList = new Queue<MethodCall>();
            private Control control;

            public ControlCaller(Control control)
            {
                this.control = control;
            }

            private void ControlInvoke(object sender, EventArgs e)
            {
                MethodCall call = this.argumentInvokeList.Dequeue();
                call.invoker(call.args);
            }

            public void Invoke(ThreadUiController.MethodCallCaller invoker, ThreadUiController.CallerParameter args)
            {
                this.argumentInvokeList.Enqueue(new MethodCall(invoker, args));
                try
                {
                    this.control.Invoke(new EventHandler(this.ControlInvoke));
                }
                catch (Exception exception)
                {
                    string message = exception.Message;
                }
            }

            private class MethodCall
            {
                public ThreadUiController.CallerParameter args;
                public ThreadUiController.MethodCallCaller invoker;

                public MethodCall(ThreadUiController.MethodCallCaller invoker, ThreadUiController.CallerParameter args)
                {
                    this.invoker = invoker;
                    this.args = args;
                }
            }
        }

        public delegate void CustomMethodCallInvoker(Control argsControl);

        public enum LOG_LEVEL
        {
            DEBUG = 1,
            ERROR = 8,
            FATAL = 0x10,
            INFO = 2,
            NONE = 17,
            WARN = 4
        }

        public delegate void MethodCallCaller(ThreadUiController.CallerParameter args);
    } //public class ThreadUiController 

    public class HttpProcessor
    {
        public TcpClient socket;
        public HttpServer srv;

        private Stream inputStream;
        public StreamWriter outputStream;

        public String http_method;
        public String http_url;
        public String http_protocol_versionstring;
        public System.Collections.Hashtable httpHeaders = new System.Collections.Hashtable();


        private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

        public HttpProcessor(TcpClient s, HttpServer srv)
        {
            this.socket = s;
            this.srv = srv;
        }


        private string streamReadLine(Stream inputStream)
        {
            int next_char;
            string data = "";
            while (true)
            {
                next_char = inputStream.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                if (next_char == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(next_char);
            }
            return data;
        }
        public void process()
        {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            inputStream = new BufferedStream(socket.GetStream());

            // we probably shouldn't be using a streamwriter for all output from handlers either
            outputStream = new StreamWriter(new BufferedStream(socket.GetStream()));
            try
            {
                parseRequest();
                readHeaders();
                if (http_method.Equals("GET"))
                {
                    handleGETRequest();
                }
                else if (http_method.Equals("POST"))
                {
                    handlePOSTRequest();
                }
            }
            catch (Exception e)
            {
                xConsole.WriteLine("Exception: " + e.ToString());
                writeFailure();
            }
            outputStream.Flush();
            // bs.Flush(); // flush any remaining output
            inputStream = null; outputStream = null; // bs = null;            
            socket.Close();
        }

        public void parseRequest()
        {
            String request = streamReadLine(inputStream);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }
            http_method = tokens[0].ToUpper();
            http_url = tokens[1];
            http_protocol_versionstring = tokens[2];

            xConsole.WriteLine("starting: " + request);
        }

        public void readHeaders()
        {

            xConsole.WriteLine("readHeaders()");
            String line;
            while ((line = streamReadLine(inputStream)) != null)
            {
                if (line.Equals(""))
                {
                    xConsole.WriteLine("got headers");
                    return;
                }

                int separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                String name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++; // strip any spaces
                }

                string value = line.Substring(pos, line.Length - pos);
                xConsole.WriteLine("header: {0}:{1}", name, value);
                httpHeaders[name] = value;
            }
        }

        public void handleGETRequest()
        {
            srv.handleGETRequest(this);
        }

        private const int BUF_SIZE = 4096;
        public void handlePOSTRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 

            xConsole.WriteLine("get post data start");
            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (this.httpHeaders.ContainsKey("Content-Length"))
            {
                content_len = Convert.ToInt32(this.httpHeaders["Content-Length"]);
                if (content_len > MAX_POST_SIZE)
                {
                    throw new Exception(
                        String.Format("POST Content-Length({0}) too big for this simple server",
                          content_len));
                }
                byte[] buf = new byte[BUF_SIZE];
                int to_read = content_len;
                while (to_read > 0)
                {
                    xConsole.WriteLine("starting Read, to_read={0}", to_read);

                    int numread = this.inputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
                    xConsole.WriteLine("read finished, numread={0}", numread);
                    if (numread == 0)
                    {
                        if (to_read == 0)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    to_read -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }
            xConsole.WriteLine("get post data end");
            srv.handlePOSTRequest(this, new StreamReader(ms));

        }

        public void writeSuccess(string content_type = "text/html")
        {
            outputStream.WriteLine("HTTP/1.0 200 OK");
            outputStream.WriteLine("Content-Type: " + content_type);
            outputStream.WriteLine("Connection: close");
            outputStream.WriteLine("");
        }

        public void writeFailure()
        {
            outputStream.WriteLine("HTTP/1.0 404 File not found");
            outputStream.WriteLine("Connection: close");
            outputStream.WriteLine("");
        }
    } //public class HttpProcessor 

    public abstract class HttpServer
    {

        protected int port;
        TcpListener listener;
        bool is_active = true;

        public HttpServer(int port)
        {
            this.port = port;
        }

        public void listen()
        {
            listener = new TcpListener(port);
            listener.Start();
            Boolean lb_IsActive = is_active;
            while (lb_IsActive)
            {
                TcpClient s = listener.AcceptTcpClient();
                HttpProcessor processor = new HttpProcessor(s, this);
                Thread thread = new Thread(new ThreadStart(processor.process));
                thread.Start();
                Thread.Sleep(1);

                lock (this.m_strLocker)
                {
                    lb_IsActive = is_active;
                }
            }
        }

        String m_strLocker = "httpServer";

        public void Stop()
        {
            lock (this)
            {
                this.is_active = false;
            } 
        }


        public abstract void handleGETRequest(HttpProcessor p);
        public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
    }//public abstract class HttpServer 


    public class xConsole
    {
        public static void WriteLine(string format, params object[] arg)
        {
#if DEBUG
            //Console.WriteLine(format,arg);
#endif
        }
        public static void WriteLine(String astrData)
        {
#if DEBUG
           // Console.WriteLine(astrData);
#endif
        }
    }
    
}

