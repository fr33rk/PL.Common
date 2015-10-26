using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace PL.Logger
{

	/// <summary>Log event arguments used in the OnLog event of the log file.
	/// </summary>
	public class LogEventArgs : EventArgs
	{
		/// <summary>Initializes a new instance of the <see cref="LogEventArgs"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		public LogEventArgs(string message)
		{
			this.Message = message;
		}

		/// <summary>The log message.
		/// </summary>
		public string Message { get; private set; }
	}

	/// <summary>
	/// General log file for .net applications. 
	/// General usage:
	///    Create with constructor
	///    Set LogLevel. Log lines of this level and higher are actually written in the log file.
	///    Write log start
	///    Write log lines (including log-level)
	///    Write the log end
	///    Dispose the log file.
	///    LogFile is Thread safe!
	/// </summary>
	public class LogFile : IDisposable
	{
		#region Fields

		private const int cMaxFileSize = 1048576; // 1 MB

		// Object used for locking (Thread safety)
		private object mLockObject; 

		#endregion Fields

		#region Constructor

		/// <summary>Create the LogFile class. The log file will be written in the same folder as the executable by default.
		/// </summary>
		/// <param name="sName">The second part of the name of the log file. The first part is the domain name.</param>
		public LogFile(String sName)
			: this(sName, DefaultLogLevel, cMaxFileSize)
		{
			// Nothing additional to do here.
		}

		/// <summary>Create the LogFile class. The log file will be written in the same folder as the executable by default.
		/// </summary>
		/// <param name="sName">The second part of the name of the log file. The first part is the domain name.</param>
		/// <param name="logLevel">The log level.</param>
		/// <param name="maxFileSize">Maximum size of the file.</param>
		public LogFile(String sName, LogLevel logLevel, int maxFileSize)
		{
			if (sName.Length > 0)
			{
				this.FileName = string.Format("{0}.{1}.log"
										 , System.Reflection.Assembly.GetEntryAssembly().Location
										 , sName);

				mFile = new StreamWriter(FileName, true);
			}
			else
			{
				this.FileName = string.Format("{0}.log"
										 , System.Reflection.Assembly.GetEntryAssembly().Location);

				mFile = new StreamWriter(this.FileName, true);
			}

			mFile.AutoFlush = true;
			mLoglevel = logLevel;
			this.mMaxFileSize = maxFileSize;
			mEnableArchiving = true;

			mLockObject = new object();
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>Finalizes an instance of the <see cref="LogFile"/> class.
		/// </summary>
		~LogFile()
		{
			Dispose(false);
		}

		/// <summary>Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="includeManaged"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool includeManaged)
		{
			if (includeManaged)
			{
				// free managed resources
			}

			mFile.Dispose();
		}

		#endregion

		#region Filename

		/// <summary>Gets the name of the file.
		/// </summary>
		public string FileName
		{
			get;
			private set;
		}

		#endregion

		#region LogLevel

		/// <summary>The log-level is an indication of the severity of the log message. Each log-line should be set in one of these
		/// levels. 
		/// </summary>
		public enum LogLevel
		{
			/// <summary>Severe errors that cause premature termination
			/// </summary>
			Critical,

			/// <summary>Other runtime errors or unexpected conditions.
			/// </summary>
			Error,

			/// <summary>Use of deprecated APIs, poor use of API, 'almost' errors, other runtime situations that are undesirable or unexpected, but not necessarily "wrong". 
			/// </summary>
			Warning,

			/// <summary>Interesting runtime events (startup/shutdown).
			/// </summary>
			Info,

			/// <summary>Detailed information on the flow through the system.
			/// </summary>
			Debug
		};

		/// <summary>The default log level for all log files used in an solution.
		/// </summary>
		public static LogLevel DefaultLogLevel = LogLevel.Info;

		/// <summary>
		/// Override the default log level.
		/// </summary>
		/// <param name="loglevel">The new log-level</param>
		public void SetLoglevel(LogLevel loglevel)
		{
			lock (mLockObject)
			{
				mLoglevel = loglevel;
			}
		}

		/// <summary>
		/// Log-level set for this log file.
		/// </summary>
		private LogLevel mLoglevel;

		#endregion

		#region Log line writing

		/// <summary>
		/// Object used to do the actual writing.
		/// </summary>
		private StreamWriter mFile;

		/// <summary>
		/// Write the log start line. 
		/// </summary>
		public void WriteLogStart()
		{
			lock (mLockObject)
			{
				string logLine = string.Format("*********| {0} Logging started |**********\r\n PL.Logger version: {1}\r\n Max file size: {2} bytes\r\n LogLevel set to: {3}"
											   , GetTime()
											   , typeof(LogFile).Assembly.GetName().Version
											   , this.mMaxFileSize.ToString()
											   , this.mLoglevel.ToString());

				TextWriter.Synchronized(mFile).WriteLine(logLine);

				signalLog(logLine);
			}
		}

		/// <summary>
		/// Write the log end line
		/// </summary>
		public void WriteLogEnd()
		{
			lock (mLockObject)
			{
				string logLine = (String)"*********| " + GetTime() + " Logging ended   |**********" + Environment.NewLine;

				TextWriter.Synchronized(mFile).WriteLine(logLine);

				signalLog(logLine);
			}
		}

		/// <summary>
		/// Write a log file entry in the log file.
		/// </summary>
		/// <param name="sLine">The log line</param>
		/// <param name="logLevel">The log-level of the message. It is only actually printed when it is lower or equal to the set log-level </param>
		public void WriteLine(String sLine, LogLevel logLevel)
		{
			lock (mLockObject)
			{
				WriteLine(sLine, logLevel, true);
			}
		}

		/// <summary>
		/// Write a log file entry in the log file.
		/// </summary>
		/// <param name="sLine">The log line</param>
		/// <param name="loglevel">The log-level of the message. It is only actually printed when it is lower or equal to the set log-level</param>
		/// <param name="checkSize">if set to <c>true</c> [check size].</param>
		private void WriteLine(String sLine, LogLevel loglevel, bool checkSize)
		{
			if (loglevel <= mLoglevel)
			{
				sLine = sLine.Replace("\r", "<CR>");
				sLine = sLine.Replace("\n", "<LF>");

				string logLine = String.Format("{0} [{1, 8}] - {2}", GetTime(), LogLevelToText(loglevel), sLine);
				TextWriter.Synchronized(mFile).WriteLine(logLine);

				signalLog(logLine);

				if (checkSize)
				{ 
					CheckSize();
				}
			}
		}

		#endregion

		#region HelperFunctions

		/// <summary>
		/// Get the date and time string formatted for the log file.
		/// </summary>
		/// <returns>The formatted string</returns>
		private String GetTime()
		{
			return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
		}

		/// <summary>
		/// Translate the log-level to text.
		/// </summary>
		/// <param name="loglevel">The log-level to translate</param>
		/// <returns>The translated log-level</returns>
		private String LogLevelToText(LogLevel loglevel)
		{
			switch (loglevel)
			{
				case LogLevel.Debug: return "DEBUG";
				case LogLevel.Info: return "INFO";
				case LogLevel.Warning: return "WARNING";
				case LogLevel.Error: return "ERROR";
				case LogLevel.Critical: return "CRITICAL";
				default: return "UNKNOWN";
			}
		}

		#endregion

		#region OnLog event

		/// <summary>Occurs when a log line is written in the log file.
		/// </summary>
		public event EventHandler<LogEventArgs> OnLog;

		/// <summary>Inform all clients attached to OnLog that a log line has been written.
		/// </summary>
		/// <param name="logLine">The log line.</param>
		private void signalLog(string logLine)
		{
			var handler = OnLog;
			if (handler != null)
			{
				handler(this, new LogEventArgs(logLine));
			}
		}

		#endregion OnLog event

		#region File size

		private int mMaxFileSize;
		private bool mEnableArchiving;

		/// <summary>Checks the size of the log file and test if it needs to be archived.
		/// </summary>
		private void CheckSize()
		{
			if (mEnableArchiving)
			{
				FileInfo logFileInfo = new FileInfo(this.FileName);
				if (logFileInfo.Length > mMaxFileSize)
				{
					WriteLine(string.Format("Current file size {0} exceeds max file size of {1}"
											, logFileInfo.Length.ToString()
											, mMaxFileSize.ToString()), LogLevel.Info, false);
					Archive();
				}
			}
		}

		/// <summary>Moves the log file to the log file archive to save space. After that creates new empty log file.
		/// </summary>
		/// <remarks>ZipArchive takes ownership of FileStream and might dispose it. Considered a false positive.</remarks>
		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		public void Archive()
		{
			mFile.Close();

			string archivedFileName = this.FileName.Replace(".log", string.Format(".{0}.log", DateTime.Now.ToString("yyyyMMdd.HHmmss")));
			string archiveName = this.FileName.Replace(".log", ".zip");

			try
			{
				// Rename the log file
				File.Move(this.FileName, archivedFileName);

				using (FileStream zipToOpen = new FileStream(archiveName, FileMode.OpenOrCreate))
				{
					using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
					{
						ZipArchiveEntry readmeEntry = archive.CreateEntry(archivedFileName);
						using (Stream zipEntryStream = readmeEntry.Open())
						{
							using (FileStream logFileStream = new FileStream(archivedFileName, FileMode.Open))
							{
								logFileStream.Position = 0;
								logFileStream.CopyTo(zipEntryStream);
								zipEntryStream.Flush();
							}
						}
					}
				}

				// Remove the log file
				File.Delete(archivedFileName);
			}
			catch (Exception e)
			{
				mEnableArchiving = false;
				WriteLine(string.Format("Error archiving log file {0}.\r\nArchiving is disabled.", e.Message), LogLevel.Error);
			}
			finally
			{
				// Reopen the file stream
				mFile = new StreamWriter(FileName, true);
				mFile.AutoFlush = true;
				WriteLine(string.Format("Continuing log file. Previous log file is {0}", archivedFileName), LogLevel.Info);
			}
		}

		#endregion File size

		#region Shortcuts

		/// <summary>Write a critical error in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Critical(string message)
		{
			this.WriteLine(message, LogLevel.Critical);
		}

		/// <summary>Write an error in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Error(string message)
		{
			this.WriteLine(message, LogLevel.Error);
		}

		/// <summary>Write a warning in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Warning(string message)
		{
			this.WriteLine(message, LogLevel.Warning);
		}

		/// <summary>Write information in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Info(string message)
		{
			this.WriteLine(message, LogLevel.Info);
		}

		/// <summary>Write a debug message in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Debug(string message)
		{
			this.WriteLine(message, LogLevel.Debug);
		}

		#endregion Shortcuts
	}
}
