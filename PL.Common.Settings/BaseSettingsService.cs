using System;
using System.IO;
using Newtonsoft.Json;
using PL.Logger;

namespace PL.Common.Settings
{
	public abstract class BaseSettingsService<TSettings>
		where TSettings : class
	{
		private readonly IFileSystemService mFileService;
		private readonly ILogFile mLogger;
		private readonly string mSettingsFileName;

		/// <summary>Gets or sets the settings.</summary>
		public TSettings Settings { get; set; }

		/// <summary>Initializes a new instance of the <see cref="BaseSettingsService{TSettings}"/> class.
		/// </summary>
		/// <param name="fileService">The file service.</param>
		/// <param name="logger">The logger.</param>
		protected BaseSettingsService(IFileSystemService fileService, ILogFile logger)
		{
			mFileService = fileService;
			mLogger = logger;

			// All settings files are stored in the same location with a different filename.
			var filename = mFileService.GetEntryAssemblyName() + "Settings.json";
			mSettingsFileName = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"PanteraLeo", "MBC",
				filename
				);

			LoadSettings();
		}

		/// <summary>Loads the settings.</summary>
		public void LoadSettings()
		{
			try
			{
				var jsonString = mFileService.ReadAllText(mSettingsFileName);
				if (!string.IsNullOrEmpty(jsonString))
					Settings = JsonConvert.DeserializeObject<TSettings>(jsonString, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Populate });
				else
					mLogger?.Warning($"Settings file not found at location '{mSettingsFileName}'.");
			}
			catch (Exception ex)
			{
				// No (valid) file found, use default settings.
				mLogger?.Warning($"Could not load settings: {ex.Message}");
			}

			if (Settings == null)
			{
				Settings = JsonConvert.DeserializeObject<TSettings>("{}", new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Populate });
				mLogger?.Warning("Default settings loaded.");
				SaveSettings();
			}

#if CONFIGDEBUG

			SaveSettings(); // Save debug settings

#endif
		}

		/// <summary>Saves the settings.</summary>
		public void SaveSettings()
		{
			try
			{
				var jsonString = JsonConvert.SerializeObject(Settings, Formatting.Indented);
				mFileService.WriteAllText(mSettingsFileName, jsonString);
			}
			catch (Exception ex)
			{
				mLogger?.Warning($"Could not write settings to: {mSettingsFileName}.\n{ex.Message}");
			}
		}
	}
}