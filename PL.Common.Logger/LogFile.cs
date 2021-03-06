﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using PL.Logger;

namespace PL.Common.Logger
{
	/// <inheritdoc />
	/// <summary>Log event arguments used in the OnLog event of the log file.
	/// </summary>
	public class LogEventArgs : EventArgs
	{
		/// <inheritdoc />
		/// <summary>Initializes a new instance of the <see cref="T:PL.Common.Logger.LogEventArgs" /> class.
		/// </summary>
		/// <param name="message">The message.</param>
		public LogEventArgs(string message)
		{
			Message = message;
		}

		/// <summary>The log message.
		/// </summary>
		public string Message { get; }
	}

	/// <summary>General log file for .net applications.
	/// General usage:
	///    Create with constructor
	///    Set LogLevel. Log lines of this level and higher are actually written in the log file.
	///    Write log start
	///    Write log lines (including log-level)
	///    Write the log end
	///    Dispose the log file.
	///    LogFile is Thread safe!
	/// </summary>
	public class LogFile : IDisposable, ILogFile
	{
		#region Fields

		private const int MaxFileSize = 1048576; // 1 MB
		private readonly bool mUseSingleLineLogging;

		// Object used for locking (Thread safety)
		private readonly object mLockObject;

		#endregion Fields

		#region Constructor

		/// <inheritdoc />
		/// <summary>Create the LogFile class. The log file will be written in the same folder as the executable by default.
		/// </summary>
		/// <param name="name">The second part of the name of the log file. The first part is the domain name.</param>
		public LogFile(string name)
			: this(name, DefaultLogLevel, MaxFileSize, true)
		{
			// Nothing additional to do here.
		}

		/// <summary>
		/// Create the LogFile class. The log file will be written in the same folder as the executable by default.
		/// </summary>
		/// <param name="name">The second part of the name of the log file. The first part is the domain name.</param>
		/// <param name="logLevel">The minimum log level. E.g when set to info, debug messages wont be logged.</param>
		/// <param name="maxFileSize">Maximum size of the file in bytes.</param>
		/// <param name="useSingleLineLogging">if set to <c>true</c> multi line log entries will be rewritten to single line logs.</param>
		public LogFile(string name, LogLevel logLevel, int maxFileSize, bool useSingleLineLogging)
		{
			var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
			string productName;

			if (entryAssembly != null)
			{
				var fileVersionInfo = FileVersionInfo.GetVersionInfo(entryAssembly.Location);
				var companyName = fileVersionInfo.CompanyName;
				productName = fileVersionInfo.ProductName;

				if (companyName != string.Empty && productName != string.Empty)
				{
					FileName = $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\{companyName}\{productName}";
					Directory.CreateDirectory(FileName);
				}
				else
				{
					throw new InvalidOperationException("Unable to determine log file location due to missing company name or product name.");
				}
			}
			else
			{
				throw new InvalidOperationException("Unable to determine log file location due to missing FileVersionInfo");
			}

			if (name.Length > 0)
			{
				FileName = $@"{FileName}\{productName}.{name}.log";

				mFile = new StreamWriter(FileName, true);
			}
			else
			{
				FileName = $@"{FileName}\{productName}.log";

				mFile = new StreamWriter(FileName, true);
			}

			mFile.AutoFlush = true;
			mLogLevel = logLevel;
			mMaxFileSize = maxFileSize;
			mEnableArchiving = true;
			mUseSingleLineLogging = useSingleLineLogging;

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

			try
			{
				mFile.Dispose();
			}
			catch (ObjectDisposedException)
			{
				// mFile is already closed/disposed apparently. Have not found a method that can test if a filestream is open or not.
			}
		}

		#endregion Constructor

		#region Filename

		/// <summary>Gets the name of the file.
		/// </summary>
		public string FileName
		{
			get;
		}

		#endregion Filename

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
		/// <param name="logLevel">The new log-level</param>
		public void SetLogLevel(LogLevel logLevel)
		{
			lock (mLockObject)
			{
				mLogLevel = logLevel;
			}
		}

		/// <summary>
		/// Log-level set for this log file.
		/// </summary>
		private LogLevel mLogLevel;

		#endregion LogLevel

		#region Log line writing

		/// <summary>
		/// Object used to do the actual writing.
		/// </summary>
		private StreamWriter mFile;

		/// <summary>Write the log start line.
		/// </summary>
		public void WriteLogStart()
		{
			lock (mLockObject)
			{
				var logLine =
					$"*********| {GetTime()} Logging started |**********\r\n PL.Logger version: {typeof(LogFile).Assembly.GetName().Version}\r\n Max file size: {mMaxFileSize.ToString()} bytes\r\n LogLevel set to: {mLogLevel.ToString()}";

				TextWriter.Synchronized(mFile).WriteLine(logLine);

				SignalLog(logLine);
			}
		}

