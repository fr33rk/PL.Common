using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PL.Common.Settings
{
	/// <summary>Interface for the SettingsService.</summary>
	public interface ISettingsService
    {
		/// <summary>Loads the settings.</summary>
		void LoadSettings();

		/// <summary>Saves the settings.</summary>
		void SaveSettings();
	}
}
