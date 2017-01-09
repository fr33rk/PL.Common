using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace PL.Common.Settings
{
	/// <summary> Interface for the FileSystem Service
	/// </summary>
	public class FileSystemService : IFileSystemService
	{
		/// <summary>Opens a text file, reads all lines of the file, and then closes the file.
		///
		/// Parameters:
		///   path:
		///     The file to open for reading.
		///
		/// Returns:
		///     A string containing all lines of the file.
		///
		/// Exceptions:
		///   T:System.ArgumentException:
		///     path is a zero-length string, contains only white space, or contains one or more
		///     invalid characters as defined by System.IO.Path.InvalidPathChars.
		///
		///   T:System.ArgumentNullException:
		///     path is null.
		///
		///   T:System.IO.PathTooLongException:
		///     The specified path, file name, or both exceed the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must be less than 248 characters,
		///     and file names must be less than 260 characters.
		///
		///   T:System.IO.DirectoryNotFoundException:
		///     The specified path is invalid (for example, it is on an unmapped drive).
		///
		///   T:System.IO.IOException:
		///     An I/O error occurred while opening the file.
		///
		///   T:System.UnauthorizedAccessException:
		///     path specified a file that is read-only.-or- This operation is not supported
		///     on the current platform.-or- path specified a directory.-or- The caller does
		///     not have the required permission.
		///
		///   T:System.IO.FileNotFoundException:
		///     The file specified in path was not found.
		///
		///   T:System.NotSupportedException:
		///     path is in an invalid format.
		///
		///   T:System.Security.SecurityException:
		///     The caller does not have the required permission.
		/// </summary>
		/// <param name="file">The file.</param>
		/// <returns></returns>
		public string ReadAllText(string file)
		{
			return File.ReadAllText(file);
		}

		/// <summary>Creates a new file, writes the specified string to the file, and then closes
		///     the file. If the target file already exists, it is overwritten.
		///
		/// Parameters:
		///   path:
		///     The file to write to.
		///
		///   contents:
		///     The string to write to the file.
		///
		/// Exceptions:
		///   T:System.ArgumentException:
		///     path is a zero-length string, contains only white space, or contains one or more
		///     invalid characters as defined by System.IO.Path.InvalidPathChars.
		///
		///   T:System.ArgumentNullException:
		///     path is null or contents is empty.
		///
		///   T:System.IO.PathTooLongException:
		///     The specified path, file name, or both exceed the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must be less than 248 characters,
		///     and file names must be less than 260 characters.
		///
		///   T:System.IO.DirectoryNotFoundException:
		///     The specified path is invalid (for example, it is on an unmapped drive).
		///
		///   T:System.IO.IOException:
		///     An I/O error occurred while opening the file.
		///
		///   T:System.UnauthorizedAccessException:
		///     path specified a file that is read-only.-or- This operation is not supported
		///     on the current platform.-or- path specified a directory.-or- The caller does
		///     not have the required permission.
		///
		///   T:System.NotSupportedException:
		///     path is in an invalid format.
		///
		///   T:System.Security.SecurityException:
		///     The caller does not have the required permission.</summary>
		/// <param name="file">The file.</param>
		/// <param name="text">The text.</param>
		public void WriteAllText(string file, string text)
		{
			File.WriteAllText(file, text);
		}

		/// <summary>Gets the name of the process executable in the default application domain.
		/// </summary>
		/// <returns></returns>
		public string GetEntryAssemblyName()
		{
			return Assembly.GetEntryAssembly().GetName().Name;
		}

		/// <summary>Ask user to select a folder.</summary>
		/// <param name="rootFolder">The root folder.</param>
		/// <returns>The path of the folder first selected in the dialog box or the last folder
		/// selected by the user. The default is an empty string ("").</returns>
		public string SelectFolderDialog(string rootFolder)
		{
			var folderBrowserDialog = new FolderBrowserDialog
			{
				SelectedPath = rootFolder
			};

			var result = folderBrowserDialog.ShowDialog();

			if (result == DialogResult.OK)
				return folderBrowserDialog.SelectedPath;
			else
				return rootFolder;
		}
	}
}