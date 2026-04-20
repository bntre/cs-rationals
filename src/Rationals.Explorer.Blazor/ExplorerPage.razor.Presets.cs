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
		bool _currentPresetChanged = false; // local, demo or an unsaved preset was changed
		bool _descriptionShown = true;

		InputFile? _presetInputFile;

		string[] DemoPresetNames = [ // List of demo preset names: wwwroot/presets/*.xml
			"5 EDO",
			"12 EDO",
			"19 EDO",
			"53 EDO",
			"Bohlen-Pierce scale",
		];

		List<string> LocalPresetNames = new();
		
		// Local storage keys
		static string GetLocalPresetKeyName(string presetName) { return $"rationals_preset_{presetName}"; }
		static readonly string LocalPresetNamesKey = "rationals_presets";

		void OnPresetLoaded() {
			// Preset settings (viewport, drawer) were loaded (preset was reset or loaded).
			// Now propagate new settings to form controls & services.

			if (string.IsNullOrEmpty(_presetDescription)) {
				_descriptionShown = false;
			}

			// Drawer
			SetSettingsToControls();
			_gridDrawer.SetBounds(_viewport.GetUserBounds());   // viewport       -> gridDrawer
			_drawerSettings.UpdateDrawer(_gridDrawer);          // drawerSettings -> gridDrawer

			UpdateSubgroupTip();            // gui <- gridDrawer.Subgroup (tip or error)
			UpdateTemperamentRowErrors();   // gui <- gridDrawer.Temperament (row errors)

			StateHasChanged();    // Invalidate gui
			InvalidateCanvas();   // Invalidate drawer canvas
		}

		async void SaveLocalPresetNames() {
			string json = JsonSerializer.Serialize(LocalPresetNames);
			await JS.InvokeVoidAsync("localStorage.setItem", LocalPresetNamesKey, json);
		}

		async void LoadLocalPresetNames() {
			string? json = await JS.InvokeAsync<string>("localStorage.getItem", LocalPresetNamesKey);
			if (!string.IsNullOrEmpty(json)) {
				LocalPresetNames = new(JsonSerializer.Deserialize<string[]>(json) ?? []);
			}
		}

		static readonly string DefaultDescription =
			"Mouse controls:\n" +
			"Ctrl + Wheel - Zoom\n" +
			"Shift + Wheel - Stretch zoom\n" +
			"Alt + Wheel - Resize notes\n" +
			"Middle-drag - Pan the grid\n" +
			"Ctrl + Left-click - Change selection";

		void ResetPreset() {
			// reset all preset components (viewport, drawer)
			_drawerSettings = DrawerSettings.Reset();
			
			// set default description (mouse controls)
			_presetName = null;
			_presetDescription = DefaultDescription;
			
			ResetPresetViewport();
			//
			_currentPresetChanged = false;
		}

		void WritePresetXml(XmlWriter w) {
			w.WriteStartDocument();
			w.WriteStartElement("preset");

			// metadata
			if (!string.IsNullOrEmpty(_presetName))         w.WriteElementString("name",        _presetName);
			if (!string.IsNullOrEmpty(_presetDescription))  w.WriteElementString("description", _presetDescription);

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

		byte[] WritePresetXml() {
			var settings = new XmlWriterSettings {
				Indent = true,
				OmitXmlDeclaration = true,
			};
			using var memoryStream = new MemoryStream();
			using (XmlWriter writer = XmlWriter.Create(memoryStream, settings)) {
				WritePresetXml(writer);
			}
			return memoryStream.ToArray();
		}

		void ReadPresetXml(XmlReader r) {
			// read preset from App Settings or saved preset xml
			while (r.Read()) {
				if (r.NodeType == XmlNodeType.Element) {
					switch (r.Name) {
						case "name":
							_presetName = r.ReadElementContentAsString();
							break;
						case "description":
							_presetDescription = r.ReadElementContentAsString();
							break;
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

		async void SaveCurrentPresetLocally() {
			if (string.IsNullOrWhiteSpace(_presetName)) {
				await DialogService.ShowMessageBoxAsync("Error", "Preset name is empty.", yesText: "OK");
				return;
			}

			// Save preset data
			string xml = Encoding.UTF8.GetString(WritePresetXml());
			await JS.InvokeVoidAsync("localStorage.setItem", GetLocalPresetKeyName(_presetName), xml);

			_currentPresetChanged = false;

			// Update local preset list
			LocalPresetNames.Remove(_presetName);
			LocalPresetNames.Insert(0, _presetName);
			SaveLocalPresetNames();

			// Force GUI update
			StateHasChanged();
		}

		void LoadPreset(string alternateName, string presetXml, bool isLocal) { // Load preset xml from local storage or imported file
			bool presetLoaded = false;
			
			// Load preset data
			presetXml = presetXml.Trim('\uFEFF'); // Remove BOM if present
			try {
				using (StringReader stringReader = new StringReader(presetXml))
				using (XmlReader r = XmlReader.Create(stringReader)) {
					while (r.Read()) {
						if (r.NodeType == XmlNodeType.Element && r.Name == "preset") {
							ResetPreset();
							ReadPresetXml(r);
							if (string.IsNullOrWhiteSpace(_presetName)) {
								_presetName = alternateName;
							}
							presetLoaded = true;
						}
					}
				}
			} catch (Exception ex) {
				Console.WriteLine($"Can't open preset '{alternateName}': {ex}");
			}
			
			// Update caption and presets menu at once
			if (presetLoaded) {
				MarkPresetChanged(false);

				if (isLocal && !string.IsNullOrWhiteSpace(_presetName)) {
					LocalPresetNames.Remove(_presetName);
					LocalPresetNames.Insert(0, _presetName);
				}

				OnPresetLoaded(); // Update the GridDrawer and GUI
			}
		}

		async void LoadDemoPreset(string demoPresetName) {
			if (await CancelIfUnsaved()) return;

			try {
				string xmlContent = await Http.GetStringAsync($"presets/{demoPresetName}.xml");
				LoadPreset(demoPresetName, xmlContent, false);
			}
			catch (Exception ex) {
				Console.WriteLine($"Could not load demo preset '{demoPresetName}': {ex}");
			}
		}

		async void LoadLocalPreset(string presetName) {
			if (await CancelIfUnsaved()) return;
			
			string? xml = await JS.InvokeAsync<string>("localStorage.getItem", GetLocalPresetKeyName(presetName));
			if (xml != null) {
				LoadPreset(presetName, xml, true);
			}
		}

		async void DeleteLocalPreset(string presetName) {
			// Remove preset data
			await JS.InvokeVoidAsync(
				"localStorage.removeItem",
				GetLocalPresetKeyName(presetName)
			);
			// Update local preset list
			LocalPresetNames.Remove(presetName);
			SaveLocalPresetNames();
			//
			if (_presetName == presetName) { //!!! we should know here if it's local
				_currentPresetChanged = true;
			}
			// Force GUI update
			StateHasChanged();
		}

		async void ExportCurrentPreset() {
			string base64Data = Convert.ToBase64String(WritePresetXml());

			await JS.InvokeVoidAsync(
				"downloadFileFromByteArray",
				$"{_presetName ?? "rationals_preset"}.xml",
				"application/xml",
				base64Data
			);
		}

		#region URL Preset
		static byte[] DecodePresetFromUrl(string urlSafe) {
			urlSafe = Uri.UnescapeDataString(urlSafe); // Decode percent-encoding
			string base64 = urlSafe.Replace('-', '+').Replace('_', '/');
			switch (base64.Length % 4) {
				case 2: base64 += "=="; break;
				case 3: base64 += "="; break;
			}
			return Convert.FromBase64String(base64); // exception if invalid
		}
		static string EncodePresetToUrl(byte[] presetXml) {
			string base64 = Convert.ToBase64String(presetXml);
			string urlSafe = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
			return Uri.EscapeDataString(urlSafe);
		}

		void SavePresetAsUrl() {
			string urlSafe = EncodePresetToUrl(WritePresetXml());

			// Build new URI with fragment "#preset=<base64>"
			string baseUri = Navigation.Uri.Split('#')[0];
			string target = baseUri + "#preset=" + urlSafe;

			// Update address bar without adding history entry
			Navigation.NavigateTo(target, replace: true);
		}

		void LoadPresetFromUrl() {
			// Read fragment like "#preset=<base64>"
			var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
			string? frag = uri.Fragment?.TrimStart('#');
			if (string.IsNullOrEmpty(frag)) return;

			const string prefix = "preset=";
			if (frag.StartsWith(prefix)) {
				string encoded = frag.Substring(prefix.Length);
				try {
					byte[] data = DecodePresetFromUrl(encoded);
					string xml = Encoding.UTF8.GetString(data);
					LoadPreset("url-preset", xml, false);
				}
				catch (Exception ex) {
					Console.WriteLine($"Failed to decode preset from URL fragment: {ex}");
				}
			}
		}
		#endregion URL Preset

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
			string alternateName = Path.GetFileNameWithoutExtension(e.File.Name);
			LoadPreset(alternateName, xml, false);
		}

		#endregion Main functions
	}
}