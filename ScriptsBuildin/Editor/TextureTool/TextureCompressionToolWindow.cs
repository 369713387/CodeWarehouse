using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Sirenix.Utilities.Editor;

namespace YF.EditorTools
{
	public class TextureCompressionToolWindow : EditorToolBase
	{
		private class TextureEntry
		{
			public string assetPath;
			public string assetName;
			public Texture2D texture;
			public TextureImporter importer;
			public Texture preview;
			public int selectedFormatIndex;
		}

		private readonly List<TextureEntry> _allEntries = new List<TextureEntry>();
		private readonly List<TextureEntry> _filteredEntries = new List<TextureEntry>();
		private Vector2 _scrollPosition;
		private string _search = string.Empty;
		private bool _isScanning;

		private static readonly string[] PlatformOptions = new[] { "Standalone", "Android", "IOS", "WebGL" };
		private int _selectedPlatformIndex = 0; // Standalone

		// Batch apply state
		private int _batchFormatIndex = 0;
		private List<(string label, TextureImporterFormat format)> _currentFormatChoices = new List<(string, TextureImporterFormat)>();

		// Responsive column widths
		private float _colPreviewW = 70f;
		private float _colNameW = 220f;
		private float _colFormatW = 200f;
		private float _colActionsW = 300f;
		private const float _colGap = 6f;

		public override string ToolName => "Texture Compression Tool";
		public override Vector2Int WinSize => new Vector2Int(760, 420);	

		private void ComputeColumnWidths()
		{
			float windowWidth = position.width;
			float padding = 40f; // approximate padding from boxes/scroll
			float total = Mathf.Max(600f, windowWidth - padding);

			float preview = 64f;
			float name = Mathf.Clamp(total * 0.20f, 160f, 360f);
			float format = Mathf.Clamp(total * 0.18f, 140f, 320f);
			float actions = Mathf.Clamp(total * 0.22f, 240f, 420f);

			float fixedSum = preview + name + format + actions;
			float path = Mathf.Max(120f, total - fixedSum);
			// Path column is flexible; others are set below

			_colPreviewW = preview;
			_colNameW = name;
			_colFormatW = format;
			_colActionsW = actions;
		}

		[MenuItem("Tools/Texture Compression Tool")]
		public static void Open()
		{
			var window = GetWindow<TextureCompressionToolWindow>(true, "Texture Compression Tool");
			window.minSize = new Vector2(760, 420);
			window.Show();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			Scan();
		}

		protected override void OnImGUI()
		{
			base.OnImGUI();

			ComputeColumnWidths();

			SirenixEditorGUI.Title("Texture Compression Tool", null, TextAlignment.Left, true);

			SirenixEditorGUI.BeginBox();
			DrawToolbar();
			SirenixEditorGUI.EndBox();

			EditorGUILayout.Space();

			SirenixEditorGUI.BeginBox();
			DrawHeader();
			DrawList();
			SirenixEditorGUI.EndBox();
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				// Platform selector
				var newPlatformIndex = EditorGUILayout.Popup(_selectedPlatformIndex, PlatformOptions, GUILayout.Width(140));
				if (newPlatformIndex != _selectedPlatformIndex)
				{
					_selectedPlatformIndex = newPlatformIndex;
					RebuildFormatChoices();
					ResetPerItemFormatSelection();
				}

				GUILayout.Space(8);

				// Search
				GUILayout.Label("Search:", GUILayout.Width(55));
				var newSearch = GUILayout.TextField(_search, EditorStyles.toolbarTextField, GUILayout.MinWidth(180));
				if (!string.Equals(newSearch, _search, StringComparison.Ordinal))
				{
					_search = newSearch;
					ApplyFilter();
				}

				GUILayout.FlexibleSpace();

				// Refresh/rescan buttons
				if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
				{
					RefreshPreviews();
				}
				if (GUILayout.Button(_isScanning ? "Scanning..." : "Rescan", EditorStyles.toolbarButton, GUILayout.Width(70)))
				{
					Scan();
				}
			}