		/// <summary>Write the log end line
		/// </summary>
		public void WriteLogEnd()
		{
			lock (mLockObject)
			{
				var logLine = "*********| " + GetTime() + " Logging ended   |**********" + Environment.NewLine;

				TextWriter.Synchronized(mFile).WriteLine(logLine);

				SignalLog(logLine);
			}
		}

		/// <summary>
		/// Write a log file entry in the log file.
		/// </summary>
		/// <param name="sLine">The log line</param>
		/// <param name="logLevel">The log-level of the message. It is only actually printed when it is lower or equal to the set log-level </param>
		public void WriteLine(string sLine, LogLevel logLevel)
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
		/// <param name="logLevel">The log-level of the message. It is only actually printed when it is lower or equal to the set log-level</param>
		/// <param name="checkSize">if set to <c>true</c> [check size].</param>
		private void WriteLine(string sLine, LogLevel logLevel, bool checkSize)
		{
			if (logLevel <= mLogLevel)
			{
				if (mUseSingleLineLogging)
				{
					sLine = sLine.Replace("\r", "<CR>");
					sLine = sLine.Replace("\n", "<LF>");
				}

				var logLine = $"{GetTime()} [{LogLevelToText(logLevel),8}] - {sLine}";
				TextWriter.Synchronized(mFile).WriteLine(logLine);

				SignalLog(logLine);

				if (checkSize)
				{
					CheckSize();
				}
			}
		}

		#endregion Log line writing

		#region HelperFunctions

		/// <summary>
		/// Get the date and time string formatted for the log file.
		/// </summary>
		/// <returns>The formatted string</returns>
		private static string GetTime()
		{
			return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
		}

		/// <summary>
		/// Translate the log-level to text.
		/// </summary>
		/// <param name="logLevel">The log-level to translate</param>
		/// <returns>The translated log-level</returns>
		private static string LogLevelToText(LogLevel logLevel)
		{
			switch (logLevel)
			{
				case LogLevel.Debug: return "DEBUG";
				case LogLevel.Info: return "INFO";
				case LogLevel.Warning: return "WARNING";
				case LogLevel.Error: return "ERROR";
				case LogLevel.Critical: return "CRITICAL";
				default: return "UNKNOWN";
			}
		}

		#endregion HelperFunctions

		#region OnLog event

		/// <inheritdoc />
		/// <summary>Occurs when a log line is written in the log file.
		/// </summary>
		public event EventHandler<LogEventArgs> OnLog;

		/// <summary>Inform all clients attached to OnLog that a log line has been written.
		/// </summary>
		/// <param name="logLine">The log line.</param>
		private void SignalLog(string logLine)
		{
			OnLog?.Invoke(this, new LogEventArgs(logLine));
		}

		#endregion OnLog event

		#region File size

		private readonly int mMaxFileSize;
		private bool mEnableArchiving;

		/// <summary>Checks the size of the log file and test if it needs to be archived.
		/// </summary>
		private void CheckSize()
		{
			if (mEnableArchiving)
			{
				var logFileInfo = new FileInfo(FileName);
				if (logFileInfo.Length > mMaxFileSize)
				{
					WriteLine($"Current file size {logFileInfo.Length.ToString()} exceeds max file size of {mMaxFileSize.ToString()}", LogLevel.Info, false);
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

			var archivedFileName = FileName.Replace(".log", $".{DateTime.Now:yyyyMMdd.HHmmss}.log");
			var archiveName = FileName.Replace(".log", ".zip");

			try
			{
				// Rename the log file
				File.Move(FileName, archivedFileName);

				using (var zipToOpen = new FileStream(archiveName, FileMode.OpenOrCreate))
				{
					using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
					{
						var readmeEntry = archive.CreateEntry(archivedFileName);
						using (var zipEntryStream = readmeEntry.Open())
						{
							using (var logFileStream = new FileStream(archivedFileName, FileMode.Open))
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
				WriteLine($"Error archiving log file {e.Message}.\r\nArchiving is disabled.", LogLevel.Error);
			}
			finally
			{
				// Reopen the file stream
				mFile = new StreamWriter(FileName, true) { AutoFlush = true };
				WriteLine($"Continuing log file. Previous log file is {archivedFileName}", LogLevel.Info);
			}
		}

		#endregion File size

		#region Shortcuts

		/// <summary>Write a critical error in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Critical(string message)
		{
			WriteLine(message, LogLevel.Critical);
		}

		/// <summary>Write an error in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Error(string message)
		{
			WriteLine(message, LogLevel.Error);
		}

		/// <summary>Write a warning in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Warning(string message)
		{
			WriteLine(message, LogLevel.Warning);
		}

		/// <summary>Write information in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Info(string message)
		{
			WriteLine(message, LogLevel.Info);
		}

		/// <summary>Write a debug message in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		public void Debug(string message)
		{
			WriteLine(message, LogLevel.Debug);
		}

		#endregion Shortcuts
	}
}