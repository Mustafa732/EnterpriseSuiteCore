using System;
using System.Diagnostics;
using System.Configuration;
using Common.Config;

namespace Common.Utils
{
	/// <summary>
	/// This class is for logging messages in file or console.
	/// </summary>
	public class Logger
	{
		#region Constants and Variable Declaration
		private static string LOG_TIME_FORMAT = "dd.MM.yyyy HH:mm:ss";
		private string DEFAULT_LOG_PATH = System.AppDomain.CurrentDomain.BaseDirectory;
		private string DEFAULT_LOG_FILENAME = "EBPS";
		private string DEFULT_EVENTLOG_SOURCE = "WebAppEventSource";
		private string DEFULT_WRITER_NAME = "FileLogging";
		private string DEFULT_EVENTLOG_NAME = "WebAppEventLog";
		private int SEVERITY_LEVEL=4;
		private static Logger staticLogger = null;
		private const string CRLF = Constants.CRLF;
		private const string EXT = ".txt";
		private TextWriterTraceListener objTextWriterListener = null;
		private TraceLevel m_severityLevel;
		private DateTime FileDate=DateTime.Now;
		#endregion

		#region getters and setters
		public string LogPath
		{
			set
			{
				if(value != null)
				{
					DEFAULT_LOG_PATH = value;
				}
			}
			get
			{
				return DEFAULT_LOG_PATH ;
			}
		}
		public string LogFileName
		{
			set
			{
				if(value != null)
				{
					DEFAULT_LOG_FILENAME= value;
				}
			}
			get
			{
				return DEFAULT_LOG_FILENAME;
			}
		}
		public string TraceEventSource
		{
			set
			{
				if(value != null)
				{
					DEFULT_EVENTLOG_SOURCE = value;
				}
			}
			get
			{
				return DEFULT_EVENTLOG_SOURCE;
			}
		}
		public int DebugLevel
		{
			get
			{
				return SEVERITY_LEVEL;
			}
		}
		#endregion
		
		public static Logger getInstance()
		{

			if (staticLogger==null)
			{
				Logger objLogger = new Logger();
				staticLogger = objLogger;
				return staticLogger;
			}
			else
			{
				return staticLogger;
			}
		}
		private Logger()
		{
			// Set default severity level
			m_severityLevel = TraceLevel.Warning;

			// Attempt to set the severity level from system parameters
			try
			{
				string logLevel = "Verbose";
				if(ConfigurationSettings.AppSettings["LOG_SEVERITY_LEVEL"]!=null && !(ConfigurationSettings.AppSettings["LOG_SEVERITY_LEVEL"].Equals("")))
				{
					logLevel = ConfigurationSettings.AppSettings["LOG_SEVERITY_LEVEL"];
				}
				
				setDebugLevel(logLevel);
			}
			catch(Exception e)
			{
				m_severityLevel = TraceLevel.Verbose;
			}

			// Initlistners Code
			if (Trace.Listeners[DEFULT_WRITER_NAME] == null)
			{
				string LogFileName = DEFAULT_LOG_FILENAME;
				string LogPath = DEFAULT_LOG_PATH;

				if(Constants.LOGFILENAME!=null && Constants.LOGPATH != null)
				{
					LogFileName = Constants.LOGFILENAME; 
					LogPath =  Constants.LOGPATH; 
				}
				DateTime date=DateTime.Now;				
				string FDate=date.Year.ToString() + date.Month.ToString().PadLeft(2,'0') + date.Day.ToString().PadLeft(2,'0');
										
				string FullyQualifiedFileName = LogPath + LogFileName+ FDate + EXT ;				
				
				objTextWriterListener = new TextWriterTraceListener(FullyQualifiedFileName);
				
				Trace.Listeners.Add(objTextWriterListener);
				Trace.AutoFlush = true;
				
			}
		
			// End
		}
		
		/// <summary>
		/// Check the provided severity level and set the level in the logger 
		/// </summary>
		/// <param name="logLevel"></param>
		public void setDebugLevel(string logLevel)
		{
			
			if(logLevel.Equals("Off"))
			{
				m_severityLevel = TraceLevel.Off;
				SEVERITY_LEVEL=0;
			}
			else if(logLevel.Equals("Error"))
			{
				m_severityLevel = TraceLevel.Error;
				SEVERITY_LEVEL=1;
			}
			else if(logLevel.Equals("Warning"))
			{
				m_severityLevel = TraceLevel.Warning;
				SEVERITY_LEVEL=2;
			}
			else if(logLevel.Equals("Info"))
			{
				m_severityLevel = TraceLevel.Info;
				SEVERITY_LEVEL=3;
			}
			else if(logLevel.Equals("Verbose"))
			{
				m_severityLevel = TraceLevel.Verbose;
				SEVERITY_LEVEL=4;
			}
			else
			{
				m_severityLevel = TraceLevel.Verbose;
				SEVERITY_LEVEL=4;
			}			
		}

		/// <summary>
		/// Check the provided severity level and set the level in the logger 
		/// </summary>
		/// <param name="logLevel"></param>
		public void SetDebugLevel(int logLevel)
		{
			SEVERITY_LEVEL=logLevel;
			if(logLevel==0)
			{
				m_severityLevel = TraceLevel.Off;
				
			}
			else if(logLevel==1)
			{
				m_severityLevel = TraceLevel.Error;
				SEVERITY_LEVEL=1;
			}
			else if(logLevel==2)
			{
				m_severityLevel = TraceLevel.Warning;
			}
			else if(logLevel==3)
			{
				m_severityLevel = TraceLevel.Info;
			}
			else if(logLevel==4)
			{
				m_severityLevel = TraceLevel.Verbose;
			}
			else
			{
				m_severityLevel = TraceLevel.Verbose;
				SEVERITY_LEVEL=4;
			}			
		}

