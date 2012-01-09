﻿//
//    MCSkin3D, a 3d skin management studio for Minecraft
//    Copyright (C) 2011-2012 Altered Softworks & MCSkin3D Team
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Devcorp.Controls.Design;
using MB.Controls;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Security.Cryptography;
using Paril.Settings;
using Paril.Settings.Serializers;
using Paril.Components;
using Paril.Components.Shortcuts;
using Paril.Components.Update;
using System.Runtime.InteropServices;
using System.Collections;
using Paril.Compatibility;
using System.Drawing.Drawing2D;
using Paril.OpenGL;
using OpenTK.Graphics;
using System.Windows.Forms.VisualStyles;
using DragDropLib;
using Paril.Extensions;
using MCSkin3D.Language;
using System.Globalization;
using MCSkin3D.Forms;
using Version = Paril.Components.Update.Version;

namespace MCSkin3D
{
	public partial class Editor : Form
	{
		// ===============================================
		// Private/Static variables
		// ===============================================
		#region Variables
		Updater _updater;

		ColorSliderRenderer redRenderer, greenRenderer, blueRenderer, alphaRenderer;
		HueSliderRenderer hueRenderer;
		SaturationSliderRenderer saturationRenderer;
		LuminanceSliderRenderer lightnessRenderer;

		static ShortcutEditor _shortcutEditor = new ShortcutEditor();
		int _grassTop;
		int _alphaTex;
		int _previewPaint;
		Dictionary<Size, int> _charPaintSizes = new Dictionary<Size, int>();

		float _animationTime = 0;
		float _2dCamOffsetX = 0;
		float _2dCamOffsetY = 0;
		float _2dZoom = 8;
		float _3dZoom = -80;
		float _3dRotationX = 0, _3dRotationY = 180;
		bool _mouseIsDown = false;
		Point _mousePoint;
		UndoBuffer _currentUndoBuffer = null;
		Skin _lastSkin = null;
		bool _skipListbox = false;
		internal PleaseWait _pleaseWaitForm;
		Color _primaryColor = Color.FromArgb(255, 255, 255, 255), _secondaryColor = Color.FromArgb(255, 0, 0, 0);
		bool _skipColors = false;
		ViewMode _currentViewMode = ViewMode.Perspective;
		Renderer _renderer;
		List<BackgroundImage> _backgrounds = new List<BackgroundImage>();
		int _selectedBackground = 0;
		GLControl rendererControl;
		int _toolboxUpNormal, _toolboxUpHover, _toolboxDownNormal, _toolboxDownHover;

		List<ToolIndex> _tools = new List<ToolIndex>();
		ToolIndex _selectedTool;
		FileSystemWatcher _watcher;
		#endregion

		public DodgeBurnOptions DodgeBurnOptions { get; private set; }
		public DarkenLightenOptions DarkenLightenOptions { get; private set; }
		public PencilOptions PencilOptions { get; private set; }
		public FloodFillOptions FloodFillOptions { get; private set; }
		public NoiseOptions NoiseOptions { get; private set; }
		public EraserOptions EraserOptions { get; private set; }

		public static Editor MainForm { get; private set; }

		class ModelToolStripMenuItem : ToolStripMenuItem
		{
			public Model Model;

			public ModelToolStripMenuItem(Model model) :
				base(model.Name)
			{
				Model = model;
			}

			protected override void OnClick(EventArgs e)
			{
				MainForm.SetModel(Model);
			}
		}

