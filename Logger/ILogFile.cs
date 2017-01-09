using System;

namespace PL.Logger
{
	/// <summary>General log file interface for .net applications.</summary>
	public interface ILogFile
	{
		/// <summary>Occurs when a log line is written in the log file.
		/// </summary>
		event EventHandler<LogEventArgs> OnLog;

		/// <summary>Write a critical error in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		void Critical(string message);

		/// <summary>Write a debug message in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		void Debug(string message);

		/// <summary>Write an error in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		void Error(string message);

		/// <summary>Write information in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		void Info(string message);

		/// <summary>Write a warning in the log file.
		/// </summary>
		/// <param name="message">The message.</param>
		void Warning(string message);

		/// <summary>Write the log end line
		/// </summary>
		void WriteLogEnd();

		/// <summary>Write the log start line.
		/// </summary>
		void WriteLogStart();
	}
}