			// Batch apply bar
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUI.BeginDisabledGroup(_selectedPlatformIndex == 0); // Default platform cannot directly set format
				GUILayout.Label("Batch preset:", GUILayout.Width(90));
				EnsureFormatChoices();
				var formatLabels = _currentFormatChoices.Select(c => c.label).ToArray();
				_batchFormatIndex = EditorGUILayout.Popup(_batchFormatIndex, formatLabels, GUILayout.Width(220));
				if (GUILayout.Button("Apply to filtered (current platform)", GUILayout.Width(250)))
				{
					ApplyBatchFormatToFiltered(_currentFormatChoices[_batchFormatIndex].format);
				}
				EditorGUI.EndDisabledGroup();

				GUILayout.Space(12);
				if (GUILayout.Button("Clear override on filtered", GUILayout.Width(180)))
				{
					ClearOverrideOnFiltered();
				}
			}
		}

		private void DrawHeader()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("Preview", GUILayout.Width(_colPreviewW));
				GUILayout.Space(_colGap);
				GUILayout.Label("Name", GUILayout.Width(_colNameW));
				GUILayout.Label("Path");
				GUILayout.Label("Current Format", GUILayout.Width(_colFormatW));
				GUILayout.Label("Actions", GUILayout.Width(_colActionsW));
			}
			EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
		}

		private void DrawList()
		{
			using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
			{
				_scrollPosition = scroll.scrollPosition;
				foreach (var entry in _filteredEntries)
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						var previewRect = GUILayoutUtility.GetRect(_colPreviewW, 64, GUILayout.Width(_colPreviewW), GUILayout.Height(64));
						Texture texForPreview = entry.preview;
						if (texForPreview == null && entry.texture != null)
						{
							// Try large preview first
							texForPreview = AssetPreview.GetAssetPreview(entry.texture);
							// Fallback to mini thumbnail if still null
							if (texForPreview == null)
							{
								texForPreview = AssetPreview.GetMiniThumbnail(entry.texture);
							}
							// Cache if available
							if (texForPreview != null)
							{
								entry.preview = texForPreview;
							}
							// If still loading, repaint to update when ready
							if (AssetPreview.IsLoadingAssetPreview(entry.texture.GetInstanceID()))
							{
								Repaint();
							}
						}
						EditorGUI.DrawPreviewTexture(previewRect, texForPreview ? texForPreview : Texture2D.grayTexture);
						GUILayout.Space(_colGap);
						GUILayout.Label(entry.assetName, GUILayout.Width(_colNameW));
						GUILayout.Label(entry.assetPath);

						// Current format display
						GUILayout.Label(GetCurrentFormatLabel(entry.importer), GUILayout.Width(_colFormatW));

						// Actions: settings
						using (new EditorGUILayout.HorizontalScope(GUILayout.Width(_colActionsW)))
						{
							if (GUILayout.Button("Settings", GUILayout.Width(70)))
							{
								ShowSettingsMenu(entry);
							}
						}
					}
					EditorGUILayout.Space(2);
				}
			}
		}

		private void ShowSettingsMenu(TextureEntry entry)
		{
			var importer = entry.importer;
			var menu = new GenericMenu();

			// Format (per platform)
			EnsureFormatChoices();
			var platformName = GetPlatformName();
			menu.AddSeparator("");
			if (string.IsNullOrEmpty(platformName))
			{
				menu.AddDisabledItem(new GUIContent("Format/Select a platform in toolbar"));
			}
			else
			{
				var currentSettings = importer.GetPlatformTextureSettings(platformName);
				for (int i = 0; i < _currentFormatChoices.Count; i++)
				{
					var choice = _currentFormatChoices[i];
					bool on;
					if (currentSettings == null || !currentSettings.overridden)
					{
						on = choice.format == TextureImporterFormat.Automatic;
					}
					else
					{
						on = currentSettings.format == choice.format;
					}
					var fmt = choice.format;
					menu.AddItem(new GUIContent($"Format/{choice.label}"), on, () =>
					{
						ApplyFormatForEntry(entry, fmt);
					});
				}
				menu.AddItem(new GUIContent("Format/Clear Override"), false, () =>
				{
					ClearOverride(importer);
				});
			}

			// Toggles
			menu.AddItem(new GUIContent("Read/Write Enabled"), importer.isReadable, () =>
			{
				ApplyImporterChange(entry, imp => imp.isReadable = !imp.isReadable);
			});
			menu.AddItem(new GUIContent("sRGB (Color Texture)"), importer.sRGBTexture, () =>
			{
				ApplyImporterChange(entry, imp => imp.sRGBTexture = !imp.sRGBTexture);
			});
			menu.AddItem(new GUIContent("Alpha is Transparency"), importer.alphaIsTransparency, () =>
			{
				ApplyImporterChange(entry, imp => imp.alphaIsTransparency = !imp.alphaIsTransparency);
			});
			menu.AddItem(new GUIContent("Generate Mip Maps"), importer.mipmapEnabled, () =>
			{
				ApplyImporterChange(entry, imp => imp.mipmapEnabled = !imp.mipmapEnabled);
			});

			menu.AddSeparator("");

			// Filter Mode
			menu.AddItem(new GUIContent("Filter Mode/Point"), importer.filterMode == FilterMode.Point, () =>
			{
				ApplyImporterChange(entry, imp => imp.filterMode = FilterMode.Point);
			});
			menu.AddItem(new GUIContent("Filter Mode/Bilinear"), importer.filterMode == FilterMode.Bilinear, () =>
			{
				ApplyImporterChange(entry, imp => imp.filterMode = FilterMode.Bilinear);
			});
			menu.AddItem(new GUIContent("Filter Mode/Trilinear"), importer.filterMode == FilterMode.Trilinear, () =>
			{
				ApplyImporterChange(entry, imp => imp.filterMode = FilterMode.Trilinear);
			});

			// Wrap Mode
			menu.AddItem(new GUIContent("Wrap Mode/Repeat"), importer.wrapMode == TextureWrapMode.Repeat, () =>
			{
				ApplyImporterChange(entry, imp => imp.wrapMode = TextureWrapMode.Repeat);
			});
			menu.AddItem(new GUIContent("Wrap Mode/Clamp"), importer.wrapMode == TextureWrapMode.Clamp, () =>
			{
				ApplyImporterChange(entry, imp => imp.wrapMode = TextureWrapMode.Clamp);
			});
			menu.AddItem(new GUIContent("Wrap Mode/Mirror"), importer.wrapMode == TextureWrapMode.Mirror, () =>
			{
				ApplyImporterChange(entry, imp => imp.wrapMode = TextureWrapMode.Mirror);
			});

			// Max Size presets
			int[] sizes = new[] { 256, 512, 1024, 2048, 4096, 8192 };
			for (int i = 0; i < sizes.Length; i++)
			{
				int size = sizes[i];
				menu.AddItem(new GUIContent($"Max Size/{size}"), importer.maxTextureSize == size, () =>
				{
					ApplyImporterChange(entry, imp => imp.maxTextureSize = size);
				});
			}

			// NPOT Scale
			menu.AddSeparator("");
			AddNpotItem(menu, entry, "NPOT/None", TextureImporterNPOTScale.None);
			AddNpotItem(menu, entry, "NPOT/ToNearest", TextureImporterNPOTScale.ToNearest);
			AddNpotItem(menu, entry, "NPOT/ToLarger", TextureImporterNPOTScale.ToLarger);
			AddNpotItem(menu, entry, "NPOT/ToSmaller", TextureImporterNPOTScale.ToSmaller);

			menu.ShowAsContext();
		}

		private void AddNpotItem(GenericMenu menu, TextureEntry entry, string label, TextureImporterNPOTScale value)
		{
			var importer = entry.importer;
			menu.AddItem(new GUIContent(label), importer.npotScale == value, () =>
			{
				ApplyImporterChange(entry, imp => imp.npotScale = value);
			});
		}

		private void ApplyImporterChange(TextureEntry entry, Action<TextureImporter> change)
		{
			var importer = entry.importer;
			change(importer);
			EditorUtility.SetDirty(importer);
			importer.SaveAndReimport();
			entry.preview = null; // force refresh preview after reimport
			EditorApplication.delayCall += RefreshPreviews;
			Repaint();
		}

		private void Update()
		{
			// Drive repaint while some previews are still being generated
			bool anyLoading = false;
			for (int i = 0; i < _filteredEntries.Count; i++)
			{
				var tex = _filteredEntries[i].texture;
				if (tex != null && AssetPreview.IsLoadingAssetPreview(tex.GetInstanceID()))
				{
					anyLoading = true;
					break;
				}
			}
			if (anyLoading)
			{
				Repaint();
			}
		}

		private void Scan()
		{
			_isScanning = true;
			try
			{
				_allEntries.Clear();
				_filteredEntries.Clear();

				var guids = AssetDatabase.FindAssets("t:Texture2D");
				foreach (var guid in guids)
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var importer = AssetImporter.GetAtPath(path) as TextureImporter;
					if (importer == null)
						continue;

					var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
					var entry = new TextureEntry
					{
						assetPath = path,
						assetName = System.IO.Path.GetFileNameWithoutExtension(path),
						texture = tex,
						importer = importer,
						preview = AssetPreview.GetAssetPreview(tex),
						selectedFormatIndex = 0
					};
					_allEntries.Add(entry);
				}

				ApplyFilter();
				RebuildFormatChoices();
			}
			finally
			{
				_isScanning = false;
			}
		}

		private void RefreshPreviews()
		{
			foreach (var entry in _allEntries)
			{
				if (entry.texture != null)
				{
					entry.preview = AssetPreview.GetAssetPreview(entry.texture);
				}
			}
			Repaint();
		}

		private void ApplyFilter()
		{
			_filteredEntries.Clear();
			if (string.IsNullOrEmpty(_search))
			{
				_filteredEntries.AddRange(_allEntries);
			}
			else
			{
				var lower = _search.ToLowerInvariant();
				_filteredEntries.AddRange(_allEntries.Where(e => e.assetName.ToLowerInvariant().Contains(lower) || e.assetPath.ToLowerInvariant().Contains(lower)));
			}
		}

		private string GetPlatformName()
		{
			// Unity platform names used by TextureImporterPlatformSettings
			var option = PlatformOptions[_selectedPlatformIndex];
			switch (option)
			{
				case "Standalone": return "Standalone";
				case "Android": return "Android";
				case "iPhone": return "iPhone";
				case "WebGL": return "WebGL";
				case "tvOS": return "tvOS";
				default: return null; // Default
			}
		}

		private string GetCurrentFormatLabel(TextureImporter importer)
		{
			var platformName = GetPlatformName();
			if (string.IsNullOrEmpty(platformName))
			{
				// Default summary
				return $"Default ({importer.textureCompression})";
			}

			var settings = importer.GetPlatformTextureSettings(platformName);
			if (settings == null)
				return "-";

			if (!settings.overridden)
				return "Auto (Default)";

			return settings.format.ToString();
		}

		private void EnsureFormatChoices()
		{
			if (_currentFormatChoices.Count == 0)
			{
				RebuildFormatChoices();
			}
		}

		private void RebuildFormatChoices()
		{
			_currentFormatChoices.Clear();
			var platformName = GetPlatformName();
			if (string.IsNullOrEmpty(platformName))
			{
				// Default platform: nothing to set directly (use Clear Override)
				_currentFormatChoices.Add(("Automatic", TextureImporterFormat.Automatic));
				_currentFormatChoices.Add(("RGBA32 (Uncompressed)", TextureImporterFormat.RGBA32));
				_batchFormatIndex = 0;
				return;
			}

			switch (platformName)
			{
				case "Standalone":
					_currentFormatChoices.Add(("Automatic", TextureImporterFormat.Automatic));
					_currentFormatChoices.Add(("DXT1 (RGB)", TextureImporterFormat.DXT1));
					_currentFormatChoices.Add(("DXT5 (RGBA)", TextureImporterFormat.DXT5));
					_currentFormatChoices.Add(("BC7 (HQ RGBA)", TextureImporterFormat.BC7));
					_currentFormatChoices.Add(("RGBA32 (Uncompressed)", TextureImporterFormat.RGBA32));
					break;
				case "Android":
					_currentFormatChoices.Add(("Automatic", TextureImporterFormat.Automatic));
					_currentFormatChoices.Add(("ETC2 RGB4", TextureImporterFormat.ETC2_RGB4));
					_currentFormatChoices.Add(("ETC2 RGBA8", TextureImporterFormat.ETC2_RGBA8));
					_currentFormatChoices.Add(("ASTC 6x6", TextureImporterFormat.ASTC_6x6));
					_currentFormatChoices.Add(("ASTC 4x4", TextureImporterFormat.ASTC_4x4));
					_currentFormatChoices.Add(("RGBA32 (Uncompressed)", TextureImporterFormat.RGBA32));
					break;
				case "iPhone":
					_currentFormatChoices.Add(("Automatic", TextureImporterFormat.Automatic));
					_currentFormatChoices.Add(("PVRTC RGBA4", TextureImporterFormat.PVRTC_RGBA4));
					_currentFormatChoices.Add(("ASTC 6x6", TextureImporterFormat.ASTC_6x6));
					_currentFormatChoices.Add(("ASTC 4x4", TextureImporterFormat.ASTC_4x4));
					_currentFormatChoices.Add(("RGBA32 (Uncompressed)", TextureImporterFormat.RGBA32));
					break;
				case "WebGL":
					_currentFormatChoices.Add(("Automatic", TextureImporterFormat.Automatic));
					_currentFormatChoices.Add(("ETC2 RGBA8", TextureImporterFormat.ETC2_RGBA8));
					_currentFormatChoices.Add(("ASTC 6x6", TextureImporterFormat.ASTC_6x6));
					_currentFormatChoices.Add(("RGBA32 (Uncompressed)", TextureImporterFormat.RGBA32));
					break;
			}

			_batchFormatIndex = 0;
		}

		private void ApplyFormat(TextureImporter importer, TextureImporterFormat format)
		{
			var platformName = GetPlatformName();
			if (string.IsNullOrEmpty(platformName))
				return;

			var settings = importer.GetPlatformTextureSettings(platformName);
			if (settings == null)
			{
				settings = new TextureImporterPlatformSettings();
				settings.name = platformName;
			}

			settings.overridden = true;
			settings.format = format;
			importer.SetPlatformTextureSettings(settings);
			EditorUtility.SetDirty(importer);
			importer.SaveAndReimport();
		}

		private void ClearOverride(TextureImporter importer)
		{
			var platformName = GetPlatformName();
			if (string.IsNullOrEmpty(platformName))
			{
				// Clear all overrides when Default is selected
				foreach (var name in PlatformOptions)
				{
					if (name == "Default") continue;
					var s = importer.GetPlatformTextureSettings(name);
					if (s != null && s.overridden)
					{
						s.overridden = false;
						importer.SetPlatformTextureSettings(s);
					}
				}
			}
			else
			{
				var settings = importer.GetPlatformTextureSettings(platformName);
				if (settings != null)
				{
					settings.overridden = false;
					importer.SetPlatformTextureSettings(settings);
				}
			}

			EditorUtility.SetDirty(importer);
			importer.SaveAndReimport();
		}

		private void ApplyBatchFormatToFiltered(TextureImporterFormat format)
		{
			try
			{
				AssetDatabase.StartAssetEditing();
				foreach (var entry in _filteredEntries)
				{
					ApplyFormat(entry.importer, format);
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				// Refresh previews after reimports
				RefreshPreviews();
				EditorApplication.delayCall += Repaint;
			}
		}

		private void ClearOverrideOnFiltered()
		{
			try
			{
				AssetDatabase.StartAssetEditing();
				foreach (var entry in _filteredEntries)
				{
					ClearOverride(entry.importer);
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				// Refresh previews after reimports
				RefreshPreviews();
				EditorApplication.delayCall += Repaint;
			}
		}

		private void ResetPerItemFormatSelection()
		{
			for (int i = 0; i < _allEntries.Count; i++)
			{
				_allEntries[i].selectedFormatIndex = 0;
			}
		}

		private void ApplyFormatForEntry(TextureEntry entry, TextureImporterFormat format)
		{
			ApplyFormat(entry.importer, format);
			entry.preview = null;
			EditorApplication.delayCall += RefreshPreviews;
			Repaint();
		}
	}



}

