namespace PL.Common.Settings
{
	/// <summary>Allows unit testing of file system calls by adding a level of indirection.
	/// </summary>
	public interface IFileSystemService
	{
		/// <summary>Opens a text file, reads all lines of the file, and then closes the file.
		/// </summary>
		/// <param name="file">The file.</param>
		/// <returns></returns>
		string ReadAllText(string file);

		/// <summary>>Creates a new file, writes the specified string to the file, and then closes
		/// the file. If the target file already exists, it is overwritten.
		/// </summary>
		/// <param name="file">The file.</param>
		/// <param name="text">The text.</param>
		void WriteAllText(string file, string text);

		/// <summary>Gets the name of the process executable in the default application domain.
		/// </summary>
		/// <returns></returns>
		string GetEntryAssemblyName();

		/// <summary>Ask user to select a folder.</summary>
		/// <param name="rootFolder">The root folder.</param>
		/// <returns>The path of the folder first selected in the dialog box or the last folder
		/// selected by the user. The default is an empty string ("").</returns>
		string SelectFolderDialog(string rootFolder);
	}
}