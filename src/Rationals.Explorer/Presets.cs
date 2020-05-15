using System;
using System.Collections.Generic;
using System.Xml;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.CustomControls;


namespace Rationals.Explorer
{
    public partial class MainWindow
    {
        private void SavePresetsSettings(XmlWriter w) { 
            if (_menuPresetRecentItems.Count > 0) {
                int counter = 0;
                foreach (Avalonia.Controls.MenuItem item in _menuPresetRecentItems) {
                    if (++counter <= _recentPresetMaxCount) {
                        w.WriteElementString("recentPreset", item.Name);
                    }
                }
            }
            if (_currentPresetPath != null) {
                w.WriteElementString("currentPresetPath", _currentPresetPath);
            }
            w.WriteElementString("currentPresetChanged", (_currentPresetChanged ? 1 : 0).ToString());
        }

        private void LoadPresetsSettings(XmlReader r)  // this procedure fills menu of main form
        {
            var recentPresets = new List<string>();

            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "recentPreset":
                            recentPresets.Add(r.ReadElementContentAsString());
                            break;
                        case "currentPresetPath":
                            _currentPresetPath = r.ReadElementContentAsString();
                            break;
                        case "currentPresetChanged":
                            MarkPresetChanged(r.ReadElementContentAsInt() != 0); // _currentPresetPath already set - so call MarkPresetChanged now
                            break;
                    }
                }
            }

            // Fill recent presets menu
            SetRecentPresets(recentPresets.ToArray());
        }

        private void SavePreset(XmlWriter w) {
            // drawer
            w.WriteStartElement("drawer");
            DrawerSettings.Save(_drawerSettings, w);
            w.WriteEndElement();
            // viewport
            w.WriteStartElement("viewport");
            SavePresetViewport(w);
            w.WriteEndElement();
            // Sound settings
            w.WriteStartElement("sound");
            WriteSoundSettings(w, _soundSettings);
            w.WriteEndElement();
        }

        private void LoadPreset(XmlReader r) {
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
                        case "sound":
                            _soundSettings = ReadSoundSettings(r.ReadSubtree());
                            break;
                    }
                }
            }
        }

        protected void ResetPreset() {
            // reset all preset components (viewport, drawer, sound)
            _drawerSettings = DrawerSettings.Reset();
            ResetPresetViewport();
            ResetPresetSoundSettings();
            //
            _currentPresetPath = null;
            _currentPresetChanged = false;
        }

        private static readonly FileDialogFilter[] _fileDialogFilters = new[] {
            new FileDialogFilter() { Name = "Xml files", Extensions = {"xml"} }
        };
        private async Task OpenPreset() {
            var dialog = new OpenFileDialog { Title = "Open Preset" };
            dialog.Filters.AddRange(_fileDialogFilters);
            string[] result = await dialog.ShowAsync(this); // await Open Dialog
            if (result != null && result.Length > 0) {
                string presetPath = result[0];
                LoadPreset(presetPath);
            }
        }
        private async Task SavePreset(bool withNewName) {
            string presetPath = null;
            if (_currentPresetPath != null && !withNewName) {
                presetPath = _currentPresetPath;
            } else {
                var dialog = new SaveFileDialog { Title = "Save Preset As" };
                dialog.Filters.AddRange(_fileDialogFilters);
                if (_currentPresetPath != null) {
                    dialog.Directory = System.IO.Path.GetDirectoryName(_currentPresetPath);
                    dialog.InitialFileName = System.IO.Path.GetFileName(_currentPresetPath);
                }
                presetPath = await dialog.ShowAsync(this);
            }
            if (presetPath != null) {
                if (SavePreset(presetPath)) {
                    _currentPresetPath = presetPath;
                    PopRecentPreset(_currentPresetPath);
                    MarkPresetChanged(false);
                }
            }
        }

        private async Task<bool> SaveChangedPreset() {
            if (!_currentPresetChanged) return true; // continue
            string message = (_currentPresetPath ?? "Unnamed") + " preset has unsaved changes.\r\nSave preset?";
            var result = await MessageBox.Show(this, message, "Rationals Explorer", MessageBox.MessageBoxButtons.YesNoCancel);
            if (result == MessageBox.MessageBoxResult.Cancel) return false; // cancel current operation
            if (result == MessageBox.MessageBoxResult.Yes) {
                await SavePreset(withNewName: false);
            }
            return true; // continue
        }

        private bool SavePreset(string presetPath) {
            using (XmlWriter w = XmlWriter.Create(presetPath, _xmlWriterSettings)) {
                w.WriteStartDocument();
                w.WriteStartElement("preset");
                SavePreset(w);
                w.WriteEndElement();
                w.WriteEndDocument();
            }
            return true;
        }
        private void LoadPreset(string presetPath) {
            bool presetLoaded = false;
            try {
                using (XmlTextReader r = new XmlTextReader(presetPath)) {
                    while (r.Read()) {
                        if (r.NodeType == XmlNodeType.Element && r.Name == "preset") {
                            LoadPreset(r);
                            presetLoaded = true;
                        }
                    }
                }
            } catch (Exception ex) {
                string message = "Can't open preset '" + presetPath + "':\r\n" + ex.Message;
                MessageBox.Show(this, message, "Rationals Explorer", MessageBox.MessageBoxButtons.Ok);
                presetLoaded = false;
            }

            // update presets menu at once
            if (presetLoaded) {
                _currentPresetPath = presetPath;
                PopRecentPreset(_currentPresetPath);
                MarkPresetChanged(false);
            } else {
                // invalid preset path - remove from "recent" list
                RemoveRecentPreset(presetPath);
            }
        }
        private void SavePresetViewport(XmlWriter w) {
            var scale  = _viewport.GetScaleSaved();
            var center = _viewport.GetUserCenter();
            w.WriteElementString("scaleX",  scale .X.ToString());
            w.WriteElementString("scaleY",  scale .Y.ToString());
            w.WriteElementString("centerX", center.X.ToString());
            w.WriteElementString("centerY", center.Y.ToString());
        }
        private void LoadPresetViewport(XmlReader r) {
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
    }

}