		/// <summary>
		/// This method will load and initialize the trace listneres 
		/// </summary>
		public void InitTraceListeners()
		{
			if (Trace.Listeners[DEFULT_WRITER_NAME] == null)
			{
				string LogFileName; // = DEFAULT_LOG_FILENAME;
				string LogPath; // = DEFAULT_LOG_PATH;

				//if(Constants.LOGFILENAME!=null && Constants.LOGPATH != null)
				//{
					LogFileName = Constants.LOGFILENAME; 
					LogPath =  Constants.LOGPATH; 
				//}
				DateTime date=DateTime.Now;
                string FDate = date.Year.ToString() + date.Month.ToString().PadLeft(2, '0') + date.Day.ToString().PadLeft(2, '0');
										
				string FullyQualifiedFileName = LogPath + LogFileName+ FDate + EXT ;
								
				if (FileDate.Date!=date.Date)
				{
					FileDate=DateTime.Now;

					Trace.Listeners.Clear();
					Trace.AutoFlush=false;

					objTextWriterListener = new TextWriterTraceListener(FullyQualifiedFileName);

					Trace.Listeners.Remove(objTextWriterListener);	

					Trace.Listeners.Add(objTextWriterListener);
					Trace.AutoFlush = true;
				}
			}
		}

		/// <summary>
		/// This method will remove the TextWriter Listener from Trace Listeners List 
		/// </summary>
		public void DisposeTraceListeners()
		{
			
			Trace.Listeners.Remove(objTextWriterListener);
			objTextWriterListener.Close();

		}

		/// <summary>
		/// This Method will write the Messages to Listners along with Module Name and Method Name
		/// </summary>
		/// <param name="severity">Message Severity: Will be used to check whether to log the message or not based on the severity level found in web.Constants</param>
		/// <param name="methodName">Method Name</param>
		/// <param name="message">Message String</param>
		public void Log(System.Diagnostics.TraceLevel severity, string moduleName, string methodName, string message)
		{
			string strLogMessage = "";

			if (m_severityLevel >= severity)
			{
				Trace.IndentSize = 4;
				strLogMessage = DateTime.Now.ToString (LOG_TIME_FORMAT) + " | " + String.Format ("{0,-7}|{1,-12}|{2,-15}| ",severity ,moduleName, methodName) +  message;
				Trace.WriteLine(strLogMessage);
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="severity"></param>
		public void Log(string message, System.Diagnostics.TraceLevel severity)
		{
		
			if (m_severityLevel >= severity)
				Trace.WriteLine(DateTime.Now.ToString(LOG_TIME_FORMAT) + " " + message +  CRLF);
			
		}

		#region Logging Methods
		/// <summary>
		/// This method will be used for logging Debug/Info Messages
		/// </summary>
		/// <param name="message"></param>
		public void Debug(string aModuleName, string aMethodName, string aMessage)
		{
			InitTraceListeners();
			this.Log(System.Diagnostics.TraceLevel.Info, aModuleName, aMethodName, aMessage);
			
		}
        public void Debug(string aMessage)
        {
            InitTraceListeners();
            StackTrace trace = new StackTrace();
            this.Log(System.Diagnostics.TraceLevel.Info, trace.GetFrame(1).GetMethod().DeclaringType.Name, trace.GetFrame(1).GetMethod().Name, aMessage);
        }


		/// <summary>
		/// This method will be used for logging Error Messages
		/// </summary>
		/// <param name="message"></param>
		public void Error(string aModuleName,string aMethodName, string aMessage)
		{
			InitTraceListeners();
			this.Log(System.Diagnostics.TraceLevel.Error,aModuleName,aMethodName,aMessage);			
		}
        public void Error(string aMessage)
        {
            InitTraceListeners();
            StackTrace trace = new StackTrace();
            this.Log(System.Diagnostics.TraceLevel.Error, trace.GetFrame(1).GetMethod().DeclaringType.Name, trace.GetFrame(1).GetMethod().Name, aMessage);
        }
		
		/// <summary>
		/// This method will be used for logging Error Messages
		/// </summary>
		/// <param name="message"></param>
		public void Error( string aModuleName,string aMethodName,Exception error)
		{
			InitTraceListeners();
			this.Log(System.Diagnostics.TraceLevel.Error,aModuleName,aMethodName,error.StackTrace);
			
		}
        public void Error(Exception error)
        {
            InitTraceListeners();
            StackTrace trace = new StackTrace();
            this.Log(System.Diagnostics.TraceLevel.Error, trace.GetFrame(1).GetMethod().DeclaringType.Name, trace.GetFrame(1).GetMethod().Name, error.StackTrace);

        }

		/// <summary>
		/// This method will be used for logging Warning Messages
		/// </summary>
		/// <param name="message"></param>
		public void Warning( string aModuleName,string aMethodName, string aMessage)
		{
			InitTraceListeners();
			this.Log(System.Diagnostics.TraceLevel.Warning,aModuleName,aMethodName,aMessage);
        }
        public void Warning(string aMessage)
        {
            InitTraceListeners();
            StackTrace trace = new StackTrace();
            this.Log(System.Diagnostics.TraceLevel.Warning, trace.GetFrame(1).GetMethod().DeclaringType.Name, trace.GetFrame(1).GetMethod().Name, aMessage);
        }
        #endregion 
    }
}
