using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Rationals.Drawing;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Rationals.Explorer.Blazor
{
	public partial class ExplorerPage
	{
		string? _currentPresetName = null;
		bool _currentPresetChanged = false;
		
		InputFile? _presetInputFile;

		string[] DemoPresetNames = [ // List of demo preset names: wwwroot/presets/*.xml
			"5 EDO",
			"12 EDO",
			"19 EDO",
			"53 EDO",
			"Bohlen-Pierce scale",
		];

		List<string> PresetNames = new(); // List of local storage preset names
		
		// Local storage keys
		static string GetPresetKeyName(string presetName) { return $"rationals_preset_{presetName}"; }
		static readonly string PresetNamesKey = "rationals_presets";

		void OnPresetLoaded() {
			// Preset settings (viewport, drawer) were loaded (preset was reset or loaded).
			// Now propagate new settings to form controls & services.

			// Drawer
			SetSettingsToControls();
			_gridDrawer.SetBounds(_viewport.GetUserBounds());   // viewport       -> gridDrawer
			_drawerSettings.UpdateDrawer(_gridDrawer);          // drawerSettings -> gridDrawer

			UpdateSubgroupTip();            // gui <- gridDrawer.Subgroup (tip or error)
			UpdateTemperamentRowErrors();   // gui <- gridDrawer.Temperament (row errors)

			StateHasChanged();    // Invalidate gui
			InvalidateCanvas();   // Invalidate drawer canvas
		}

		async void SavePresetNames() {
			string json = JsonSerializer.Serialize(PresetNames);
			await JS.InvokeVoidAsync("localStorage.setItem", PresetNamesKey, json);
		}

		async void LoadPresetNames() {
			string? json = await JS.InvokeAsync<string>("localStorage.getItem", PresetNamesKey);
			if (!string.IsNullOrEmpty(json)) {
				PresetNames = new(JsonSerializer.Deserialize<string[]>(json) ?? []);
			}
		}

		void ResetPreset() {
			// reset all preset components (viewport, drawer)
			_drawerSettings = DrawerSettings.Reset();
			ResetPresetViewport();
			//
			_currentPresetName = null;
			_currentPresetChanged = false;
		}

		void SavePreset(XmlWriter w) {
			w.WriteStartDocument();
			w.WriteStartElement("preset");

			// drawer
			w.WriteStartElement("drawer");
			DrawerSettings.Save(_drawerSettings, w);
			w.WriteEndElement();

			// viewport
			w.WriteStartElement("viewport");
			SavePresetViewport(w);
			w.WriteEndElement();

			w.WriteEndElement();
			w.WriteEndDocument();
		}

		byte[] SavePresetXml() {
			var settings = new XmlWriterSettings {
				Indent = true,
				OmitXmlDeclaration = true,
			};
			using var memoryStream = new MemoryStream();
			using (XmlWriter writer = XmlWriter.Create(memoryStream, settings)) {
				SavePreset(writer);
			}
			return memoryStream.ToArray();
		}

		void LoadPreset(XmlReader r) {
			// read preset from App Settings or saved preset xml
			while (r.Read()) {
				if (r.NodeType == XmlNodeType.Element) {
					switch (r.Name) {
						case "drawer":
							_drawerSettings = DrawerSettings.Load(r.ReadSubtree());
							break;
						case "viewport":
							LoadPresetViewport(r.ReadSubtree());
							break;
					}
				}
			}
		}

		async Task<bool> CancelIfUnsaved() {
			if (!_currentPresetChanged) return false;
			bool? result = await DialogService.ShowMessageBoxAsync(
				"Warning",
				"Current preset is not saved. Discard the changes?",
				yesText: "Discard", cancelText: "Cancel");
			return result != true;
		}

		#region Viewport
		void SavePresetViewport(XmlWriter w) {
			var scale  = _viewport.GetScaleSaved();
			var center = _viewport.GetUserCenter();
			w.WriteElementString("scaleX",  scale .X.ToString());
			w.WriteElementString("scaleY",  scale .Y.ToString());
			w.WriteElementString("centerX", center.X.ToString());
			w.WriteElementString("centerY", center.Y.ToString());
		}
		void LoadPresetViewport(XmlReader r) {
			float sx = 1, sy = 1; // scale
			float cx = 0, cy = 0; // center
			while (r.Read()) {
				if (r.NodeType == XmlNodeType.Element) {
					switch (r.Name) {
						case "scaleX":  sx = r.ReadElementContentAsFloat(); break;
						case "scaleY":  sy = r.ReadElementContentAsFloat(); break;
						case "centerX": cx = r.ReadElementContentAsFloat(); break;
						case "centerY": cy = r.ReadElementContentAsFloat(); break;
					}
				}
			}
			// keep initial viewport size, change scale and center only
			_viewport.SetScaleSaved(sx, sy);
			_viewport.SetUserCenter(cx, cy);
		}
		private void ResetPresetViewport() {
			_viewport.SetScaleSaved(1f, 1f);
			_viewport.SetUserCenter(0f, 0f);
		}
		#endregion Viewport

		#region Main functions

		void MarkPresetChanged(bool changed = true) {
			_currentPresetChanged = changed;
		}

		async void ResetCurrentPreset() {
			if (await CancelIfUnsaved()) return;

			// Reset preset
			ResetPreset();

			// Propagate new settings to form controls & drawer
			OnPresetLoaded();
		}

		async void SaveCurrentPreset() {
			if (string.IsNullOrWhiteSpace(_currentPresetName)) {
				await DialogService.ShowMessageBoxAsync("Error", "Preset name is empty.", yesText: "OK");
				return;
			}

			string presetName = _currentPresetName;

			// Save preset data
			string xml = Encoding.UTF8.GetString(SavePresetXml());
			await JS.InvokeVoidAsync("localStorage.setItem", GetPresetKeyName(presetName), xml);

			_currentPresetChanged = false;

			// Update presets list
			PresetNames.Remove(presetName);
			PresetNames.Insert(0, presetName);
			SavePresetNames();

			// Force GUI update
			StateHasChanged();
		}

		void LoadPreset(string presetName, string presetXml) { // Load preset xml from local storage or imported file
			bool presetLoaded = false;
			
			// Load preset data
			presetXml = presetXml.Trim('\uFEFF'); // Remove BOM if present
			try {
				using (StringReader stringReader = new StringReader(presetXml))
				using (XmlReader r = XmlReader.Create(stringReader)) {
					while (r.Read()) {
						if (r.NodeType == XmlNodeType.Element && r.Name == "preset") {
							ResetPreset();
							LoadPreset(r);
							presetLoaded = true;
						}
					}
				}
			} catch (Exception ex) {
				Console.WriteLine($"Can't open preset '{presetName}': {ex}");
			}
			
			// Update caption and presets menu at once
			if (presetLoaded) {
				_currentPresetName = presetName;
				MarkPresetChanged(false);
				PresetNames.Remove(presetName);
				PresetNames.Insert(0, presetName);

				OnPresetLoaded();
			}
		}

		async void LoadDemoPreset(string presetName) {
			if (await CancelIfUnsaved()) return;

			try {
				string xmlContent = await Http.GetStringAsync($"presets/{presetName}.xml");
				LoadPreset(presetName, xmlContent);
			}
			catch (Exception ex) {
				Console.WriteLine($"Could not load demo preset '{presetName}': {ex}");
			}
		}

		async void LoadPreset(string presetName) {
			if (await CancelIfUnsaved()) return;
			
			string? xml = await JS.InvokeAsync<string>("localStorage.getItem", GetPresetKeyName(presetName));
			if (xml != null) {
				LoadPreset(presetName, xml);
			}
		}

		async void DeletePreset(string presetName) {
			// Remove preset data
			await JS.InvokeVoidAsync(
				"localStorage.removeItem",
				GetPresetKeyName(presetName)
			);
			// Update presets list
			PresetNames.Remove(presetName);
			SavePresetNames();
			//
			if (_currentPresetName == presetName) {
				_currentPresetChanged = true;
			}
			// Force GUI update
			StateHasChanged();
		}

		async void ExportCurrentPreset() {
			string base64Data = Convert.ToBase64String(SavePresetXml());

			await JS.InvokeVoidAsync(
				"downloadFileFromByteArray",
				$"{_currentPresetName ?? "rationals_preset"}.xml",
				"application/xml",
				base64Data
			);
		}

		async void TriggerImportPreset() {
			if (await CancelIfUnsaved()) return;

			await JS.InvokeVoidAsync("HTMLElement.prototype.click.call", _presetInputFile?.Element);
		}

		async void ImportPreset(InputFileChangeEventArgs e) { // called from <InputFile OnChange="ImportPreset" /> when user selects a file
			// Read uploaded file
			if (e.File == null) return;
			using var stream = e.File.OpenReadStream();
			using var reader = new StreamReader(stream);
			string xml = await reader.ReadToEndAsync();

			// Load preset
			string presetName = Path.GetFileNameWithoutExtension(e.File.Name);
			LoadPreset(presetName, xml);
		}

		#endregion Main functions
	}
}