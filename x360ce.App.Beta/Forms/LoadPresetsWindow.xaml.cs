﻿using JocysCom.ClassLibrary.Controls;
using System.Windows;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Interaction logic for LoadPresetsWindow.xaml
	/// </summary>
	public partial class LoadPresetsWindow : Window
	{
		public LoadPresetsWindow()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			ControlsHelper.CheckTopMost(this);
			WinControl.MainBody.Content = WinControl.MainContent;
			WinControl.InfoPanel.SetTitle("X360CE - Load Preset");
			WinControl.InfoPanel.SetHead("Load Preset");
			WinControl.InfoPanel.SetBodyInfo("Select Preset and press [Load Selected] button.");
			WinControl.InfoPanel.SetImage(App.GetResource(Icons_Default.Icon_gamepad));
			WinControl.SetButton1("Load Selected", Icons_Default.Icon_ok);
			WinControl.SetButton2("Cancel", Icons_Default.Icon_close);
			WinControl.SetButton3();
			MainControl._ParentControl = WinControl;
		}

		public Controls.PresetsControl MainControl
		=> (Controls.PresetsControl)WinControl.MainBody.Content;

	}
}