		// ===============================================
		// Constructor
		// ===============================================
		#region Constructor
		public Editor()
		{
			MainForm = this;
			InitializeComponent();

			bool settingsLoaded = GlobalSettings.Load();

			Icon = Properties.Resources.Icon_new;

			LanguageLoader.LoadLanguages("Languages");

			DodgeBurnOptions = new DodgeBurnOptions();
			DarkenLightenOptions = new DarkenLightenOptions();
			PencilOptions = new PencilOptions();
			FloodFillOptions = new FloodFillOptions();
			NoiseOptions = new NoiseOptions();
			EraserOptions = new EraserOptions();

			_tools.Add(new ToolIndex(new CameraTool(), null, "T_TOOL_CAMERA", Properties.Resources.eye__1_, Keys.C));
			_tools.Add(new ToolIndex(new PencilTool(), PencilOptions, "T_TOOL_PENCIL", Properties.Resources.pen, Keys.P));
			_tools.Add(new ToolIndex(new EraserTool(), EraserOptions, "T_TOOL_ERASER", Properties.Resources.erase, Keys.E));
			_tools.Add(new ToolIndex(new DropperTool(), null, "T_TOOL_DROPPER", Properties.Resources.pipette, Keys.D));
			_tools.Add(new ToolIndex(new DodgeBurnTool(), DodgeBurnOptions, "T_TOOL_DODGEBURN", Properties.Resources.dodge, Keys.B));
			_tools.Add(new ToolIndex(new DarkenLightenTool(), DarkenLightenOptions, "T_TOOL_DARKENLIGHTEN", Properties.Resources.darkenlighten, Keys.L));
			_tools.Add(new ToolIndex(new FloodFillTool(), FloodFillOptions, "T_TOOL_BUCKET", Properties.Resources.fill_bucket, Keys.F));
			_tools.Add(new ToolIndex(new NoiseTool(), NoiseOptions, "T_TOOL_NOISE", Properties.Resources.noise, Keys.N));

			animateToolStripMenuItem.Checked = GlobalSettings.Animate;
			followCursorToolStripMenuItem.Checked = GlobalSettings.FollowCursor;
			grassToolStripMenuItem.Checked = GlobalSettings.Grass;
			ghostHiddenPartsToolStripMenuItem.Checked = GlobalSettings.Ghost;

			alphaCheckerboardToolStripMenuItem.Checked = GlobalSettings.AlphaCheckerboard;
			textureOverlayToolStripMenuItem.Checked = GlobalSettings.TextureOverlay;
			modeToolStripMenuItem1.Checked = GlobalSettings.OnePointOhMode;

			SetCheckbox(VisiblePartFlags.HeadFlag, headToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.ChestFlag, chestToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.LeftArmFlag, leftArmToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.RightArmFlag, rightArmToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.HelmetFlag, helmetToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.LeftLegFlag, leftLegToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.RightLegFlag, rightLegToolStripMenuItem);
			Language.Language useLanguage = null;
			try
			{
				// stage 1 (prelim): if no language, see if our languages contain it
				if (string.IsNullOrEmpty(GlobalSettings.LanguageFile))
					useLanguage = LanguageLoader.FindLanguage((CultureInfo.CurrentUICulture.IsNeutralCulture == false) ? CultureInfo.CurrentUICulture.Parent.Name : CultureInfo.CurrentUICulture.Name);
				// stage 2: load from last used language
				if (useLanguage == null)
					useLanguage = LanguageLoader.FindLanguage(GlobalSettings.LanguageFile);
				// stage 3: use English file, if it exists
				if (useLanguage == null)
					useLanguage = LanguageLoader.FindLanguage("English");
			}
			catch
			{
			}
			finally
			{
				// stage 4: fallback to built-in English file
				if (useLanguage == null)
				{
					MessageBox.Show(this, "For some reason, the default language files were missing or failed to load(did you extract?) - we'll supply you with a base language of English just so you know what you're doing!");
					useLanguage = LanguageLoader.LoadDefault();
				}
			}

			foreach (var lang in LanguageLoader.Languages)
			{
				lang.Item = new ToolStripMenuItem((lang.Culture != null) ? (char.ToUpper(lang.Culture.NativeName[0]) + lang.Culture.NativeName.Substring(1)) : lang.Name);
				lang.Item.Tag = lang;
				lang.Item.Click += new EventHandler(languageToolStripMenuItem_Click);
				languageToolStripMenuItem.DropDownItems.Add(lang.Item);
			}

			for (int i = _tools.Count - 1; i >= 0; --i)
			{
				toolToolStripMenuItem.DropDownItems.Insert(0, _tools[i].MenuItem);
				_tools[i].MenuItem.Click += ToolMenuItemClicked;
				toolStrip1.Items.Insert(6, _tools[i].Button);
				_tools[i].Button.Click += ToolMenuItemClicked;

				languageProvider1.SetPropertyNames(_tools[i].MenuItem, "Text");
				languageProvider1.SetPropertyNames(_tools[i].Button, "Text");
			}

			InitShortcuts();
			LoadShortcutKeys(GlobalSettings.ShortcutKeys);
			_shortcutEditor.ShortcutExists += new EventHandler<ShortcutExistsEventArgs>(_shortcutEditor_ShortcutExists);
			CurrentLanguage = useLanguage;
			Brushes.LoadBrushes();
			CurrentLanguage = useLanguage;

			SetSelectedTool(_tools[0]);

			if (Screen.PrimaryScreen.BitsPerPixel != 32)
			{
				MessageBox.Show(this, GetLanguageString("B_MSG_PIXELFORMAT"), GetLanguageString("B_CAP_SORRY"), MessageBoxButtons.OK, MessageBoxIcon.Error);
				Application.Exit();
			}

			redColorSlider.Renderer = redRenderer = new ColorSliderRenderer(redColorSlider);
			greenColorSlider.Renderer = greenRenderer = new ColorSliderRenderer(greenColorSlider);
			blueColorSlider.Renderer = blueRenderer = new ColorSliderRenderer(blueColorSlider);
			alphaColorSlider.Renderer = alphaRenderer = new ColorSliderRenderer(alphaColorSlider);

			hueColorSlider.Renderer = hueRenderer = new HueSliderRenderer(hueColorSlider);
			saturationColorSlider.Renderer = saturationRenderer = new SaturationSliderRenderer(saturationColorSlider);
			lightnessColorSlider.Renderer = lightnessRenderer = new LuminanceSliderRenderer(lightnessColorSlider);

			KeyPreview = true;
			Text = "MCSkin3D v" + Program.Version.ToString();

#if BETA
			Text += " [Beta]";
#endif

			if (!Directory.Exists("Swatches") || !Directory.Exists("Skins"))
				MessageBox.Show(this, GetLanguageString("B_MSG_DIRMISSING"));

			Directory.CreateDirectory("Swatches");
			Directory.CreateDirectory("Skins");
			swatchContainer.AddDirectory("Swatches");

			_updater = new Updater("http://alteredsoftworks.com/mcskin3d/update", Program.Version.ToString());
			_updater.UpdateHandler = new AssemblyVersion();
			_updater.NewVersionAvailable += _updater_NewVersionAvailable;
			_updater.SameVersion += _updater_SameVersion;
			_updater.CheckForUpdate();

			automaticallyCheckForUpdatesToolStripMenuItem.Checked = GlobalSettings.AutoUpdate;

			ModelLoader.LoadModels();

			foreach (var x in ModelLoader.Models)
				toolStripDropDownButton1.DropDownItems.Add(new ModelToolStripMenuItem(x.Value));

			SetSampleMenuItem(GlobalSettings.Multisamples);

			// set up the GL control
			rendererControl = new GLControl(new GraphicsMode(new ColorFormat(32), 24, 8, GlobalSettings.Multisamples));
			rendererControl.BackColor = System.Drawing.Color.Black;
			rendererControl.Dock = System.Windows.Forms.DockStyle.Fill;
			rendererControl.Location = new System.Drawing.Point(0, 25);
			rendererControl.Name = "rendererControl";
			rendererControl.Size = new System.Drawing.Size(641, 580);
			rendererControl.TabIndex = 4;
			rendererControl.VSync = true;
			rendererControl.Load += new System.EventHandler(this.rendererControl_Load);
			rendererControl.Paint += new System.Windows.Forms.PaintEventHandler(this.rendererControl_Paint);
			rendererControl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.rendererControl_MouseDown);
			rendererControl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.rendererControl_MouseMove);
			rendererControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.rendererControl_MouseUp);
			rendererControl.MouseLeave += new EventHandler(rendererControl_MouseLeave);
			rendererControl.Resize += new System.EventHandler(this.rendererControl_Resize);
			rendererControl.MouseWheel += new MouseEventHandler(rendererControl_MouseWheel);

			splitContainer4.Panel2.Controls.Add(rendererControl);
			rendererControl.BringToFront();

			System.Timers.Timer animTimer = new System.Timers.Timer();
			animTimer.Interval = 22;
			animTimer.Elapsed += new System.Timers.ElapsedEventHandler(animTimer_Elapsed);
			animTimer.Start();

			_animTimer.Elapsed += new System.Timers.ElapsedEventHandler(_animTimer_Elapsed);
			_animTimer.SynchronizingObject = this;

			if (!settingsLoaded)
				MessageBox.Show(this, GetLanguageString("C_SETTINGSFAILED"));

			treeView1.ItemHeight = GlobalSettings.TreeViewHeight;
			treeView1.Scrollable = true;
			splitContainer4.SplitterDistance = 74;

			_watcher = new FileSystemWatcher("Skins");
			_watcher.SynchronizingObject = MainForm;
			_watcher.Changed += _watcher_Changed;
			_watcher.Created += _watcher_Created;
			_watcher.Deleted += _watcher_Deleted;
			_watcher.Renamed += _watcher_Renamed;
			_watcher.IncludeSubdirectories = true;
			_watcher.EnableRaisingEvents = true;

			if (GlobalSettings.OnePointOhMode)
				ModelLoader.InvertBottomFaces();
		}

		static List<string> _ignoreFiles = new List<string>();

		public static void AddIgnoreFile(string fileName)
		{
			_ignoreFiles.Add(fileName);
		}

		public static bool IsIgnored(string fileName)
		{
			FileInfo right = new FileInfo(fileName);

			if (_ignoreFiles.Contains(right.FullName))
			{
				_ignoreFiles.Remove(fileName);
				return true;
			}

			return false;
		}

		void _watcher_Renamed(object sender, RenamedEventArgs e)
		{
			if (IsIgnored(e.OldFullPath))
				return;
	
			System.Diagnostics.Debug.WriteLine(e.ChangeType.ToString() + " detected on " + e.Name);

			if (Path.HasExtension(e.OldFullPath))
			{
				Skin node = (Skin)treeView1.NodeFromPath(Path.GetDirectoryName(e.OldFullPath).Replace("Skins\\", "") + "\\" + Path.GetFileNameWithoutExtension(e.OldFullPath), false);

				if (node == null)
					return;

				node.ChangeName(Path.GetFileNameWithoutExtension(e.FullPath), true);
			}
			else
			{
				FolderNode folder = (FolderNode)treeView1.NodeFromPath(Path.GetDirectoryName(e.OldFullPath).Replace("Skins\\", ""), false);

				if (folder == null)
					return;

				folder.Name = folder.Text = new DirectoryInfo(e.FullPath).Name;
				treeView1.Invalidate();
			}
		}

		void _watcher_Deleted(object sender, FileSystemEventArgs e)
		{
			if (IsIgnored(e.FullPath))
				return;
	
			System.Diagnostics.Debug.WriteLine(e.ChangeType.ToString() + " detected on " + e.Name);

			if (Path.HasExtension(e.FullPath))
			{
				Skin node = (Skin)treeView1.NodeFromPath(Path.GetDirectoryName(e.FullPath).Replace("Skins\\", "") + "\\" + Path.GetFileNameWithoutExtension(e.FullPath), false);

				if (node == null)
					return;

				node.Delete();
				node.Remove();
				node.Dispose();

				treeView1_AfterSelect(treeView1, new TreeViewEventArgs(treeView1.SelectedNode));

				Invalidate();
			}
			else
			{
				FolderNode folder = (FolderNode)treeView1.NodeFromPath(e.FullPath.Replace("Skins\\", ""), false);

				if (folder == null)
					return;

				folder.Remove();
			}
		}

		void _watcher_Created(object sender, FileSystemEventArgs e)
		{
			if (IsIgnored(e.FullPath))
				return;
	
			System.Diagnostics.Debug.WriteLine(e.ChangeType.ToString() + " detected on " + e.Name);

			if (Path.HasExtension(e.FullPath))
			{
				// Easy - assume an import!
				// FullPath is relative - thank God.
				var node = treeView1.NodeFromPath(Path.GetDirectoryName(e.FullPath).Replace("Skins\\", ""), false);

				if (node == null)
					return;
					//throw new Exception("Watcher found a new file, but path is invalid");

				Thread.Sleep(100); // Sleep a bit, because Windows might still be using this file.

				// Oh, slight modification to import code - forgot that it's not being
				// imported externally...

				Skin newSkin = new Skin(e.FullPath);

				// FIXME: Don't ask me why this is required, but, for some reason
				// the skin just stays black without this.
				treeView1.BeginUpdate();
				var oldNode = treeView1.SelectedNode;

				node.Nodes.Add(newSkin);
				newSkin.SetImages();

				treeView1.SelectedNode = newSkin;
				treeView1.SelectedNode = oldNode;
				treeView1.EndUpdate();
			}
			else
			{
				FolderNode folder = (FolderNode)treeView1.NodeFromPath(Path.GetDirectoryName(e.FullPath.Replace("Skins\\", "")), false);

				if (folder == null)
					return;

				FolderNode newFolder = new FolderNode(new DirectoryInfo(e.FullPath).Name);
				folder.Nodes.Add(newFolder);
			}
		}

		void _watcher_Changed(object sender, FileSystemEventArgs e)
		{
			if (IsIgnored(e.FullPath))
				return;

			System.Diagnostics.Debug.WriteLine(e.ChangeType.ToString() + " detected on " + e.Name);

			rendererControl.MakeCurrent();
			var node = treeView1.NodeFromPath(e.FullPath.Replace(".png", "").Replace("Skins\\", ""));

			if (node == null)
				return;

			Thread.Sleep(100); // Sleep a bit, because Windows might still be using this file.
			
			if (node is Skin)
			{
				Skin skin = (Skin)node;
				skin.SetImages();
				skin.Undo.Clear();

				CheckUndo();
				
				if (treeView1.SelectedNode == skin)
				{
					ColorGrabber skinImage = new ColorGrabber(skin.GLImage, skin.Width, skin.Height);
					skinImage.Load();

					skinImage.Texture = GlobalDirtiness.CurrentSkin;
					skinImage.Save();
					skinImage.Texture = _previewPaint;
					skinImage.Save();
				}
				else
				{
					// FIXME: Don't ask me why this is required, but, for some reason
					// the skin just stays black without this.
					treeView1.BeginUpdate();
					var oldNode = treeView1.SelectedNode;
					treeView1.SelectedNode = skin;
					treeView1.SelectedNode = oldNode;
					treeView1.EndUpdate();
				}
			}
		}

		void _shortcutEditor_ShortcutExists(object sender, ShortcutExistsEventArgs e)
		{
			//MessageBox.Show(string.Format(GetLanguageString("B_MSG_SHORTCUTEXISTS"), e.ShortcutName, e.OtherName));
		}
		#endregion

		public GLControl Renderer
		{
			get { return rendererControl; }
		}

		public MouseButtons CameraRotate
		{
			get
			{
				if (_selectedTool == _tools[(int)Tools.Camera])
					return MouseButtons.Left;
				else
					return MouseButtons.Right;
			}
		}

		public MouseButtons CameraZoom
		{
			get
			{
				if (_selectedTool == _tools[(int)Tools.Camera])
					return MouseButtons.Right;
				else
					return MouseButtons.Middle;
			}
		}

		// =====================================================================
		// Updating
		// =====================================================================
		#region Update
		public void Invoke(Action action)
		{
			this.Invoke((Delegate)action);
		}

		void _updater_SameVersion(object sender, EventArgs e)
		{
			this.Invoke(() => MessageBox.Show(this, GetLanguageString("B_MSG_UPTODATE")));
		}

		void _updater_NewVersionAvailable(object sender, EventArgs e)
		{
			this.Invoke(delegate()
			{
				if (MessageBox.Show(this, GetLanguageString("B_MSG_NEWUPDATE"), "Woo!", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
					Process.Start("http://www.minecraftforum.net/topic/746941-mcskin3d-new-skinning-program/");
			});
		}
		#endregion

		// =====================================================================
		// Shortcuts
		// =====================================================================
		#region Shortcuts

		string CompileShortcutKeys()
		{
			string c = "";

			for (int i = 0; i < _shortcutEditor.ShortcutCount; ++i)
			{
				var shortcut = _shortcutEditor.ShortcutAt(i);

				if (i != 0)
					c += "|";

				Keys key = shortcut.Keys & ~Keys.Modifiers;
				Keys modifiers = (Keys)((int)shortcut.Keys - (int)key);

				if (modifiers != 0)
					c += shortcut.SaveName + "=" + key + "+" + modifiers;
				else
					c += shortcut.SaveName + "=" + key;
			}

			return c;
		}

		IShortcutImplementor FindShortcut(string name)
		{
			foreach (var s in _shortcutEditor.Shortcuts)
			{
				if (s.SaveName == name)
					return s;
			}

			return null;
		}

		void LoadShortcutKeys(string s)
		{
			if (string.IsNullOrEmpty(s))
				return; // leave defaults

			var shortcuts = s.Split('|');

			foreach (var shortcut in shortcuts)
			{
				var args = shortcut.Split('=');

				string name = args[0];
				string key;
				string modifiers = "0";

				if (args[1].Contains('+'))
				{
					var mods = args[1].Split('+');

					key = mods[0];
					modifiers = mods[1];
				}
				else
					key = args[1];

				var sh = FindShortcut(name);

				if (sh == null)
					continue;

				sh.Keys = (Keys)Enum.Parse(typeof(Keys), key) | (Keys)Enum.Parse(typeof(Keys), modifiers);
			}
		}

		void InitMenuShortcut(ToolStripMenuItem item, Action callback)
		{
			MenuStripShortcut shortcut = new MenuStripShortcut(item);
			shortcut.Pressed = callback;

			_shortcutEditor.AddShortcut(shortcut);
		}

		void InitMenuShortcut(ToolStripMenuItem item, Keys keys, Action callback)
		{
			MenuStripShortcut shortcut = new MenuStripShortcut(item, keys);
			shortcut.Pressed = callback;

			_shortcutEditor.AddShortcut(shortcut);
		}

		void InitUnlinkedShortcut(string name, Keys defaultKeys, Action callback)
		{
			ShortcutBase shortcut = new ShortcutBase(name, defaultKeys);
			shortcut.Pressed = callback;

			_shortcutEditor.AddShortcut(shortcut);
		}

		void InitControlShortcut(string name, Control control, Keys defaultKeys, Action callback)
		{
			ControlShortcut shortcut = new ControlShortcut(name, defaultKeys, control);
			shortcut.Pressed = callback;

			_shortcutEditor.AddShortcut(shortcut);
		}

		void InitShortcuts()
		{
			// shortcut menus
			InitMenuShortcut(undoToolStripMenuItem, PerformUndo);
			InitMenuShortcut(redoToolStripMenuItem, PerformRedo);
			InitMenuShortcut(perspectiveToolStripMenuItem, () => SetViewMode(ViewMode.Perspective));
			InitMenuShortcut(textureToolStripMenuItem, () => SetViewMode(ViewMode.Orthographic));
			InitMenuShortcut(hybridViewToolStripMenuItem, () => SetViewMode(ViewMode.Hybrid));
			InitMenuShortcut(animateToolStripMenuItem, ToggleAnimation);
			InitMenuShortcut(followCursorToolStripMenuItem, ToggleFollowCursor);
			InitMenuShortcut(grassToolStripMenuItem, ToggleGrass);
			InitMenuShortcut(ghostHiddenPartsToolStripMenuItem, ToggleGhosting);
			InitMenuShortcut(alphaCheckerboardToolStripMenuItem, ToggleAlphaCheckerboard);
			InitMenuShortcut(textureOverlayToolStripMenuItem, ToggleOverlay);
			InitMenuShortcut(offToolStripMenuItem, () => SetTransparencyMode(TransparencyMode.Off));
			InitMenuShortcut(helmetOnlyToolStripMenuItem, () => SetTransparencyMode(TransparencyMode.Helmet));
			InitMenuShortcut(allToolStripMenuItem, () => SetTransparencyMode(TransparencyMode.All));
			InitMenuShortcut(headToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.HeadFlag));
			InitMenuShortcut(helmetToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.HelmetFlag));
			InitMenuShortcut(chestToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.ChestFlag));
			InitMenuShortcut(leftArmToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.LeftArmFlag));
			InitMenuShortcut(rightArmToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.RightArmFlag));
			InitMenuShortcut(leftLegToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.LeftLegFlag));
			InitMenuShortcut(rightLegToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.RightLegFlag));
			InitMenuShortcut(saveToolStripMenuItem, PerformSave);
			InitMenuShortcut(saveAsToolStripMenuItem, PerformSaveAs);
			InitMenuShortcut(saveAllToolStripMenuItem, PerformSaveAll);
			InitMenuShortcut(uploadToolStripMenuItem, PerformUpload);

			foreach (var item in _tools)
				InitMenuShortcut(item.MenuItem, item.DefaultKeys, item.SetMeAsTool);

			// not in the menu
			InitUnlinkedShortcut("S_TOGGLETRANS", Keys.Shift | Keys.U, ToggleTransparencyMode);
			InitUnlinkedShortcut("S_TOGGLEVIEW", Keys.Control | Keys.V, ToggleViewMode);
			InitUnlinkedShortcut("S_SCREENSHOT_CLIP", Keys.Control | Keys.B, TakeScreenshot);
			InitUnlinkedShortcut("S_SCREENSHOT_SAVE", Keys.Control | Keys.Shift | Keys.B, SaveScreenshot);
			InitUnlinkedShortcut("S_DELETE", Keys.Delete, PerformDeleteSkin);
			InitUnlinkedShortcut("S_CLONE", Keys.Control | Keys.C, PerformCloneSkin);
			InitUnlinkedShortcut("S_RENAME", Keys.Control | Keys.R, PerformNameChange);
			InitUnlinkedShortcut("T_TREE_IMPORTHERE", Keys.Control | Keys.I, PerformImportSkin);
			InitUnlinkedShortcut("T_TREE_NEWFOLDER", Keys.Control | Keys.Shift | Keys.N, PerformNewFolder);
			InitUnlinkedShortcut("M_NEWSKIN_HERE", Keys.Control | Keys.Shift | Keys.M, PerformNewSkin);
			InitUnlinkedShortcut("S_COLORSWAP", Keys.S, PerformSwitchColor);
			InitControlShortcut("S_SWATCH_ZOOMIN", swatchContainer.SwatchDisplayer, Keys.Oemplus, PerformSwatchZoomIn);
			InitControlShortcut("S_SWATCH_ZOOMOUT", swatchContainer.SwatchDisplayer, Keys.OemMinus, PerformSwatchZoomOut);
			InitControlShortcut("S_TREEVIEW_ZOOMIN", treeView1, Keys.Control | Keys.Oemplus, PerformTreeViewZoomIn);
			InitControlShortcut("S_TREEVIEW_ZOOMOUT", treeView1, Keys.Control | Keys.OemMinus, PerformTreeViewZoomOut);
			InitUnlinkedShortcut("T_DECRES", Keys.Control | Keys.Shift | Keys.D, PerformDecreaseResolution);
			InitUnlinkedShortcut("T_INCRES", Keys.Control | Keys.Shift | Keys.I, PerformIncreaseResolution);
		}

		void PerformSwitchColor()
		{
			if (_secondaryIsFront)
				colorPreview1_Click(null, null);
			else
				colorPreview2_Click(null, null);
		}

		public void SetSelectedTool(ToolIndex index)
		{
			if (_selectedTool != null)
				_selectedTool.MenuItem.Checked = _selectedTool.Button.Checked = false;

			var oldTool = _selectedTool;
			_selectedTool = index;
			index.MenuItem.Checked = index.Button.Checked = true;

			splitContainer4.Panel1.Controls.Clear();

			if (oldTool != null && oldTool.OptionsPanel != null)
				oldTool.OptionsPanel.BoxHidden();
			if (_selectedTool.OptionsPanel != null)
				_selectedTool.OptionsPanel.BoxShown();

			if (_selectedTool.OptionsPanel != null)
			{
				_selectedTool.OptionsPanel.Dock = DockStyle.Fill;
				splitContainer4.Panel1.Controls.Add(_selectedTool.OptionsPanel);
			}

			toolStripStatusLabel1.Text = index.Tool.GetStatusLabelText();
		}

		void ToolMenuItemClicked(object sender, EventArgs e)
		{
			ToolStripItem item = (ToolStripItem)sender;
			SetSelectedTool((ToolIndex)item.Tag);
		}

		void PerformCamera()
		{
		}

		void PerformTreeViewZoomIn()
		{
			treeView1.ZoomIn();
		}

		void PerformTreeViewZoomOut()
		{
			treeView1.ZoomOut();
		}

		void PerformSwatchZoomOut()
		{
			swatchContainer.ZoomOut();
		}

		void PerformSwatchZoomIn()
		{
			swatchContainer.ZoomIn();
		}

		bool PerformShortcut(Keys key, Keys modifiers)
		{
			foreach (var shortcut in _shortcutEditor.Shortcuts)
			{
				if (shortcut.CanEvaluate() && (shortcut.Keys & ~Keys.Modifiers) == key &&
					(shortcut.Keys & ~(shortcut.Keys & ~Keys.Modifiers)) == modifiers)
				{
					shortcut.Pressed();
					return true;
				}
			}

			return false;
		}
		#endregion

		// =====================================================================
		// Overrides
		// =====================================================================
		#region Overrides
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (textBox1.ContainsFocus == false && labelEditTextBox.ContainsFocus == false)
				if (PerformShortcut(e.KeyCode & ~Keys.Modifiers, e.Modifiers))
				{
					e.Handled = true;
					return;
				}

			base.OnKeyDown(e);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (RecursiveNodeIsDirty(treeView1.Nodes))
			{
				if (MessageBox.Show(this, GetLanguageString("C_UNSAVED"), GetLanguageString("C_UNSAVED_CAPTION"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
				{
					e.Cancel = true;
					return;
				}
			}

			_updater.Abort();
			GlobalSettings.ShortcutKeys = CompileShortcutKeys();

			GlobalSettings.Save();
		}

		TreeNode _tempToSelect;
		void RecurseAddDirectories(string path, TreeNodeCollection nodes, List<Skin> skins)
		{
			var di = new DirectoryInfo(path);

			foreach (var file in di.GetFiles("*.png", SearchOption.TopDirectoryOnly))
			{
				var skin = new Skin(file);
				nodes.Add(skin);

				if (_tempToSelect == null)
					_tempToSelect = skin;
				else if (GlobalSettings.LastSkin == skin.Name)
					_tempToSelect = skin;

				skins.Add(skin);
			}

			foreach (var dir in di.GetDirectories())
			{
				if ((dir.Attributes & FileAttributes.Hidden) != 0)
					continue;

				FolderNode folderNode = new FolderNode(dir.Name);
				RecurseAddDirectories(dir.FullName, folderNode.Nodes, skins);
				nodes.Add(folderNode);
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			SetTransparencyMode(GlobalSettings.Transparency);
			SetViewMode(_currentViewMode);

			rendererControl.MakeCurrent();

			List<Skin> skins = new List<Skin>();
			RecurseAddDirectories("Skins", treeView1.Nodes, skins);

			foreach (var s in skins)
				s.SetImages();

			treeView1.SelectedNode = _tempToSelect;

			SetColor(Color.White);
			SetVisibleParts();

			toolToolStripMenuItem.DropDown.Closing += DontCloseMe;
			modeToolStripMenuItem.DropDown.Closing += DontCloseMe;
			threeDToolStripMenuItem.DropDown.Closing += DontCloseMe;
			twoDToolStripMenuItem.DropDown.Closing += DontCloseMe;
			transparencyModeToolStripMenuItem.DropDown.Closing += DontCloseMe;
			visiblePartsToolStripMenuItem.DropDown.Closing += DontCloseMe;

			optionsToolStripMenuItem.DropDown.Closing += (sender, args) =>
			{
				if (modeToolStripMenuItem1.Selected && (args.CloseReason == ToolStripDropDownCloseReason.ItemClicked || args.CloseReason == ToolStripDropDownCloseReason.Keyboard))
					args.Cancel = true;
			};
		}

		void DontCloseMe(object sender, ToolStripDropDownClosingEventArgs e)
		{
			if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked ||
				e.CloseReason == ToolStripDropDownCloseReason.Keyboard)
				e.Cancel = true;
		}

		#endregion

		// =====================================================================
		// Private functions
		// =====================================================================
		#region Private Functions
		// Utility function, sets a tool strip checkbox item's state if the flag is present
		void SetCheckbox(VisiblePartFlags flag, ToolStripMenuItem checkbox)
		{
			if ((GlobalSettings.ViewFlags & flag) != 0)
				checkbox.Checked = true;
			else
				checkbox.Checked = false;
		}

		int GetPaintTexture(int width, int height)
		{
			if (!_charPaintSizes.ContainsKey(new Size(width, height)))
			{
				int id = GL.GenTexture();

				int[] arra = new int[width * height];
				unsafe
				{
					fixed (int* texData = arra)
					{
						int *d = texData;

						for (int y = 0; y < height; ++y)
							for (int x = 0; x < width; ++x)
							{
								*d = ((y * width) + x) | (255 << 24);
								d++;
							}
					}
				}

				RenderState.BindTexture(id);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, arra);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

				_charPaintSizes.Add(new Size(width, height), id);

				return id;
			}

			return _charPaintSizes[new Size(width, height)];
		}

		void DrawSkinnedRectangle
			(float x, float y, float z, float width, float length, float height,
			int topSkinX, int topSkinY, int topSkinW, int topSkinH,
			int texture, int skinW = 64, int skinH = 32)
		{
			RenderState.BindTexture(texture);

			GL.Begin(BeginMode.Quads);

			width /= 2;
			length /= 2;
			height /= 2;

			float tsX = (float)topSkinX / skinW;
			float tsY = (float)topSkinY / skinH;
			float tsW = (float)topSkinW / skinW;
			float tsH = (float)topSkinH / skinH;

			GL.TexCoord2(tsX, tsY); GL.Vertex3(x - width, y + length, z - height);          // Top Right Of The Quad (Top)
			GL.TexCoord2(tsX + tsW - 0.00005, tsY); GL.Vertex3(x + width, y + length, z - height);          // Top Left Of The Quad (Top)
			GL.TexCoord2(tsX + tsW - 0.00005, tsY + tsH - 0.00005); GL.Vertex3(x + width, y + length, z + height);          // Bottom Left Of The Quad (Top)
			GL.TexCoord2(tsX, tsY + tsH - 0.00005); GL.Vertex3(x - width, y + length, z + height);          // Bottom Right Of The Quad (Top)

			GL.End();
		}

		void DrawPlayer2D(int tex, Skin skin, bool pickView)
		{
			if (!pickView && GlobalSettings.AlphaCheckerboard)
			{
				RenderState.BindTexture(_alphaTex);

				GL.Begin(BeginMode.Quads);
				GL.TexCoord2(0, 0); GL.Vertex2(0, 0);
				GL.TexCoord2(_currentViewport.Width / 32.0f, 0); GL.Vertex2(_currentViewport.Width, 0);
				GL.TexCoord2(_currentViewport.Width / 32.0f, _currentViewport.Height / 32.0f); GL.Vertex2(_currentViewport.Width, _currentViewport.Height);
				GL.TexCoord2(0, _currentViewport.Height / 32.0f); GL.Vertex2(0, _currentViewport.Height);
				GL.End();
			}

			if (skin != null)
				RenderState.BindTexture(tex);

			GL.PushMatrix();

			GL.Translate((_2dCamOffsetX), (_2dCamOffsetY), 0);
			GL.Translate((_currentViewport.Width / 2) + -_2dCamOffsetX, (_currentViewport.Height / 2) + -_2dCamOffsetY, 0);
			GL.Scale(_2dZoom, _2dZoom, 1);

			if (pickView)
				GL.Disable(EnableCap.Blend);
			else
				GL.Enable(EnableCap.Blend);

			GL.Translate((_2dCamOffsetX), (_2dCamOffsetY), 0);
			if (skin != null)
			{
				float w = skin.Width;
				float h = skin.Height;
				GL.Begin(BeginMode.Quads);
				GL.TexCoord2(0, 0); GL.Vertex2(-(skin.Width / 2), -(skin.Height / 2));
				GL.TexCoord2(1, 0); GL.Vertex2((skin.Width / 2), -(skin.Height / 2));
				GL.TexCoord2(1, 1); GL.Vertex2((skin.Width / 2), (skin.Height / 2));
				GL.TexCoord2(0, 1); GL.Vertex2(-(skin.Width / 2), (skin.Height / 2));
				GL.End();
			}

			if (!pickView && GlobalSettings.TextureOverlay && skin != null &&
				_backgrounds[_selectedBackground].GLImage != 0)
			{
				RenderState.BindTexture(_backgrounds[_selectedBackground].GLImage);

				GL.Begin(BeginMode.Quads);
				GL.TexCoord2(0, 0); GL.Vertex2(-(skin.Width / 2), -(skin.Height / 2));
				GL.TexCoord2(1, 0); GL.Vertex2((skin.Width / 2), -(skin.Height / 2));
				GL.TexCoord2(1, 1); GL.Vertex2((skin.Width / 2), (skin.Height / 2));
				GL.TexCoord2(0, 1); GL.Vertex2(-(skin.Width / 2), (skin.Height / 2));
				GL.End();
			}
			GL.PopMatrix();

			GL.Disable(EnableCap.Blend);
		}

		public static Model CurrentModel
		{
			get { return MainForm._lastSkin == null ? null : MainForm._lastSkin.Model; }
		}

		void DrawPlayer(int tex, Skin skin, bool pickView)
		{
			bool grass = !pickView && grassToolStripMenuItem.Checked;

			var clPt = rendererControl.PointToClient(Cursor.Position);
			var x = clPt.X - (_currentViewport.Width / 2);
			var y = clPt.Y - (_currentViewport.Height / 2);

			if (!pickView && GlobalSettings.Transparency == TransparencyMode.All)
				GL.Enable(EnableCap.Blend);
			else
				GL.Disable(EnableCap.Blend);

			if (grass)
				DrawSkinnedRectangle(0, 24, 0, 1024, 4, 1024, 0, 0, 1024, 1024, _grassTop, 16, 16);

			Vector3 helmetRotate = (GlobalSettings.FollowCursor) ? new Vector3((float)y / 25, (float)x / 25, 0) : Vector3.Zero;
			double sinAnim = (GlobalSettings.Animate) ? Math.Sin(_animationTime) : 0;

			// draw non-transparent meshes
			foreach (var mesh in CurrentModel.Meshes)
			{
				if ((GlobalSettings.ViewFlags & mesh.Part) == 0 &&
					!(GlobalSettings.Ghost && !pickView))
					continue;

				var newMesh = mesh;

				newMesh.Texture = tex;

				if (mesh.FollowCursor && GlobalSettings.FollowCursor)
					newMesh.Rotate = helmetRotate;

				if ((GlobalSettings.ViewFlags & mesh.Part) == 0 && GlobalSettings.Ghost && !pickView)
				{
					foreach (var f in mesh.Faces)
						for (int i = 0; i < f.Colors.Length; ++i)
							f.Colors[i] = new Color4(1, 1, 1, 0.25f);
				}
				else
				{
					foreach (var f in mesh.Faces)
						for (int i = 0; i < f.Colors.Length; ++i)
							f.Colors[i] = Color4.White;
				}

				if (GlobalSettings.Animate && mesh.RotateFactor != 0)
					newMesh.Rotate += new Vector3((float)sinAnim * mesh.RotateFactor, 0, 0);

				_renderer.AddMesh(newMesh);
			}

			_renderer.Render();

			/*
			// Draw ghosted parts
			if (GlobalSettings.Ghost && !pickView)
			{
				foreach (var mesh in CurrentModel.Meshes)
				{
					if (mesh.Helmet)
						continue;
					if ((GlobalSettings.ViewFlags & mesh.Part) != 0)
						continue;

					var newMesh = mesh;

					newMesh.Texture = tex;

					if (mesh.FollowCursor && GlobalSettings.FollowCursor)
						newMesh.Rotate = helmetRotate;

					foreach (var f in mesh.Faces)
						for (int i = 0; i < f.Colors.Length; ++i)
							f.Colors[i] = new Color4(1, 1, 1, 0.25f);

					if (GlobalSettings.Animate && mesh.RotateFactor != 0)
						newMesh.Rotate += new Vector3((float)sinAnim * mesh.RotateFactor, 0, 0);

					_renderer.Meshes.Add(newMesh);
				}

				GL.Enable(EnableCap.Blend);
				_renderer.Render();
				GL.Disable(EnableCap.Blend);
			}

			if (!pickView && GlobalSettings.Transparency != TransparencyMode.Off)
				GL.Enable(EnableCap.Blend);
			else
				GL.Disable(EnableCap.Blend);

			// draw transparent meshes
			foreach (var mesh in CurrentModel.Meshes)
			{
				if (!mesh.Helmet)
					continue;
				if ((GlobalSettings.ViewFlags & mesh.Part) == 0)
					continue;

				var newMesh = mesh;

				newMesh.Texture = tex;

				if (mesh.FollowCursor && GlobalSettings.FollowCursor)
					newMesh.Rotate = helmetRotate;

				foreach (var f in mesh.Faces)
					for (int i = 0; i < f.Colors.Length; ++i)
						f.Colors[i] = Color4.White;

				if (GlobalSettings.Animate && mesh.RotateFactor != 0)
					newMesh.Rotate += new Vector3((float)sinAnim * mesh.RotateFactor, 0, 0);

				_renderer.Meshes.Add(newMesh);
			}

			_renderer.Render();

			// Draw ghosted parts
			if (GlobalSettings.Ghost && !pickView)
			{
				foreach (var mesh in CurrentModel.Meshes)
				{
					if (!mesh.Helmet)
						continue;
					if ((GlobalSettings.ViewFlags & mesh.Part) != 0)
						continue;

					var newMesh = mesh;

					newMesh.Texture = tex;

					if (mesh.FollowCursor && GlobalSettings.FollowCursor)
						newMesh.Rotate = helmetRotate;

					foreach (var f in mesh.Faces)
						for (int i = 0; i < f.Colors.Length; ++i)
							f.Colors[i] = new Color4(1, 1, 1, 0.25f);

					if (GlobalSettings.Animate && mesh.RotateFactor != 0)
						newMesh.Rotate += new Vector3((float)sinAnim * mesh.RotateFactor, 0, 0);

					_renderer.Meshes.Add(newMesh);
				}

				GL.Enable(EnableCap.Blend);
				_renderer.Render();
				GL.Disable(EnableCap.Blend);
			}*/
		}

		Point _pickPosition = new Point(-1, -1);
		bool _isValidPick = false;

		void SetPreview()
		{
			if (_lastSkin == null)
			{
				ColorGrabber preview = new ColorGrabber(_previewPaint, 64, 32);
				preview.Save();
			}
			else
			{
				Skin skin = _lastSkin;

				ColorGrabber currentSkin = new ColorGrabber(GlobalDirtiness.CurrentSkin, skin.Width, skin.Height);
				currentSkin.Load();

				//var pick = GetPick(_mousePoint.X, _mousePoint.Y, ref _pickPosition);
				{
					if (_selectedTool.Tool.RequestPreview(ref currentSkin, skin, _pickPosition.X, _pickPosition.Y))
					{
						currentSkin.Texture = _previewPaint;
						currentSkin.Save();
					}
					else
					{
						currentSkin.Texture = _previewPaint;
						currentSkin.Save();
					}
				}
			}
		}

		/*
		** Invert 4x4 matrix.
		** Contributed by David Moore (See Mesa bug #6748)
		*/
		static bool __gluInvertMatrixf(float[] m, float[] invOut)
		{
			float[] inv = new float[16];
			float det;
			int i;

			inv[0] = m[5] * m[10] * m[15] - m[5] * m[11] * m[14] - m[9] * m[6] * m[15]
					 + m[9] * m[7] * m[14] + m[13] * m[6] * m[11] - m[13] * m[7] * m[10];
			inv[4] = -m[4] * m[10] * m[15] + m[4] * m[11] * m[14] + m[8] * m[6] * m[15]
					 - m[8] * m[7] * m[14] - m[12] * m[6] * m[11] + m[12] * m[7] * m[10];
			inv[8] = m[4] * m[9] * m[15] - m[4] * m[11] * m[13] - m[8] * m[5] * m[15]
					 + m[8] * m[7] * m[13] + m[12] * m[5] * m[11] - m[12] * m[7] * m[9];
			inv[12] = -m[4] * m[9] * m[14] + m[4] * m[10] * m[13] + m[8] * m[5] * m[14]
					 - m[8] * m[6] * m[13] - m[12] * m[5] * m[10] + m[12] * m[6] * m[9];
			inv[1] = -m[1] * m[10] * m[15] + m[1] * m[11] * m[14] + m[9] * m[2] * m[15]
					 - m[9] * m[3] * m[14] - m[13] * m[2] * m[11] + m[13] * m[3] * m[10];
			inv[5] = m[0] * m[10] * m[15] - m[0] * m[11] * m[14] - m[8] * m[2] * m[15]
					 + m[8] * m[3] * m[14] + m[12] * m[2] * m[11] - m[12] * m[3] * m[10];
			inv[9] = -m[0] * m[9] * m[15] + m[0] * m[11] * m[13] + m[8] * m[1] * m[15]
					 - m[8] * m[3] * m[13] - m[12] * m[1] * m[11] + m[12] * m[3] * m[9];
			inv[13] = m[0] * m[9] * m[14] - m[0] * m[10] * m[13] - m[8] * m[1] * m[14]
					 + m[8] * m[2] * m[13] + m[12] * m[1] * m[10] - m[12] * m[2] * m[9];
			inv[2] = m[1] * m[6] * m[15] - m[1] * m[7] * m[14] - m[5] * m[2] * m[15]
					 + m[5] * m[3] * m[14] + m[13] * m[2] * m[7] - m[13] * m[3] * m[6];
			inv[6] = -m[0] * m[6] * m[15] + m[0] * m[7] * m[14] + m[4] * m[2] * m[15]
					 - m[4] * m[3] * m[14] - m[12] * m[2] * m[7] + m[12] * m[3] * m[6];
			inv[10] = m[0] * m[5] * m[15] - m[0] * m[7] * m[13] - m[4] * m[1] * m[15]
					 + m[4] * m[3] * m[13] + m[12] * m[1] * m[7] - m[12] * m[3] * m[5];
			inv[14] = -m[0] * m[5] * m[14] + m[0] * m[6] * m[13] + m[4] * m[1] * m[14]
					 - m[4] * m[2] * m[13] - m[12] * m[1] * m[6] + m[12] * m[2] * m[5];
			inv[3] = -m[1] * m[6] * m[11] + m[1] * m[7] * m[10] + m[5] * m[2] * m[11]
					 - m[5] * m[3] * m[10] - m[9] * m[2] * m[7] + m[9] * m[3] * m[6];
			inv[7] = m[0] * m[6] * m[11] - m[0] * m[7] * m[10] - m[4] * m[2] * m[11]
					 + m[4] * m[3] * m[10] + m[8] * m[2] * m[7] - m[8] * m[3] * m[6];
			inv[11] = -m[0] * m[5] * m[11] + m[0] * m[7] * m[9] + m[4] * m[1] * m[11]
					 - m[4] * m[3] * m[9] - m[8] * m[1] * m[7] + m[8] * m[3] * m[5];
			inv[15] = m[0] * m[5] * m[10] - m[0] * m[6] * m[9] - m[4] * m[1] * m[10]
					 + m[4] * m[2] * m[9] + m[8] * m[1] * m[6] - m[8] * m[2] * m[5];

			det = m[0] * inv[0] + m[1] * inv[4] + m[2] * inv[8] + m[3] * inv[12];
			if (det == 0)
				return false;

			det = 1.0f / det;

			for (i = 0; i < 16; i++)
				invOut[i] = inv[i] * det;

			return true;
		}

		static void __gluMultMatricesf(float[] a, float[] b,
			float[] r)
		{
			int i, j;

			for (i = 0; i < 4; i++)
			{
				for (j = 0; j < 4; j++)
				{
					r[i * 4 + j] =
				 a[i * 4 + 0] * b[0 * 4 + j] +
				 a[i * 4 + 1] * b[1 * 4 + j] +
				 a[i * 4 + 2] * b[2 * 4 + j] +
				 a[i * 4 + 3] * b[3 * 4 + j];
				}
			}
		}

		static void __gluMultMatrixVecf(float[] matrix, float[] inMatrix,
				float[] outMatrix)
		{
			int i;

			for (i = 0; i < 4; i++)
			{
				outMatrix[i] =
			 inMatrix[0] * matrix[0 * 4 + i] +
			 inMatrix[1] * matrix[1 * 4 + i] +
			 inMatrix[2] * matrix[2 * 4 + i] +
			 inMatrix[3] * matrix[3 * 4 + i];
			}
		}

		static bool gluUnProject(float winx, float winy, float winz,
		  float[] modelMatrix,
		  float[] projMatrix, int[] viewport,
				 out float objx, out float objy, out float objz)
		{
			objx = objy = objz = float.NaN;

			float[] finalMatrix = new float[16];
			float[] inMatrix = new float[4];
			float[] outMatrix = new float[4];

			__gluMultMatricesf(modelMatrix, projMatrix, finalMatrix);
			if (!__gluInvertMatrixf(finalMatrix, finalMatrix))
				return false;

			inMatrix[0] = winx;
			inMatrix[1] = winy;
			inMatrix[2] = winz;
			inMatrix[3] = 1.0f;

			/* Map x and y from window coordinates */
			inMatrix[0] = (inMatrix[0] - viewport[0]) / viewport[2];
			inMatrix[1] = (inMatrix[1] - viewport[1]) / viewport[3];

			/* Map to range -1 to 1 */
			inMatrix[0] = inMatrix[0] * 2 - 1;
			inMatrix[1] = inMatrix[1] * 2 - 1;
			inMatrix[2] = inMatrix[2] * 2 - 1;

			__gluMultMatrixVecf(finalMatrix, inMatrix, outMatrix);
			if (outMatrix[3] == 0.0)
				return false;
			outMatrix[0] /= outMatrix[3];
			outMatrix[1] /= outMatrix[3];
			outMatrix[2] /= outMatrix[3];
			objx = outMatrix[0];
			objy = outMatrix[1];
			objz = outMatrix[2];
			return true;
		}

		/**
			  *  Return true if triangle or quad intersects with segment and the distance is 
			  *  stored in dist[0].
			  * */

		private static bool segmentAndPoly(Vector3d[] coordinates,
				Vector3d start, Vector3d end, out double dist)
		{
			dist = float.NaN;
			Vector3d vec0 = new Vector3d(); // Edge vector from point 0 to point 1;
			Vector3d vec1 = new Vector3d(); // Edge vector from point 0 to point 2 or 3;
			Vector3d pNrm = new Vector3d();
			double absNrmX, absNrmY, absNrmZ, pD = 0.0;
			Vector3d tempV3d = new Vector3d();
			Vector3d direction = new Vector3d();
			double pNrmDotrDir = 0.0;
			int axis, nc, sh, nsh;

			Vector3d iPnt = new Vector3d(); // Point of intersection.

			double[] uCoor = new double[4]; // Only need to support up to quad.
			double[] vCoor = new double[4];
			double tempD;

			int i, j;

			// Compute plane normal.
			for (i = 0; i < coordinates.Length - 1; )
			{
				vec0.X = coordinates[i + 1].X - coordinates[i].X;
				vec0.Y = coordinates[i + 1].Y - coordinates[i].Y;
				vec0.Z = coordinates[i + 1].Z - coordinates[i++].Z;
				if (vec0.Length > 0.0)
					break;
			}

			for (j = i; j < coordinates.Length - 1; j++)
			{
				vec1.X = coordinates[j + 1].X - coordinates[j].X;
				vec1.Y = coordinates[j + 1].Y - coordinates[j].Y;
				vec1.Z = coordinates[j + 1].Z - coordinates[j].Z;
				if (vec1.Length > 0.0)
					break;
			}

			if (j == (coordinates.Length - 1))
			{
				// System.out.println("(1) Degenerated polygon.");
				return false; // Degenerated polygon.
			}

			/* 
			   System.out.println("Triangle/Quad :");
			   for(i=0; i<coordinates.length; i++) 
			   System.out.println("P" + i + " " + coordinates[i]);
			 */

			pNrm = Vector3d.Cross(vec0, vec1);

			if (pNrm.Length == 0.0)
			{
				// System.out.println("(2) Degenerated polygon.");
				return false; // Degenerated polygon.
			}
			// Compute plane D.
			tempV3d = new Vector3d(coordinates[0].X, coordinates[0].Y, coordinates[0].Z);
			Vector3d.Dot(ref pNrm, ref tempV3d, out pD);

			// System.out.println("Segment start : " + start + " end " + end);

			direction.X = end.X - start.X;
			direction.Y = end.Y - start.Y;
			direction.Z = end.Z - start.Z;

			Vector3d.Dot(ref pNrm, ref direction, out pNrmDotrDir);

			// Segment is parallel to plane. 
			if (pNrmDotrDir == 0.0)
			{
				// System.out.println("Segment is parallel to plane.");
				return false;
			}

			tempV3d = new Vector3d(start.X, start.Y, start.Z);

			dist = (pD - Vector3d.Dot(pNrm, tempV3d)) / pNrmDotrDir;

			// Segment intersects the plane behind the segment's start.
			// or exceed the segment's length.
			if ((dist < 0.0) || (dist > 1.0))
			{
				// System.out.println("Segment intersects the plane behind the start or exceed end.");
				return false;
			}

			// Now, one thing for sure the segment intersect the plane.
			// Find the intersection point.
			iPnt.X = start.X + direction.X * dist;
			iPnt.Y = start.Y + direction.Y * dist;
			iPnt.Z = start.Z + direction.Z * dist;

			// System.out.println("dist " + dist[0] + " iPnt : " + iPnt);

			// Project 3d points onto 2d plane and apply Jordan curve theorem. 
			// Note : Area of polygon is not preserve in this projection, but
			// it doesn't matter here. 

			// Find the axis of projection.
			absNrmX = Math.Abs(pNrm.X);
			absNrmY = Math.Abs(pNrm.Y);
			absNrmZ = Math.Abs(pNrm.Z);

			if (absNrmX > absNrmY)
				axis = 0;
			else
				axis = 1;

			if (axis == 0)
			{
				if (absNrmX < absNrmZ)
					axis = 2;
			}
			else if (axis == 1)
			{
				if (absNrmY < absNrmZ)
					axis = 2;
			}

			// System.out.println("Normal " + pNrm + " axis " + axis );

			for (i = 0; i < coordinates.Length; i++)
			{
				switch (axis)
				{
				case 0:
					uCoor[i] = coordinates[i].Y - iPnt.Y;
					vCoor[i] = coordinates[i].Z - iPnt.Z;
					break;

				case 1:
					uCoor[i] = coordinates[i].X - iPnt.X;
					vCoor[i] = coordinates[i].Z - iPnt.Z;
					break;

				case 2:
					uCoor[i] = coordinates[i].X - iPnt.X;
					vCoor[i] = coordinates[i].Y - iPnt.Y;
					break;
				}

				// System.out.println("i " + i + " u " + uCoor[i] + " v " + vCoor[i]); 
			}

			// initialize number of crossing, nc.
			nc = 0;

			if (vCoor[0] < 0.0)
				sh = -1;
			else
				sh = 1;

			for (i = 0; i < coordinates.Length; i++)
			{
				j = i + 1;
				if (j == coordinates.Length)
					j = 0;

				if (vCoor[j] < 0.0)
					nsh = -1;
				else
					nsh = 1;

				if (sh != nsh)
				{
					if ((uCoor[i] > 0.0) && (uCoor[j] > 0.0))
					{
						// This line must cross U+.
						nc++;
					}
					else if ((uCoor[i] > 0.0) || (uCoor[j] > 0.0))
					{
						// This line might cross U+. We need to compute intersection on U azis.
						tempD = uCoor[i] - vCoor[i] * (uCoor[j] - uCoor[i])
								/ (vCoor[j] - vCoor[i]);
						if (tempD > 0)
							// This line cross U+.
							nc++;
					}
					sh = nsh;
				} // sh != nsh
			}

			// System.out.println("nc " + nc);

			if ((nc % 2) == 1)
			{

				// calculate the distance
				dist *= direction.Length;

				// System.out.println("Segment Intersected!");	
				/* 
				   System.out.println("Segment orgin : " + start + " dir " + direction);
				   System.out.println("Triangle/Quad :");
				   for(i=0; i<coordinates.length; i++) 
				   System.out.println("P" + i + " " + coordinates[i]);
				   System.out.println("dist " + dist[0] + " iPnt : " + iPnt);
				 */
				return true;
			}
			else
			{
				// System.out.println("Segment Not Intersected!");
				return false;
			}
		}


		public bool GetPick(int x, int y, ref Point hitPixel)
		{
			if (x == -1 || y == -1)
				return false;

			rendererControl.MakeCurrent();

			GL.ClearColor(Color.White);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.ClearColor(GlobalSettings.BackgroundColor);

			var skin = _lastSkin;

			if (skin == null)
				return false;

			if (_currentViewMode == ViewMode.Perspective)
			{
				Setup3D(new Rectangle(0, 0, rendererControl.Width, rendererControl.Height));
				DrawPlayer(GetPaintTexture(skin.Width, skin.Height), skin, true);
			}
			else if (_currentViewMode == ViewMode.Orthographic)
			{
				Setup2D(new Rectangle(0, 0, rendererControl.Width, rendererControl.Height));
				DrawPlayer2D(GetPaintTexture(skin.Width, skin.Height), skin, true);
			}
			else
			{
				int halfHeight = (int)Math.Ceiling(rendererControl.Height / 2.0f);

				Setup3D(new Rectangle(0, 0, rendererControl.Width, halfHeight));
				DrawPlayer(GetPaintTexture(skin.Width, skin.Height), skin, true);

				Setup2D(new Rectangle(0, halfHeight, rendererControl.Width, halfHeight));
				DrawPlayer2D(GetPaintTexture(skin.Width, skin.Height), skin, true);
			}

			GL.Flush();

			byte[] pixel = new byte[4];

			GL.ReadPixels(x, rendererControl.Height - y, 1, 1,
				PixelFormat.Rgb, PixelType.UnsignedByte, pixel);

			uint pixVal = BitConverter.ToUInt32(pixel, 0);

			if (pixVal != 0xFFFFFF)
			{
				hitPixel = new Point((int)(pixVal % skin.Width), (int)(pixVal / skin.Width));
				return true;
			}

			return false;
		}

		public Color SelectedColor
		{
			get { return (_secondaryIsFront) ? _secondaryColor : _primaryColor; }
			set { SetColor(value); }
		}

		public Color UnselectedColor
		{
			get { return (!_secondaryIsFront) ? _secondaryColor : _primaryColor; }
			set
			{
				if (_secondaryIsFront)
					SetColor(colorPreview1, ref _primaryColor, value);
				else
					SetColor(colorPreview2, ref _secondaryColor, value);
			}
		}

		void UseToolOnViewport(int x, int y, bool begin = false)
		{
			if (_lastSkin == null)
				return;

			if (_isValidPick)
			{
				Skin skin = _lastSkin;

				ColorGrabber currentSkin = new ColorGrabber(GlobalDirtiness.CurrentSkin, skin.Width, skin.Height);
				currentSkin.Load();

				if (_selectedTool.Tool.MouseMoveOnSkin(ref currentSkin, skin, _pickPosition.X, _pickPosition.Y))
				{
					SetCanSave(true);
					skin.Dirty = true;
					currentSkin.Save();
				}
			}

			rendererControl.Invalidate();
		}

		#region File uploading (FIXME: REMOVE)
		public static Exception HttpUploadFile(string url, string file, string paramName, string contentType, Dictionary<string, byte[]> nvc, CookieContainer cookies)
		{
			//log.Debug(string.Format("Uploading {0} to {1}", file, url));
			string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
			byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

			HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
			wr.ContentType = "multipart/form-data; boundary=" + boundary;
			wr.Method = "POST";
			wr.KeepAlive = true;
			wr.CookieContainer = cookies;
			wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
			wr.Timeout = 10000;

			Stream rs = wr.GetRequestStream();

			string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
			foreach (var kvp in nvc)
			{
				rs.Write(boundarybytes, 0, boundarybytes.Length);
				string formitem = string.Format(formdataTemplate, kvp.Key, Encoding.ASCII.GetString(kvp.Value));
				byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
				rs.Write(formitembytes, 0, formitembytes.Length);
			}
			rs.Write(boundarybytes, 0, boundarybytes.Length);

			string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
			string header = string.Format(headerTemplate, paramName, Path.GetFileName(file), contentType);
			byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
			rs.Write(headerbytes, 0, headerbytes.Length);

			FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
			byte[] buffer = new byte[4096];
			int bytesRead = 0;
			while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
			{
				rs.Write(buffer, 0, bytesRead);
			}
			fileStream.Close();

			byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
			rs.Write(trailer, 0, trailer.Length);
			rs.Close();

			WebResponse wresp = null;
			Exception ret = null;
			try
			{
				wresp = wr.GetResponse();
				Stream stream2 = wresp.GetResponseStream();
				StreamReader reader2 = new StreamReader(stream2);
				//log.Debug(string.Format("File uploaded, server response is: {0}", reader2.ReadToEnd()));
			}
			catch (Exception ex)
			{
				//log.Error("Error uploading file", ex);
				if (wresp != null)
				{
					wresp.Close();
					wresp = null;
				}

				ret = ex;
			}
			finally
			{
				wr = null;
			}

			return ret;
		}

		public enum ErrorCodes
		{
			Succeeded,
			TimeOut,
			WrongCredentials,
			Unknown
		}

		class ErrorReturn
		{
			public ErrorCodes Code;
			public Exception Exception;
			public string ReportedError;
		}

		void UploadThread(object param)
		{
			var parms = (object[])param;
			ErrorReturn error = (ErrorReturn)parms[3];

			error.Code = ErrorCodes.Succeeded;
			error.Exception = null;
			error.ReportedError = null;

			try
			{
				CookieContainer cookies = new CookieContainer();
				var request = (HttpWebRequest)HttpWebRequest.Create("http://www.minecraft.net/login");
				request.CookieContainer = cookies;
				request.Timeout = 10000;
				var response = request.GetResponse();
				StreamReader sr = new StreamReader(response.GetResponseStream());
				var text = sr.ReadToEnd();

				var match = Regex.Match(text, @"<input type=""hidden"" name=""authenticityToken"" value=""(.*?)"">");
				string authToken = null;
				if (match.Success)
					authToken = match.Groups[1].Value;

				if (authToken == null)
					return;

				sr.Dispose();

				response.Close();

				string requestTemplate = @"authenticityToken={0}&redirect=http%3A%2F%2Fwww.minecraft.net%2Fprofile&username={1}&password={2}";
				string requestContent = string.Format(requestTemplate, authToken, parms[0].ToString(), parms[1].ToString());
				var inBytes = Encoding.UTF8.GetBytes(requestContent);

				// craft the login request
				request = (HttpWebRequest)HttpWebRequest.Create("https://www.minecraft.net/login");
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.CookieContainer = cookies;
				request.ContentLength = inBytes.Length;
				request.Timeout = 10000;

				using (Stream dataStream = request.GetRequestStream())
					dataStream.Write(inBytes, 0, inBytes.Length);

				response = request.GetResponse();
				sr = new StreamReader(response.GetResponseStream());
				text = sr.ReadToEnd();

				match = Regex.Match(text, @"<p class=""error"">([\w\W]*?)</p>");

				sr.Dispose();
				response.Close();

				if (match.Success)
				{
					error.ReportedError = match.Groups[1].Value.Trim();
					error.Code = ErrorCodes.WrongCredentials;
				}
				else
				{
					var dict = new Dictionary<string, byte[]>();
					dict.Add("authenticityToken", Encoding.ASCII.GetBytes(authToken));
					if ((error.Exception = HttpUploadFile("http://www.minecraft.net/profile/skin", parms[2].ToString(), "skin", "image/png", dict, cookies)) != null)
						error.Code = ErrorCodes.Unknown;
				}
			}
			catch (Exception ex)
			{
				error.Exception = ex;
			}
			finally
			{
				Invoke((Action)delegate() { _pleaseWaitForm.Close(); });
			}
		}

		Thread _uploadThread;
		Login login = new Login();

		void PerformUpload()
		{
			if (_lastSkin == null)
				return;

			if (_lastSkin.Width != 64 || _lastSkin.Height != 32)
			{
				MessageBox.Show(this, GetLanguageString("B_MSG_UPLOADRES"));
				return;
			}

			login.Username = GlobalSettings.LastUsername;
			login.Password = GlobalSettings.LastPassword;

			bool dialogRes = true;
			bool didShowDialog = false;

			if ((ModifierKeys & Keys.Shift) != 0 || !GlobalSettings.RememberMe || !GlobalSettings.AutoLogin)
			{
				login.Remember = GlobalSettings.RememberMe;
				login.AutoLogin = GlobalSettings.AutoLogin;
				dialogRes = login.ShowDialog() == System.Windows.Forms.DialogResult.OK;
				didShowDialog = true;
			}

			if (!dialogRes)
				return;

			_pleaseWaitForm = new PleaseWait();
			_pleaseWaitForm.FormClosed += new FormClosedEventHandler(_pleaseWaitForm_FormClosed);

			_uploadThread = new Thread(UploadThread);
			ErrorReturn ret = new ErrorReturn();
			_uploadThread.Start(new object[] { login.Username, login.Password, _lastSkin.File.FullName, ret });

			_pleaseWaitForm.DialogResult = DialogResult.OK;
			_pleaseWaitForm.languageProvider1.LanguageChanged(CurrentLanguage);
			_pleaseWaitForm.ShowDialog();
			_uploadThread = null;

			if (ret.ReportedError != null)
				MessageBox.Show(this, GetLanguageString("B_MSG_UPLOADERROR") + "\r\n" + ret.ReportedError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			else if (ret.Exception != null)
				MessageBox.Show(this, GetLanguageString("B_MSG_UPLOADERROR") + "\r\n" + ret.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			else if (_pleaseWaitForm.DialogResult != DialogResult.Abort)
			{
				MessageBox.Show(this, GetLanguageString("B_MSG_UPLOADSUCCESS"), "Woo!", MessageBoxButtons.OK, MessageBoxIcon.Information);
				GlobalSettings.LastSkin = _lastSkin.Name;
				treeView1.Invalidate();
			}

			if (didShowDialog)
			{
				GlobalSettings.RememberMe = login.Remember;
				GlobalSettings.AutoLogin = login.AutoLogin;

				if (GlobalSettings.RememberMe == false)
					GlobalSettings.LastUsername = GlobalSettings.LastPassword = "";
				else
				{
					GlobalSettings.LastUsername = login.Username;
					GlobalSettings.LastPassword = login.Password;
				}
			}
		}

		void _pleaseWaitForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			_uploadThread.Abort();
		}
		#endregion

		void ToggleAnimation()
		{
			animateToolStripMenuItem.Checked = !animateToolStripMenuItem.Checked;
			GlobalSettings.Animate = animateToolStripMenuItem.Checked;

			Invalidate();
		}

		void ToggleFollowCursor()
		{
			followCursorToolStripMenuItem.Checked = !followCursorToolStripMenuItem.Checked;
			GlobalSettings.FollowCursor = followCursorToolStripMenuItem.Checked;

			Invalidate();
		}

		void ToggleGrass()
		{
			grassToolStripMenuItem.Checked = !grassToolStripMenuItem.Checked;
			GlobalSettings.Grass = grassToolStripMenuItem.Checked;

			rendererControl.Invalidate();
		}

		void ToggleGhosting()
		{
			ghostHiddenPartsToolStripMenuItem.Checked = !ghostHiddenPartsToolStripMenuItem.Checked;
			GlobalSettings.Ghost = ghostHiddenPartsToolStripMenuItem.Checked;

			rendererControl.Invalidate();
		}

		void Perform10Mode()
		{
			ModelLoader.InvertBottomFaces();

			modeToolStripMenuItem1.Checked = !modeToolStripMenuItem1.Checked;
			GlobalSettings.OnePointOhMode = modeToolStripMenuItem1.Checked;

			rendererControl.Invalidate();
		}

		#region Skin Management
		void ImportSkin(string fileName, string folderLocation, TreeNode parentNode)
		{
			var name = Path.GetFileNameWithoutExtension(fileName);

			while (File.Exists(folderLocation + name + ".png"))
				name += " (New)";

			AddIgnoreFile(folderLocation + name + ".png");
			File.Copy(fileName, folderLocation + name + ".png");

			Skin skin = new Skin(folderLocation + name + ".png");

			if (parentNode != null)
			{
				if (!(parentNode is Skin))
					parentNode.Nodes.Add(skin);
				else
					parentNode.GetParentCollection().Add(skin);
			}
			else
				treeView1.Nodes.Add(skin);

			skin.SetImages();
		}

		void ImportSkins(string[] fileName, TreeNode parentNode)
		{
			string folderLocation;
			if (parentNode != null)
			{
				if (!(parentNode is Skin))
					folderLocation = "Skins\\" + parentNode.FullPath + '\\';
				else if (parentNode.Parent != null)
					folderLocation = "Skins\\" + parentNode.Parent.FullPath + '\\';
				else
					folderLocation = "Skins\\";
			}
			else
				folderLocation = "Skins\\";

			foreach (var f in fileName)
				ImportSkin(f, folderLocation, parentNode);
		}

		void PerformImportSkin()
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.Filter = "Minecraft Skins|*.png";
				ofd.Multiselect = true;

				if (ofd.ShowDialog() == DialogResult.OK)
				{
					if (_rightClickedNode == null)
						_rightClickedNode = treeView1.SelectedNode;

					ImportSkins(ofd.FileNames, _rightClickedNode);
				}
			}
		}

		void PerformNewFolder()
		{
			string folderLocation;
			TreeNodeCollection collection;

			if (_rightClickedNode == null)
				_rightClickedNode = treeView1.SelectedNode;

			if (_rightClickedNode != null)
			{
				if (!(_rightClickedNode is Skin))
				{
					folderLocation = "Skins\\" + _rightClickedNode.FullPath + '\\';
					collection = _rightClickedNode.Nodes;
				}
				else if (_rightClickedNode.Parent != null)
				{
					folderLocation = "Skins\\" + _rightClickedNode.Parent.FullPath + '\\';
					collection = _rightClickedNode.Parent.Nodes;
				}
				else
				{
					folderLocation = "Skins\\";
					collection = treeView1.Nodes;
				}
			}
			else
			{
				folderLocation = "Skins\\";
				collection = treeView1.Nodes;
			}

			string newFolderName = "New Folder";

			while (Directory.Exists(folderLocation + newFolderName))
				newFolderName = newFolderName.Insert(0, Editor.GetLanguageString("C_NEW"));

			Directory.CreateDirectory(folderLocation + newFolderName);
			var newNode = new FolderNode(newFolderName);
			collection.Add(newNode);

			newNode.EnsureVisible();
			treeView1.SelectedNode = newNode;
			treeView1.Invalidate();

			PerformNameChange();
		}

		void PerformNewSkin()
		{
			string folderLocation;
			TreeNodeCollection collection;

			if (_rightClickedNode == null)
				_rightClickedNode = treeView1.SelectedNode;

			if (_rightClickedNode != null)
			{
				if (!(_rightClickedNode is Skin))
				{
					folderLocation = "Skins\\" + _rightClickedNode.FullPath + '\\';
					collection = _rightClickedNode.Nodes;
				}
				else if (_rightClickedNode.Parent != null)
				{
					folderLocation = "Skins\\" + _rightClickedNode.Parent.FullPath + '\\';
					collection = _rightClickedNode.Parent.Nodes;
				}
				else
				{
					folderLocation = "Skins\\";
					collection = treeView1.Nodes;
				}
			}
			else
			{
				folderLocation = "Skins\\";
				collection = treeView1.Nodes;
			}

			string newSkinName = "New Skin";

			while (File.Exists(folderLocation + newSkinName + ".png"))
				newSkinName = newSkinName.Insert(0, Editor.GetLanguageString("C_NEW"));

			using (Bitmap bmp = new Bitmap(64, 32))
			{
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.Clear(Color.FromArgb(0, 255, 255, 255));

					g.FillRectangle(System.Drawing.Brushes.White, 0, 0, 32, 32);
					g.FillRectangle(System.Drawing.Brushes.White, 32, 16, 32, 16);
				}

				bmp.Save(folderLocation + newSkinName + ".png");
			}

			Skin newSkin = new Skin(folderLocation + newSkinName + ".png");
			collection.Add(newSkin);
			newSkin.SetImages();

			newSkin.EnsureVisible();
			treeView1.SelectedNode = newSkin;
			treeView1.Invalidate();

			PerformNameChange();
		}

		void RecursiveDeleteSkins(TreeNode node)
		{
			foreach (TreeNode sub in node.Nodes)
			{
				if (!(sub is Skin))
					RecursiveDeleteSkins(sub);
				else
				{
					Skin skin = (Skin)sub;

					if (_lastSkin == skin)
						_lastSkin = null;

					skin.Dispose();
				}
			}

			Directory.Delete("Skins\\" + node.FullPath, true);
		}

		void PerformDeleteSkin()
		{
			if (treeView1.SelectedNode is Skin)
			{
				if (MessageBox.Show(this, GetLanguageString("B_MSG_DELETESKIN"), GetLanguageString("B_CAP_QUESTION"), MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
				{
					Skin skin = (Skin)treeView1.SelectedNode;

					AddIgnoreFile(skin.File.FullName);
					skin.Delete();
					skin.Remove();
					skin.Dispose();

					treeView1_AfterSelect(treeView1, new TreeViewEventArgs(treeView1.SelectedNode));

					Invalidate();
				}
			}
			else
			{
				if (MessageBox.Show(this, GetLanguageString("B_MSG_DELETEFOLDER"), GetLanguageString("B_CAP_QUESTION"), MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Yes)
				{
					DirectoryInfo folder = new DirectoryInfo("Skins\\" + treeView1.SelectedNode.FullPath);

					Editor.AddIgnoreFile(folder.FullName);
					RecursiveDeleteSkins(treeView1.SelectedNode);

					treeView1.SelectedNode.Remove();
					treeView1_AfterSelect(treeView1, new TreeViewEventArgs(treeView1.SelectedNode));
					Invalidate();
				}
			}
		}

		void PerformCloneSkin()
		{
			if (treeView1.SelectedNode == null ||
				!(treeView1.SelectedNode is Skin))
				return;

			Skin skin = (Skin)treeView1.SelectedNode;
			string newName = skin.Name;
			string newFileName;

			do
			{
				newName += " - Copy";
				newFileName = skin.Directory.FullName + '\\' + newName + ".png";
			} while (File.Exists(newFileName));

			Editor.AddIgnoreFile(newFileName);
			File.Copy(skin.File.FullName, newFileName);
			Skin newSkin = new Skin(newFileName);

			skin.GetParentCollection().Add(newSkin);

			newSkin.SetImages();
		}

		TreeNode _currentlyEditing = null;
		void PerformNameChange()
		{
			if (treeView1.SelectedNode != null)
			{
				_currentlyEditing = treeView1.SelectedNode;

				if (_currentlyEditing is Skin)
					labelEditTextBox.Text = ((Skin)_currentlyEditing).Name;
				else
					labelEditTextBox.Text = _currentlyEditing.Text;

				labelEditTextBox.Show();
				labelEditTextBox.Location = new Point(treeView1.SelectedNode.Bounds.Location.X + 24 + (treeView1.SelectedNode.Level * 1), treeView1.Location.Y + treeView1.SelectedNode.Bounds.Location.Y + 4);
				labelEditTextBox.Size = new System.Drawing.Size(treeView1.Width - labelEditTextBox.Location.X - 20, labelEditTextBox.Height);
				labelEditTextBox.BringToFront();
				labelEditTextBox.Focus();
			}
		}
		#endregion

		private void DoneEditingNode(string newName, TreeNode _currentlyEditing)
		{
			labelEditTextBox.Hide();

			if (_currentlyEditing is Skin)
			{
				Skin skin = (Skin)_currentlyEditing;

				if (skin.Name == newName)
					return;

				if (skin.ChangeName(newName) == false)
					System.Media.SystemSounds.Beep.Play();
			}
			else
			{
				string folderName = _currentlyEditing.Text;
				var folder = new DirectoryInfo("skins\\" + _currentlyEditing.FullPath);
				var newFolder = new DirectoryInfo("skins\\" + ((_currentlyEditing.Parent != null) ? (_currentlyEditing.Parent.FullPath + '\\' + newName) : newName));

				if (folderName == newName)
					return;

				if (Directory.Exists(newFolder.FullName))
				{
					System.Media.SystemSounds.Beep.Play();
					return;
				}

				AddIgnoreFile(newFolder.FullName);
				AddIgnoreFile(folder.FullName);
				folder.MoveTo(newFolder.FullName);
				_currentlyEditing.Text = _currentlyEditing.Name = newFolder.Name;
			}
		}

		#region Saving
		void SetCanSave(bool value)
		{
			saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = value;
		}

		void PerformSaveAs()
		{
			var skin = _lastSkin;

			RenderState.BindTexture(GlobalDirtiness.CurrentSkin);
			ColorGrabber grabber = new ColorGrabber(GlobalDirtiness.CurrentSkin, skin.Width, skin.Height);
			grabber.Load();

			Bitmap b = new Bitmap(skin.Width, skin.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			var locked = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			unsafe
			{
				fixed (void *inPixels = grabber.Array)
				{
					void *outPixels = locked.Scan0.ToPointer();

					int *inInt = (int*)inPixels;
					int *outInt = (int*)outPixels;

					for (int y = 0; y < b.Height; ++y)
						for (int x = 0; x < b.Width; ++x)
						{
							var color = Color.FromArgb((*inInt >> 24) & 0xFF, (*inInt >> 0) & 0xFF, (*inInt >> 8) & 0xFF, (*inInt >> 16) & 0xFF);
							*outInt = color.ToArgb();

							inInt++;
							outInt++;
						}
				}
			}

			b.UnlockBits(locked);

			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "Skin Image|*.png";

				if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
					b.Save(sfd.FileName);
			}

			b.Dispose();
		}

		void PerformSaveSkin(Skin s)
		{
			rendererControl.MakeCurrent();

			s.CommitChanges((s == _lastSkin) ? GlobalDirtiness.CurrentSkin : s.GLImage, true);
		}

		bool RecursiveNodeIsDirty(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				if (node is Skin)
				{
					Skin skin = (Skin)node;

					if (skin.Dirty)
						return true;
				}
				else
					if (RecursiveNodeIsDirty(node.Nodes))
						return true;
			}

			return false;
		}

		void RecursiveNodeSave(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				if (node is Skin)
				{
					Skin skin = (Skin)node;

					if (skin.Dirty)
						PerformSaveSkin(skin);
				}
				else
					RecursiveNodeSave(node.Nodes);
			}
		}

		void PerformSaveAll()
		{
			RecursiveNodeSave(treeView1.Nodes);
			treeView1.Invalidate();
		}

		void PerformSave()
		{
			Skin skin = _lastSkin;

			if (!skin.Dirty)
				return;

			SetCanSave(false);
			PerformSaveSkin(skin);
			treeView1.Invalidate();
		}
		#endregion

		void PerformUndo()
		{
			if (!_currentUndoBuffer.CanUndo)
				return;

			rendererControl.MakeCurrent();

			_currentUndoBuffer.Undo();

			undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = _currentUndoBuffer.CanUndo;
			redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = _currentUndoBuffer.CanRedo;

			Skin current = _lastSkin;
			SetCanSave(current.Dirty = true);

			rendererControl.Invalidate();
		}

		void PerformRedo()
		{
			if (!_currentUndoBuffer.CanRedo)
				return;

			rendererControl.MakeCurrent();

			_currentUndoBuffer.Redo();

			Skin current = _lastSkin;
			SetCanSave(current.Dirty = true);

			undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = _currentUndoBuffer.CanUndo;
			redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = _currentUndoBuffer.CanRedo;

			rendererControl.Invalidate();
		}

		Paril.Controls.Color.ColorPreview SelectedColorPreview
		{
			get { return (_secondaryIsFront) ? colorPreview2 : colorPreview1; }
		}

		void SetColor(Control colorPreview, ref Color currentColor, Color newColor)
		{
			currentColor = newColor;
			colorPreview.ForeColor = currentColor;

			if (colorPreview != SelectedColorPreview)
				return;

			var hsl = Devcorp.Controls.Design.ColorSpaceHelper.RGBtoHSL(newColor);

			_skipColors = true;
			redNumericUpDown.Value = newColor.R;
			greenNumericUpDown.Value = newColor.G;
			blueNumericUpDown.Value = newColor.B;
			alphaNumericUpDown.Value = newColor.A;

			colorSquare.CurrentHue = (int)hsl.Hue;
			colorSquare.CurrentSat = (int)(hsl.Saturation * 240);
			saturationSlider.CurrentLum = (int)(hsl.Luminance * 240);

			hueNumericUpDown.Value = colorSquare.CurrentHue;
			saturationNumericUpDown.Value = colorSquare.CurrentSat;
			luminanceNumericUpDown.Value = saturationSlider.CurrentLum;

			redRenderer.StartColor = Color.FromArgb(255, 0, currentColor.G, currentColor.B);
			greenRenderer.StartColor = Color.FromArgb(255, currentColor.R, 0, currentColor.B);
			blueRenderer.StartColor = Color.FromArgb(255, currentColor.R, currentColor.G, 0);

			redRenderer.EndColor = Color.FromArgb(255, 255, currentColor.G, currentColor.B);
			greenRenderer.EndColor = Color.FromArgb(255, currentColor.R, 255, currentColor.B);
			blueRenderer.EndColor = Color.FromArgb(255, currentColor.R, currentColor.G, 255);

			hueRenderer.Saturation = colorSquare.CurrentSat;
			hueRenderer.Luminance = saturationSlider.CurrentLum;

			saturationRenderer.Luminance = saturationSlider.CurrentLum;
			saturationRenderer.Hue = colorSquare.CurrentHue;

			lightnessRenderer.Hue = colorSquare.CurrentHue;
			lightnessRenderer.Saturation = colorSquare.CurrentSat;

			redColorSlider.Value = currentColor.R;
			greenColorSlider.Value = currentColor.G;
			blueColorSlider.Value = currentColor.B;
			alphaColorSlider.Value = currentColor.A;

			hueColorSlider.Value = colorSquare.CurrentHue;
			saturationColorSlider.Value = colorSquare.CurrentSat;
			lightnessColorSlider.Value = saturationSlider.CurrentLum;

			if (!_editingHex)
				textBox1.Text = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", newColor.R, newColor.G, newColor.B, newColor.A);

			_skipColors = false;
		}

		void SetColor(Color c)
		{
			if (_secondaryIsFront)
				SetColor(colorPreview2, ref _secondaryColor, c);
			else
				SetColor(colorPreview1, ref _primaryColor, c);
		}

		void SetViewMode(ViewMode newMode)
		{
			perspectiveToolStripButton.Checked = orthographicToolStripButton.Checked = hybridToolStripButton.Checked = false;
			perspectiveToolStripMenuItem.Checked = textureToolStripMenuItem.Checked = hybridViewToolStripMenuItem.Checked = false;
			_currentViewMode = newMode;

			switch (_currentViewMode)
			{
			case ViewMode.Orthographic:
				orthographicToolStripButton.Checked = true;
				textureToolStripMenuItem.Checked = true;
				break;
			case ViewMode.Perspective:
				perspectiveToolStripButton.Checked = true;
				perspectiveToolStripMenuItem.Checked = true;
				break;
			case ViewMode.Hybrid:
				hybridToolStripButton.Checked = true;
				hybridViewToolStripMenuItem.Checked = true;
				break;
			}

			rendererControl.Invalidate();
		}

		void SetTransparencyMode(TransparencyMode trans)
		{
			offToolStripMenuItem.Checked = helmetOnlyToolStripMenuItem.Checked = allToolStripMenuItem.Checked = false;
			GlobalSettings.Transparency = trans;

			switch (GlobalSettings.Transparency)
			{
			case TransparencyMode.Off:
				offToolStripMenuItem.Checked = true;
				break;
			case TransparencyMode.Helmet:
				helmetOnlyToolStripMenuItem.Checked = true;
				break;
			case TransparencyMode.All:
				allToolStripMenuItem.Checked = true;
				break;
			}

			rendererControl.Invalidate();
		}

		ToolStripMenuItem[] _toggleMenuItems;
		ToolStripButton[] _toggleButtons;
		void SetVisibleParts()
		{
			if (_toggleMenuItems == null)
			{
				_toggleMenuItems = new ToolStripMenuItem[] { headToolStripMenuItem, helmetToolStripMenuItem, chestToolStripMenuItem, leftArmToolStripMenuItem, rightArmToolStripMenuItem, leftLegToolStripMenuItem, rightLegToolStripMenuItem };
				_toggleButtons = new ToolStripButton[] { toggleHeadToolStripButton, toggleHelmetToolStripButton, toggleChestToolStripButton, toggleLeftArmToolStripButton, toggleRightArmToolStripButton, toggleLeftLegToolStripButton, toggleRightLegToolStripButton };
			}

			for (int i = 0; i < _toggleButtons.Length; ++i)
				_toggleMenuItems[i].Checked = _toggleButtons[i].Checked = ((GlobalSettings.ViewFlags & (VisiblePartFlags)(1 << i)) != 0);
		}

		void ToggleVisiblePart(VisiblePartFlags flag)
		{
			GlobalSettings.ViewFlags ^= flag;

			bool hasNow = (GlobalSettings.ViewFlags & flag) != 0;

			ToolStripMenuItem item = null;
			ToolStripButton itemButton = null;

			// TODO: ugly
			switch (flag)
			{
			case VisiblePartFlags.HeadFlag:
				item = headToolStripMenuItem;
				itemButton = toggleHeadToolStripButton;
				break;
			case VisiblePartFlags.HelmetFlag:
				item = helmetToolStripMenuItem;
				itemButton = toggleHelmetToolStripButton;
				break;
			case VisiblePartFlags.ChestFlag:
				item = chestToolStripMenuItem;
				itemButton = toggleChestToolStripButton;
				break;
			case VisiblePartFlags.LeftArmFlag:
				item = leftArmToolStripMenuItem;
				itemButton = toggleLeftArmToolStripButton;
				break;
			case VisiblePartFlags.RightArmFlag:
				item = rightArmToolStripMenuItem;
				itemButton = toggleRightArmToolStripButton;
				break;
			case VisiblePartFlags.LeftLegFlag:
				item = leftLegToolStripMenuItem;
				itemButton = toggleLeftLegToolStripButton;
				break;
			case VisiblePartFlags.RightLegFlag:
				item = rightLegToolStripMenuItem;
				itemButton = toggleRightLegToolStripButton;
				break;
			}

			item.Checked = hasNow;
			itemButton.Checked = hasNow;

			rendererControl.Invalidate();
		}

		void ToggleAlphaCheckerboard()
		{
			GlobalSettings.AlphaCheckerboard = !GlobalSettings.AlphaCheckerboard;
			alphaCheckerboardToolStripMenuItem.Checked = GlobalSettings.AlphaCheckerboard;
			rendererControl.Invalidate();
		}

		void ToggleOverlay()
		{
			GlobalSettings.TextureOverlay = !GlobalSettings.TextureOverlay;
			textureOverlayToolStripMenuItem.Checked = GlobalSettings.TextureOverlay;
			rendererControl.Invalidate();
		}

		void ToggleTransparencyMode()
		{
			switch (GlobalSettings.Transparency)
			{
			case TransparencyMode.Off:
				SetTransparencyMode(TransparencyMode.Helmet);
				break;
			case TransparencyMode.Helmet:
				SetTransparencyMode(TransparencyMode.All);
				break;
			case TransparencyMode.All:
				SetTransparencyMode(TransparencyMode.Off);
				break;
			}
		}

		void ToggleViewMode()
		{
			switch (_currentViewMode)
			{
			case ViewMode.Orthographic:
				SetViewMode(ViewMode.Perspective);
				break;
			case ViewMode.Perspective:
				SetViewMode(ViewMode.Hybrid);
				break;
			case ViewMode.Hybrid:
				SetViewMode(ViewMode.Orthographic);
				break;
			}
		}

		#region Screenshots
		Bitmap CopyScreenToBitmap()
		{
			rendererControl.MakeCurrent();
			_screenshotMode = true;
			rendererControl_Paint(null, null);
			_screenshotMode = false;

			Bitmap b = new Bitmap(rendererControl.Width, rendererControl.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int[] pixels = new int[rendererControl.Width * rendererControl.Height];
			GL.ReadPixels(0, 0, rendererControl.Width, rendererControl.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

			var locked = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			unsafe
			{
				fixed (void *inPixels = pixels)
				{
					void *outPixels = locked.Scan0.ToPointer();

					int *inInt = (int*)inPixels;
					int *outInt = (int*)outPixels;

					for (int y = 0; y < b.Height; ++y)
						for (int x = 0; x < b.Width; ++x)
						{
							var color = Color.FromArgb((*inInt >> 24) & 0xFF, (*inInt >> 0) & 0xFF, (*inInt >> 8) & 0xFF, (*inInt >> 16) & 0xFF);
							*outInt = color.ToArgb();

							inInt++;
							outInt++;
						}
				}
			}

			b.UnlockBits(locked);
			b.RotateFlip(RotateFlipType.RotateNoneFlipY);

			return b;
		}

		void TakeScreenshot()
		{
			Clipboard.SetImage(CopyScreenToBitmap());
		}

		void SaveScreenshot()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "PNG Image|*.png";

				if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					using (var bmp = CopyScreenToBitmap())
						bmp.Save(sfd.FileName);
				}
			}
		}
		#endregion
		#endregion

		void rendererControl_Load(object sender, EventArgs e)
		{
			rendererControl.Invalidate();
			GL.ClearColor(GlobalSettings.BackgroundColor);

			GL.Enable(EnableCap.Texture2D);
			GL.ShadeModel(ShadingModel.Smooth);                        // Enable Smooth Shading
			GL.ClearDepth(1.0f);                         // Depth Buffer Setup
			GL.Enable(EnableCap.DepthTest);                        // Enables Depth Testing
			GL.DepthFunc(DepthFunction.Lequal);                         // The Type Of Depth Testing To Do
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);          // Really Nice Perspective Calculations
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Front);

			_toolboxUpNormal = ImageUtilities.LoadImage(Properties.Resources.buttong);
			_toolboxUpHover = ImageUtilities.LoadImage(Properties.Resources.buttong_2);
			_toolboxDownNormal = ImageUtilities.LoadImage(Properties.Resources.buttong_down);
			_toolboxDownHover = ImageUtilities.LoadImage(Properties.Resources.buttong_down2);

			_grassTop = ImageUtilities.LoadImage("grass.png");
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			foreach (var file in Directory.GetFiles("Overlays"))
			{
				try
				{
					var image = ImageUtilities.LoadImage(file);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

					_backgrounds.Add(new BackgroundImage(Path.GetFileNameWithoutExtension(file), image));
				}
				catch
				{
					MessageBox.Show(this, string.Format(GetLanguageString("B_MSG_OVERLAYERROR"), file));
				}
			}

			int index = 0;
			foreach (var b in _backgrounds)
			{
				ToolStripMenuItem item = new ToolStripMenuItem(b.Name);
				b.Item = item;

				if (b.Name == GlobalSettings.LastBackground)
				{
					item.Checked = true;
					_selectedBackground = index;
				}

				item.Click += item_Clicked;
				item.Tag = index++;

				backgroundsToolStripMenuItem.DropDownItems.Add(item);
			}

			_previewPaint = GL.GenTexture();
			GlobalDirtiness.CurrentSkin = GL.GenTexture();
			_alphaTex = GL.GenTexture();

			unsafe
			{
				byte[] arra = new byte[64 * 32];
				RenderState.BindTexture(_previewPaint);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 64, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, arra);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

				RenderState.BindTexture(GlobalDirtiness.CurrentSkin);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 64, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, arra);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

				arra = new byte[4 * 4 * 4];
				fixed (byte* texData = arra)
				{
					byte *d = texData;

					for (int y = 0; y < 4; ++y)
						for (int x = 0; x < 4; ++x)
						{
							bool dark = ((x + (y & 1)) & 1) == 1;

							if (dark)
								*((int*)d) = (80 << 0) | (80 << 8) | (80 << 16) | (255 << 24);
							else
								*((int*)d) = (127 << 0) | (127 << 8) | (127 << 16) | (255 << 24);
							d += 4;
						}
				}

				RenderState.BindTexture(_alphaTex);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 4, 4, 0, PixelFormat.Rgba, PixelType.UnsignedByte, arra);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
			}

			if (GL.GetString(StringName.Extensions).Contains("GL_EXT_vertex_array"))
				_renderer = new ClientArrayRenderer();
			else
				_renderer = new ImmediateRenderer();
		}

		void item_Clicked(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			GlobalSettings.LastBackground = item.Text;
			_backgrounds[_selectedBackground].Item.Checked = false;
			_selectedBackground = (int)item.Tag;
			item.Checked = true;
		}

		static bool _ddh = false;
		void animTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (!_ddh)
			{
				new DragDropHelper();
				_ddh = true;
			}

			_animationTime += 0.24f;
			rendererControl.Invalidate();
		}

		void rendererControl_MouseWheel(object sender, MouseEventArgs e)
		{
			CheckMouse(e.Y);

			if (_currentViewMode == ViewMode.Perspective || (_currentViewMode == ViewMode.Hybrid && _mouseIn3D))
				_3dZoom += e.Delta / 50;
			else
				_2dZoom += e.Delta / 50;
			if (_2dZoom < 0.25f) _2dZoom = 0.25f;

			rendererControl.Invalidate();
		}

		void DrawGLToolbar()
		{
			// 2D
			Setup2D(new Rectangle(0, 0, rendererControl.Width, rendererControl.Height));
			RenderState.BindTexture(0);
			GL.Enable(EnableCap.Blend);

			float halfWidth = rendererControl.Width / 2.0f;
			float halfImgWidth = 56.0f / 2.0f;

			var rect = new RectangleF(halfWidth - halfImgWidth, 0, halfImgWidth * 2, 22);

			int img = (splitContainer4.SplitterDistance == 0) ? _toolboxDownNormal : _toolboxUpNormal;

			if (rect.Contains(_mousePoint))
			{
				GL.Color4((byte)255, (byte)255, (byte)255, (byte)255);
				RenderState.BindTexture(img);
			}
			else
			{
				GL.Color4((byte)255, (byte)255, (byte)255, (byte)64);
				RenderState.BindTexture(img);
			}

			const float widSep = 56.0f / 64.0f;
			const float heiSep = 22.0f / 32.0f;

			GL.Begin(BeginMode.Quads);
			GL.TexCoord2(0, 0); GL.Vertex2(halfWidth - halfImgWidth, -1);
			GL.TexCoord2(widSep, 0); GL.Vertex2(halfWidth + halfImgWidth, -1);
			GL.TexCoord2(widSep, heiSep); GL.Vertex2(halfWidth + halfImgWidth, 21);
			GL.TexCoord2(0, heiSep); GL.Vertex2(halfWidth - halfImgWidth, 21);
			GL.End();
		}

		static bool _screenshotMode = false;

		void rendererControl_Paint(object sender, PaintEventArgs e)
		{
			rendererControl.MakeCurrent();
			SetPreview();

			GL.ClearColor(GlobalSettings.BackgroundColor);
			GL.Color4((byte)255, (byte)255, (byte)255, (byte)255);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			var skin = (Skin)_lastSkin;

			GL.PushMatrix();

			if (_currentViewMode == ViewMode.Perspective)
			{
				Setup3D(new Rectangle(0, 0, rendererControl.Width, rendererControl.Height));
				DrawPlayer(_previewPaint, skin, false);
			}
			else if (_currentViewMode == ViewMode.Orthographic)
			{
				Setup2D(new Rectangle(0, 0, rendererControl.Width, rendererControl.Height));
				DrawPlayer2D(_previewPaint, skin, false);
			}
			else
			{
				int halfHeight = (int)Math.Ceiling(rendererControl.Height / 2.0f);

				Setup3D(new Rectangle(0, 0, rendererControl.Width, halfHeight));
				DrawPlayer(_previewPaint, skin, false);

				Setup2D(new Rectangle(0, halfHeight, rendererControl.Width, halfHeight));
				DrawPlayer2D(_previewPaint, skin, false);
			}

			GL.PopMatrix();

			if (!_screenshotMode)
				DrawGLToolbar();

			if (!_screenshotMode)
				rendererControl.SwapBuffers();
		}

		Rectangle _currentViewport;

		void Setup3D(Rectangle viewport)
		{
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();

			GL.Viewport(viewport);
			var mat = OpenTK.Matrix4d.Perspective(45, (double)viewport.Width / (double)viewport.Height, 0.01, 100000);
			GL.MultMatrix(ref mat);
			GL.Scale(1, -1, 1);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			_currentViewport = viewport;

			Vector3 vec = new Vector3();
			int count = 0;
			foreach (var mesh in CurrentModel.Meshes)
			{
				if ((GlobalSettings.ViewFlags & mesh.Part) != 0)
				{
					vec += mesh.Center;
					count++;
				}
			}

			if (count != 0)
				vec /= count;

			// FIXME: calculate these only on change
			Matrix4 mt =
				Matrix4.CreateTranslation(-vec.X, -vec.Y, 0) *
				Matrix4.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.DegreesToRadians(_3dRotationY)) *
				Matrix4.CreateFromAxisAngle(new Vector3(-1, 0, 0), MathHelper.DegreesToRadians(_3dRotationX)) *
				Matrix4.CreateTranslation(0, 0, _3dZoom);

			GL.LoadMatrix(ref mt);

			mt =
				Matrix4.CreateTranslation(-vec.X, -vec.Y, 0) *
				Matrix4.CreateTranslation(0, 0, _3dZoom) *
				Matrix4.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.DegreesToRadians(_3dRotationY)) *
				Matrix4.CreateFromAxisAngle(new Vector3(-1, 0, 0), MathHelper.DegreesToRadians(_3dRotationX));

			CameraPosition = Vector3.TransformPosition(Vector3.Zero, mt);
		}

		void Setup2D(Rectangle viewport)
		{
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();

			GL.Viewport(viewport);
			GL.Ortho(0, viewport.Width, viewport.Height, 0, -1, 1);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			_currentViewport = viewport;
		}

		void rendererControl_Resize(object sender, EventArgs e)
		{
			rendererControl.MakeCurrent();

			rendererControl.Invalidate();
		}

		System.Timers.Timer _animTimer = new System.Timers.Timer(25);
		bool _opening = false;

		void _animTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (_opening)
			{
				splitContainer4.SplitterDistance += splitContainer4.SplitterIncrement;

				if (splitContainer4.SplitterDistance >= 74)
				{
					splitContainer4.SplitterDistance = 74;
					_animTimer.Stop();
				}
			}
			else
			{
				if ((splitContainer4.SplitterDistance - splitContainer4.SplitterIncrement) <= splitContainer4.Panel1MinSize)
				{
					splitContainer4.SplitterDistance = splitContainer4.Panel1MinSize;
					_animTimer.Stop();
					return;
				}

				splitContainer4.SplitterDistance -= splitContainer4.SplitterIncrement;
			}
		}

		public float ToolScale
		{
			get
			{
				const float baseSize = 200.0f;

				return baseSize / rendererControl.Size.Width;
			}
		}

		public void RotateView(Point delta, float factor)
		{
			if (_currentViewMode == ViewMode.Perspective || (_currentViewMode == ViewMode.Hybrid && _mouseIn3D))
			{
				_3dRotationY += (float)(delta.X * ToolScale) * factor;
				_3dRotationX += (float)(delta.Y * ToolScale) * factor;
			}
			else
			{
				_2dCamOffsetX += delta.X / _2dZoom;
				_2dCamOffsetY += delta.Y / _2dZoom;
			}
		}

		public void ScaleView(Point delta, float factor)
		{
			if (_currentViewMode == ViewMode.Perspective || (_currentViewMode == ViewMode.Hybrid && _mouseIn3D))
			{
				_3dZoom += (float)(-delta.Y * ToolScale) * factor;
			}
			else
			{
				_2dZoom += -delta.Y / 25.0f;

				if (_2dZoom < 0.25f)
					_2dZoom = 0.25f;
			}
		}

		bool _mouseIn3D = false;
		void CheckMouse(int y)
		{
			if (y > (rendererControl.Height / 2))
				_mouseIn3D = true;
			else
				_mouseIn3D = false;
		}

		void rendererControl_MouseDown(object sender, MouseEventArgs e)
		{
			Skin skin = _lastSkin;

			if (skin == null)
				return;

			CheckMouse(e.Y);

			float halfWidth = rendererControl.Width / 2.0f;
			float halfImgWidth = 56.0f / 2.0f;

			var rect = new RectangleF(halfWidth - halfImgWidth, 0, halfImgWidth * 2, 22);

			_mousePoint = e.Location;

			if (rect.Contains(e.Location))
			{
				if (splitContainer4.SplitterDistance == 0)
					_opening = true;
				else
					_opening = false;

				_animTimer.Start();
				return;
			}

			_mouseIsDown = true;
			_isValidPick = GetPick(e.X, e.Y, ref _pickPosition);

			if (e.Button == MouseButtons.Left)
			{
				if (_isValidPick)
					_selectedTool.Tool.BeginClick(_lastSkin, _pickPosition, e);
				else
					_selectedTool.Tool.BeginClick(_lastSkin, new Point(-1, -1), e);
				UseToolOnViewport(e.X, e.Y);
			}
			else
				_tools[(int)Tools.Camera].Tool.BeginClick(_lastSkin, Point.Empty, e);
		}

		void rendererControl_MouseMove(object sender, MouseEventArgs e)
		{
			Skin skin = _lastSkin;

			if (skin == null)
				return;

			_isValidPick = GetPick(e.X, e.Y, ref _pickPosition);

			if (_mouseIsDown)
			{
				if (e.Button == MouseButtons.Left)
				{
					_selectedTool.Tool.MouseMove(_lastSkin, e);
					UseToolOnViewport(e.X, e.Y);
				}
				else
					_tools[(int)Tools.Camera].Tool.MouseMove(_lastSkin, e);

				rendererControl.Invalidate();
			}

			_mousePoint = e.Location;
		}

		void rendererControl_MouseUp(object sender, MouseEventArgs e)
		{
			Skin skin = _lastSkin;

			if (skin == null)
				return;

			if (_mouseIsDown)
			{
				ColorGrabber currentSkin = new ColorGrabber();

				if (e.Button == MouseButtons.Left)
				{
					currentSkin = new ColorGrabber(GlobalDirtiness.CurrentSkin, skin.Width, skin.Height);
					currentSkin.Load();

					if (_selectedTool.Tool.EndClick(ref currentSkin, skin, e))
					{
						SetCanSave(true);
						skin.Dirty = true;
						treeView1.Invalidate();
						currentSkin.Save();
					}
				}
				else
					_tools[(int)Tools.Camera].Tool.EndClick(ref currentSkin, _lastSkin, e);
			}

			_mouseIsDown = false;
		}

		public void CheckUndo()
		{
			undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = _currentUndoBuffer.CanUndo;
			redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = _currentUndoBuffer.CanRedo;
		}

		void rendererControl_MouseLeave(object sender, EventArgs e)
		{
			_mousePoint = new Point(-1, -1);
		}

		private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (_skipListbox || treeView1.SelectedNode == _lastSkin ||
				!(e.Node is Skin))
				return;

			rendererControl.MakeCurrent();

			if (_lastSkin != null && treeView1.SelectedNode != _lastSkin)
			{
				// Copy over the current changes to the tex stored in the skin.
				// This allows us to pick up where we left off later, without undoing any work.
				_lastSkin.CommitChanges(GlobalDirtiness.CurrentSkin, false);
			}

			//if (_lastSkin != null)
			//	_lastSkin.Undo.Clear();

			Skin skin = (Skin)treeView1.SelectedNode;
			SetCanSave(skin.Dirty);

			if (skin == null)
			{
				_currentUndoBuffer = null;
				RenderState.BindTexture(0);

				ColorGrabber currentSkin = new ColorGrabber(GlobalDirtiness.CurrentSkin, 64, 32);
				currentSkin.Save();

				undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = false;
				redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = false;
			}
			else
			{
				ColorGrabber glImage = new ColorGrabber(skin.GLImage, skin.Width, skin.Height);
				glImage.Load();

				glImage.Texture = GlobalDirtiness.CurrentSkin;
				glImage.Save();
				glImage.Texture = _previewPaint;
				glImage.Save();

				_currentUndoBuffer = skin.Undo;
				CheckUndo();
			}

			_lastSkin = (Skin)treeView1.SelectedNode;

			SetModel(skin.Model);
			rendererControl.Invalidate();
		}

		void uploadButton_Click(object sender, EventArgs e)
		{
			PerformUpload();
		}

		void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		void animateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleAnimation();
		}

		void followCursorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFollowCursor();
		}

		void grassToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleGrass();
		}

		void addNewSkinToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformImportSkin();
		}

		void deleteSelectedSkinToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformDeleteSkin();
		}

		void cloneSkinToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformCloneSkin();
		}

		private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			//PerformNameChange();
		}

		TreeNode _rightClickedNode = null;
		private void treeView1_MouseUp(object sender, MouseEventArgs e)
		{
			treeView1_MouseDown(sender, e);
			if (e.Button == MouseButtons.Right)
				contextMenuStrip1.Show(Cursor.Position);
		}

		private void treeView1_MouseDown(object sender, MouseEventArgs e)
		{
			_rightClickedNode = treeView1.GetSelectedNodeAt(e.Location);
			changeNameToolStripMenuItem.Enabled = deleteToolStripMenuItem.Enabled = cloneToolStripMenuItem.Enabled = cloneToolStripButton.Enabled = true;
			mDECRESToolStripMenuItem.Enabled = mINCRESToolStripMenuItem.Enabled = true;

			if (treeView1.SelectedNode == null)
			{
				mDECRESToolStripMenuItem.Enabled =
					mINCRESToolStripMenuItem.Enabled =
					changeNameToolStripMenuItem.Enabled =
					deleteToolStripMenuItem.Enabled =
					cloneToolStripMenuItem.Enabled =
					cloneToolStripButton.Enabled = false;
			}
			else if (!(treeView1.SelectedNode is Skin))
			{
				mDECRESToolStripMenuItem.Enabled =
					mINCRESToolStripMenuItem.Enabled =
					cloneToolStripMenuItem.Enabled =
					cloneToolStripButton.Enabled = false;
			}
			else if (treeView1.SelectedNode is Skin)
			{
				var skin = treeView1.SelectedNode as Skin;

				if (skin.Width == 8 || skin.Height == 4)
					mDECRESToolStripMenuItem.Enabled = false;
				//else if (skin.Width == 256 || skin.Height == 128)
				//	mINCRESToolStripMenuItem.Enabled = false;
			}
		}

		void undoToolStripButton_Click(object sender, EventArgs e)
		{
			PerformUndo();
		}

		void redoToolStripButton_Click(object sender, EventArgs e)
		{
			PerformRedo();
		}

		void redNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(SelectedColor.A, (byte)redNumericUpDown.Value, SelectedColor.G, SelectedColor.B));
		}

		void greenNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(SelectedColor.A, SelectedColor.R, (byte)greenNumericUpDown.Value, SelectedColor.B));
		}

		void blueNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(SelectedColor.A, SelectedColor.R, SelectedColor.G, (byte)blueNumericUpDown.Value));
		}

		void alphaNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb((byte)alphaNumericUpDown.Value, SelectedColor.R, SelectedColor.G, SelectedColor.B));
		}

		const float oneDivTwoFourty = 1.0f / 240.0f;

		void colorSquare_HueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(colorSquare.CurrentHue, (float)colorSquare.CurrentSat * oneDivTwoFourty, (float)saturationSlider.CurrentLum * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void colorSquare_SatChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(colorSquare.CurrentHue, (float)colorSquare.CurrentSat * oneDivTwoFourty, (float)saturationSlider.CurrentLum * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void saturationSlider_LumChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(colorSquare.CurrentHue, (float)colorSquare.CurrentSat * oneDivTwoFourty, (float)saturationSlider.CurrentLum * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void hueColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(e.NewValue, (float)saturationColorSlider.Value * oneDivTwoFourty, (float)lightnessColorSlider.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void saturationColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(hueColorSlider.Value, (float)e.NewValue * oneDivTwoFourty, (float)lightnessColorSlider.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void lightnessColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(hueColorSlider.Value, (float)saturationColorSlider.Value * oneDivTwoFourty, (float)e.NewValue * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void hueNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL((double)hueNumericUpDown.Value, (float)saturationNumericUpDown.Value * oneDivTwoFourty, (float)luminanceNumericUpDown.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void saturationNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL((double)hueNumericUpDown.Value, (float)saturationNumericUpDown.Value * oneDivTwoFourty, (float)luminanceNumericUpDown.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void luminanceNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL((double)hueNumericUpDown.Value, (float)saturationNumericUpDown.Value * oneDivTwoFourty, (float)luminanceNumericUpDown.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void perspectiveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Perspective);
		}

		void textureToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Orthographic);
		}

		void perspectiveToolStripButton_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Perspective);
		}

		void orthographicToolStripButton_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Orthographic);
		}

		private void hybridToolStripButton_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Hybrid);
		}

		private void hybridViewToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Hybrid);
		}

		void offToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTransparencyMode(TransparencyMode.Off);
		}

		void helmetOnlyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTransparencyMode(TransparencyMode.Helmet);
		}

		void allToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTransparencyMode(TransparencyMode.All);
		}

		void headToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.HeadFlag);
		}

		void helmetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.HelmetFlag);
		}

		void chestToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.ChestFlag);
		}

		void leftArmToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.LeftArmFlag);
		}

		void rightArmToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.RightArmFlag);
		}

		void leftLegToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.LeftLegFlag);
		}

		void rightLegToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.RightLegFlag);
		}

		void alphaCheckerboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleAlphaCheckerboard();
		}

		void textureOverlayToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleOverlay();
		}

		void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_updater.Checking)
				return;

			_updater.PrintOnEqual = true;
			_updater.CheckForUpdate();
		}

		void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformUndo();
		}

		void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformRedo();
		}

		void redColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(SelectedColor.A, e.NewValue, SelectedColor.G, SelectedColor.B));
		}

		void greenColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(SelectedColor.A, SelectedColor.R, e.NewValue, SelectedColor.B));
		}

		void blueColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(SelectedColor.A, SelectedColor.R, SelectedColor.G, e.NewValue));
		}

		void swatchContainer_SwatchChanged(object sender, SwatchChangedEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (_secondaryIsFront)
					SetColor(colorPreview2, ref _secondaryColor, e.Swatch);
				else
					SetColor(colorPreview1, ref _primaryColor, e.Swatch);
			}
			else
			{
				if (!_secondaryIsFront)
					SetColor(colorPreview2, ref _secondaryColor, e.Swatch);
				else
					SetColor(colorPreview1, ref _primaryColor, e.Swatch);
			}
		}

		void alphaColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(e.NewValue, SelectedColor.R, SelectedColor.G, SelectedColor.B));
		}

		void keyboardShortcutsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_shortcutEditor.ShowDialog();
		}

		void backgroundColorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (MultiPainter.ColorPicker picker = new MultiPainter.ColorPicker())
			{
				picker.CurrentColor = GlobalSettings.BackgroundColor;

				if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					GlobalSettings.BackgroundColor = picker.CurrentColor;

					rendererControl.Invalidate();
				}
			}
		}

		void screenshotToolStripButton_Click(object sender, EventArgs e)
		{
			if ((ModifierKeys & Keys.Shift) != 0)
				SaveScreenshot();
			else
				TakeScreenshot();
		}

		void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformSaveAs();
		}

		void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformSave();
		}

		void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformSaveAll();
		}

		void saveToolStripButton_Click(object sender, EventArgs e)
		{
			PerformSave();
		}

		void saveAlltoolStripButton_Click(object sender, EventArgs e)
		{
			PerformSaveAll();
		}

		void changeNameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformNameChange();
		}

		void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformDeleteSkin();
		}

		void cloneToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformCloneSkin();
		}

		void colorTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (colorTabControl.SelectedIndex == 1 || colorTabControl.SelectedIndex == 2)
			{
				var panel = (Panel)colorTabControl.SelectedTab.Controls[0];

				panel.Controls.Add(colorSquare);
				panel.Controls.Add(saturationSlider);
				panel.Controls.Add(colorPreview1);
				panel.Controls.Add(colorPreview2);
				panel.Controls.Add(label5);
				panel.Controls.Add(alphaColorSlider);
				panel.Controls.Add(alphaNumericUpDown);

				if (_secondaryIsFront)
				{
					colorPreview2.BringToFront();
					colorPreview1.SendToBack();
				}
				else
				{
					colorPreview2.SendToBack();
					colorPreview1.BringToFront();
				}
			}
		}

		void automaticallyCheckForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalSettings.AutoUpdate = automaticallyCheckForUpdatesToolStripMenuItem.Checked = !automaticallyCheckForUpdatesToolStripMenuItem.Checked;
		}

		private void toggleHeadToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.HeadFlag);
		}

		private void toggleHelmetToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.HelmetFlag);
		}

		private void toggleChestToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.ChestFlag);
		}

		private void toggleLeftArmToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.LeftArmFlag);
		}

		private void toggleRightArmToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.RightArmFlag);
		}

		private void toggleLeftLegToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.LeftLegFlag);
		}

		private void toggleRightLegToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.RightLegFlag);
		}

		private void labelEditTextBox_Leave(object sender, EventArgs e)
		{
			DoneEditingNode(labelEditTextBox.Text, _currentlyEditing);
		}

		bool _editingHex = false;
		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			try
			{
				if (textBox1.Text.Contains('#'))
					textBox1.Text = textBox1.Text.Replace("#", "");

				if (textBox1.Text.Length > 8)
					textBox1.Text = textBox1.Text.Remove(8);

				string realHex = textBox1.Text;

				while (realHex.Length != 8)
					realHex += 'F';

				byte r = byte.Parse(realHex.Substring(0, 2), NumberStyles.HexNumber);
				byte g = byte.Parse(realHex.Substring(2, 2), NumberStyles.HexNumber);
				byte b = byte.Parse(realHex.Substring(4, 2), NumberStyles.HexNumber);
				byte a = byte.Parse(realHex.Substring(6, 2), NumberStyles.HexNumber);

				_editingHex = true;
				SetColor(Color.FromArgb(a, r, g, b));
				_editingHex = false;
			}
			catch
			{
			}
		}

		private void labelEditTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
			{
				treeView1.Focus();
				e.Handled = true;
			}
		}

		private void labelEditTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (e.KeyChar == '\r' || e.KeyChar == '\n')
				e.Handled = true;
		}

		private void labelEditTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
				e.Handled = true;
		}

		private void importHereToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformImportSkin();
		}

		private void toolStripMenuItem1_Click(object sender, EventArgs e)
		{
			PerformNewFolder();
		}

		private void ghostHiddenPartsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleGhosting();
		}

		static ToolStripMenuItem[] _antialiasOpts;
		void SetSampleMenuItem(int samples)
		{
			if (_antialiasOpts == null)
				_antialiasOpts = new ToolStripMenuItem[] { xToolStripMenuItem4, xToolStripMenuItem, xToolStripMenuItem1, xToolStripMenuItem2, xToolStripMenuItem3 };

			int index = 0;

			switch (samples)
			{
			case 0:
			default:
				index = 0;
				break;
			case 1:
				index = 1;
				break;
			case 2:
				index = 2;
				break;
			case 4:
				index = 3;
				break;
			case 8:
				index = 4;
				break;
			}

			foreach (var item in _antialiasOpts)
				item.Checked = false;

			GlobalSettings.Multisamples = samples;
			_antialiasOpts[index].Checked = true;
		}

		private void xToolStripMenuItem4_Click(object sender, EventArgs e)
		{
			SetSampleMenuItem(0);
			MessageBox.Show(this, GetLanguageString("B_MSG_ANTIALIAS"));
		}

		private void xToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetSampleMenuItem(1);
			MessageBox.Show(this, GetLanguageString("B_MSG_ANTIALIAS"));
		}

		private void xToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			SetSampleMenuItem(2);
			MessageBox.Show(this, GetLanguageString("B_MSG_ANTIALIAS"));
		}

		private void xToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			SetSampleMenuItem(4);
			MessageBox.Show(this, GetLanguageString("B_MSG_ANTIALIAS"));
		}

		private void xToolStripMenuItem3_Click(object sender, EventArgs e)
		{
			SetSampleMenuItem(8);
			MessageBox.Show(this, GetLanguageString("B_MSG_ANTIALIAS"));
		}

		private void SetLanguage(string filename)
		{
		}

		private void ReloadLanguage()
		{
		}

		static Language.Language _currentLanguage;
		public static Language.Language CurrentLanguage
		{
			get { return _currentLanguage; }
			set
			{
				if (_currentLanguage != null)
					_currentLanguage.Item.Checked = false;

				_currentLanguage = value;
				GlobalSettings.LanguageFile = _currentLanguage.Culture.Name;
				MainForm.languageProvider1.LanguageChanged(value);
				MainForm.DarkenLightenOptions.languageProvider1.LanguageChanged(value);
				MainForm.PencilOptions.languageProvider1.LanguageChanged(value);
				MainForm.DodgeBurnOptions.languageProvider1.LanguageChanged(value);
				MainForm.FloodFillOptions.languageProvider1.LanguageChanged(value);
				MainForm.swatchContainer.languageProvider1.LanguageChanged(value);
				MainForm.login.languageProvider1.LanguageChanged(value);
				MainForm.NoiseOptions.languageProvider1.LanguageChanged(value);
				MainForm.EraserOptions.languageProvider1.LanguageChanged(value);
				MainForm._importFromSite.languageProvider1.LanguageChanged(value);

				if (MainForm._selectedTool != null)
					MainForm.toolStripStatusLabel1.Text = MainForm._selectedTool.Tool.GetStatusLabelText();

				_currentLanguage.Item.Checked = true;
			}
		}

		public static string GetLanguageString(string id)
		{
			if (!_currentLanguage.StringTable.ContainsKey(id))
				return id;
			return _currentLanguage.StringTable[id];
		}

		private void MCSkin3D_Load(object sender, EventArgs e)
		{
		}

		void languageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CurrentLanguage = (Language.Language)((ToolStripMenuItem)sender).Tag;
		}

		bool _secondaryIsFront = false;

		private void colorPreview1_Click(object sender, EventArgs e)
		{
			_secondaryIsFront = false;
			colorPreview1.BringToFront();

			SetColor(_primaryColor);
		}

		private void colorPreview2_Click(object sender, EventArgs e)
		{
			_secondaryIsFront = true;
			colorPreview2.BringToFront();

			SetColor(_secondaryColor);
		}

		private void toolStripMenuItem3_Click(object sender, EventArgs e)
		{
			PerformUpload();
		}

		private void importToolStripButton_Click(object sender, EventArgs e)
		{
			PerformImportSkin();
		}

		private void newFolderToolStripButton_Click(object sender, EventArgs e)
		{
			PerformNewFolder();
		}

		private void renameToolStripButton_Click(object sender, EventArgs e)
		{
			PerformNameChange();
		}

		private void deleteToolStripButton_Click(object sender, EventArgs e)
		{
			PerformDeleteSkin();
		}

		private void cloneToolStripButton_Click(object sender, EventArgs e)
		{
			PerformCloneSkin();
		}

		private void uploadToolStripButton_Click(object sender, EventArgs e)
		{
			PerformUpload();
		}

		private void toolStripButton2_Click(object sender, EventArgs e)
		{
			PerformTreeViewZoomOut();
		}

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			PerformTreeViewZoomIn();
		}

		private void colorTabControl_Resize(object sender, EventArgs e)
		{
		}

		private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
		{

		}

		private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
		{
			treeView1.ScrollPosition = new Point(hScrollBar1.Value, treeView1.ScrollPosition.Y);
		}

		bool ShowDontAskAgain()
		{
			bool againValue = GlobalSettings.ResChangeDontShowAgain;
			bool ret = DontAskAgain.Show(CurrentLanguage, "M_IRREVERSIBLE", ref againValue);
			GlobalSettings.ResChangeDontShowAgain = againValue;

			return ret;
		}

		void PerformDecreaseResolution()
		{
			if (_lastSkin == null)
				return;
			if (_lastSkin.Width == 8 || _lastSkin.Height == 4)
				return;
			if (!ShowDontAskAgain())
				return;

			_lastSkin.Resize(_lastSkin.Width / 2, _lastSkin.Height / 2);

			ColorGrabber grabber = new ColorGrabber(_lastSkin.GLImage, _lastSkin.Width, _lastSkin.Height);
			grabber.Load();
			grabber.Texture = GlobalDirtiness.CurrentSkin;
			grabber.Save();
			grabber.Texture = _previewPaint;
			grabber.Save();
		}

		void PerformIncreaseResolution()
		{
			if (_lastSkin == null)
				return;
			//if (_lastSkin.Width == 256 || _lastSkin.Height == 128)
			//	return;
			if (!ShowDontAskAgain())
				return;

			_lastSkin.Resize(_lastSkin.Width * 2, _lastSkin.Height * 2);

			ColorGrabber grabber = new ColorGrabber(_lastSkin.GLImage, _lastSkin.Width, _lastSkin.Height);
			grabber.Load();
			grabber.Texture = GlobalDirtiness.CurrentSkin;
			grabber.Save();
			grabber.Texture = _previewPaint;
			grabber.Save();
		}

		ImportSite _importFromSite = new ImportSite();
		public void PerformImportFromSite()
		{
			string accountName = _importFromSite.Show();

			if (string.IsNullOrEmpty(accountName))
				return;

			var url = "http://s3.amazonaws.com/MinecraftSkins/" + accountName + ".png";

			string folderLocation;
			TreeNodeCollection collection;

			if (_rightClickedNode == null)
				_rightClickedNode = treeView1.SelectedNode;

			if (_rightClickedNode != null)
			{
				if (!(_rightClickedNode is Skin))
				{
					folderLocation = "Skins\\" + _rightClickedNode.FullPath + '\\';
					collection = _rightClickedNode.Nodes;
				}
				else if (_rightClickedNode.Parent != null)
				{
					folderLocation = "Skins\\" + _rightClickedNode.Parent.FullPath + '\\';
					collection = _rightClickedNode.Parent.Nodes;
				}
				else
				{
					folderLocation = "Skins\\";
					collection = treeView1.Nodes;
				}
			}
			else
			{
				folderLocation = "Skins\\";
				collection = treeView1.Nodes;
			}

			string newSkinName = accountName;

			while (File.Exists(folderLocation + newSkinName + ".png"))
				newSkinName += " - New";

			try
			{
				byte[] pngData = Paril.Net.WebHelpers.DownloadFile(url);

				using (var file = File.Create(folderLocation + newSkinName + ".png"))
					file.Write(pngData, 0, pngData.Length);

				var skin = new Skin(folderLocation + newSkinName + ".png");
				collection.Add(skin);
				skin.SetImages();

				treeView1.Invalidate();
			}
			catch
			{
				MessageBox.Show(this, GetLanguageString("M_SKINERROR"));
				return;
			}
		}

		private void mDECRESToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformDecreaseResolution();
		}

		private void mINCRESToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformIncreaseResolution();
		}

		private void toolStripButton4_Click(object sender, EventArgs e)
		{
			PerformDecreaseResolution();
		}

		private void toolStripButton3_Click(object sender, EventArgs e)
		{
			PerformIncreaseResolution();
		}

		private void toolStripButton5_Click(object sender, EventArgs e)
		{
			PerformNewSkin();
		}

		private void toolStripButton6_Click(object sender, EventArgs e)
		{
			PerformImportFromSite();
		}

		private void mFETCHNAMEToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformImportFromSite();
		}

		private void toolStripMenuItem4_Click(object sender, EventArgs e)
		{
			PerformNewSkin();
		}

		private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{

		}

		private void mSKINDIRSToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (DirectoryList dl = new DirectoryList())
			{
				dl.StartPosition = FormStartPosition.CenterParent;
				foreach (var dir in GlobalSettings.SkinDirectories)
					dl.Directories.Add(dir);

				if (dl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
					GlobalSettings.SkinDirectories = dl.Directories.ToArray();
			}
		}

		private void modeToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			Perform10Mode();
		}

		private void mINVERTBOTTOMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (treeView1.SelectedNode == null ||
				_lastSkin == null)
				return;

			ColorGrabber grabber = new ColorGrabber(GlobalDirtiness.CurrentSkin, _lastSkin.Width, _lastSkin.Height);
			grabber.Load();

			List<Rectangle> toInvert = new List<Rectangle>();

			foreach (var meshes in CurrentModel.Meshes)
			{
				foreach (var face in meshes.Faces)
				{
					if (face.Downface)
					{
						var rect = face.TexCoordsToInteger(_lastSkin.Width, _lastSkin.Height);

						if (!toInvert.Contains(rect))
							toInvert.Add(rect);
					}
				}
			}

			PixelsChangedUndoable undoable = new PixelsChangedUndoable();

			foreach (var rect in toInvert)
			{
				for (int x = rect.X; x < rect.X + rect.Width; ++x)
				{
					for (int y = rect.Y, y2 = rect.Y + rect.Height - 1; y2 > y; ++y, --y2)
					{
						var topPixel = grabber[x, y];
						var bottomPixel = grabber[x, y2];

						undoable.Points.Add(new Point(x, y), Tuple.MakeTuple(Color.FromArgb(topPixel.Alpha, topPixel.Red, topPixel.Green, topPixel.Blue), new ColorAlpha(Color.FromArgb(bottomPixel.Alpha, bottomPixel.Red, bottomPixel.Green, bottomPixel.Blue), -1)));
						undoable.Points.Add(new Point(x, y2), Tuple.MakeTuple(Color.FromArgb(bottomPixel.Alpha, bottomPixel.Red, bottomPixel.Green, bottomPixel.Blue), new ColorAlpha(Color.FromArgb(topPixel.Alpha, topPixel.Red, topPixel.Green, topPixel.Blue), -1)));

						grabber[x, y] = bottomPixel;
						grabber[x, y2] = topPixel;
					}
				}
			}

			_lastSkin.Undo.AddBuffer(undoable);
			CheckUndo();
			SetCanSave(_lastSkin.Dirty = true);

			grabber.Save();
		}

		public void SetModel(Model Model)
		{
			if (_lastSkin == null)
				return;

			if (_lastSkin.Model != Model)
			{
				_lastSkin.Model = Model;

				_lastSkin.Dirty = true;
				SetCanSave(true);
				CheckUndo();
			}

			foreach (ModelToolStripMenuItem x in toolStripDropDownButton1.DropDownItems)
				x.Checked = (x.Model == _lastSkin.Model);

			toolStripDropDownButton1.Text = _lastSkin.Model.Name;
		}

		public static Vector3 CameraPosition
		{
			get;
			private set;
		}
	}
}