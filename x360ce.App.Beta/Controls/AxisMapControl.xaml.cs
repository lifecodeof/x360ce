﻿using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for AxisMapControl.xaml
	/// </summary>
	public partial class AxisMapControl : UserControl
	{
		public AxisMapControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			InitPaintObjects();
			// Initialize in constructor and not on "Load" event or it will reset AntiDeadZone value
			// inside DeadZoneControlsLink(...).
			updateTimer = new System.Timers.Timer();
			updateTimer.AutoReset = false;
			updateTimer.Interval = 500;
			updateTimer.Elapsed += UpdateTimer_Elapsed;
		}

		void UpdateTimerReset()
		{
			updateTimer.Stop();
			updateTimer.Start();
		}

		private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			CreateBackgroundPicture();
		}

		void deadzoneLink_ValueChanged(object sender, EventArgs e)
		{
			UpdateTimerReset();
		}

		DeadZoneWpfControlsLink deadzoneLink;
		DeadZoneWpfControlsLink antiDeadzoneLink;
		DeadZoneWpfControlsLink linearLink;

		System.Timers.Timer updateTimer;

		[Category("Appearance"), DefaultValue(0)]
		public string HeaderText
		{
			get => (string)MainGroupBox.Header;
			set => MainGroupBox.Header = value;
		}

		TargetType _TargetType;

		[Category("Appearance"), DefaultValue(TargetType.None)]
		public TargetType TargetType
		{
			get => _TargetType;
			set { _TargetType = value; UpdateTargetType(); }
		}

		void UpdateTargetType()
		{
			var maxValue = isThumb ? short.MaxValue : byte.MaxValue;
			deadzoneLink = new DeadZoneWpfControlsLink(DeadZoneTrackBar, DeadZoneUpDown, DeadZoneTextBox, 0, maxValue);
			deadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
			antiDeadzoneLink = new DeadZoneWpfControlsLink(AntiDeadZoneTrackBar, AntiDeadZoneUpDown, AntiDeadZoneTextBox, 0, maxValue);
			antiDeadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
			linearLink = new DeadZoneWpfControlsLink(LinearTrackBar, LinearUpDown, LinearTextBox, -100, 100);
			linearLink.ValueChanged += deadzoneLink_ValueChanged;
			UpdateTimerReset();
		}

		private bool isThumb =>
				TargetType == TargetType.LeftThumbX ||
				TargetType == TargetType.LeftThumbY ||
				TargetType == TargetType.RightThumbX ||
				TargetType == TargetType.RightThumbY;

		//Image LastBackgroundImage = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (deadzoneLink != null)
					deadzoneLink.Dispose();
				if (antiDeadzoneLink != null)
					antiDeadzoneLink.Dispose();
				if (updateTimer != null)
					updateTimer.Dispose();
			}
		}

		private void ThumbUserControl_EnabledChanged(object sender, EventArgs e)
		{
			//MainPictureBox.Image = IsEnabled ? LastBackgroundImage : null;
			//MainPictureBox.BackColor = Enabled ? System.Drawing.Color.White : System.Drawing.SystemColors.Control;
		}

		// Half and Invert values are only in creating XInput path - red line.
		private bool _invert;
		private bool _half;

		private float w = 150f;
		private float h = 150f;

		public void SetBinding(PadSetting o)
		{
			// Unbind first.
			SettingsManager.UnLoadMonitor(DeadZoneUpDown);
			SettingsManager.UnLoadMonitor(AntiDeadZoneUpDown);
			SettingsManager.UnLoadMonitor(LinearUpDown);
			if (o == null)
				return;
			// Set binding.
			switch (TargetType)
			{
				case TargetType.LeftTrigger:
					SettingsManager.LoadAndMonitor(o, nameof(o.LeftTriggerDeadZone), DeadZoneUpDown);
					SettingsManager.LoadAndMonitor(o, nameof(o.LeftTriggerAntiDeadZone), AntiDeadZoneUpDown);
					SettingsManager.LoadAndMonitor(o, nameof(o.LeftTriggerLinear), LinearUpDown);
					break;
				default:
					break;
			}
		}

		public void DrawPoint(int dInput, int xInput, bool invert, bool half)
		{
			// If properties affecting curve changed then...
			if (_invert != invert || _half != half)
				UpdateTimerReset();
			// Set invert and half properties.
			_invert = invert;
			_half = half;
			// Draw.
			DInputValueLabel.Content = dInput.ToString();
			XInputValueLabel.Content = xInput.ToString();
			var di = ConvertDInputToImagePosition(dInput);
			var xi = ConvertXInputToImagePosition(xInput);
			// Draw vertical DirectInput line in the middle.
			// If line goes outside of the image then...
			if (di >= w)
				di = w - 1f;
			DInputLineGeometry.StartPoint = new Point(di, 0);
			DInputLineGeometry.EndPoint = new Point(di, h);
			if (xi >= h)
				xi = h - 1f;
			// If line goes outside of the image then...
			XInputLineGeometry.StartPoint = new Point(0, xi);
			XInputLineGeometry.EndPoint = new Point(w, xi);
			// Draw dots.
			XInputEllipse.Center = new Point(di, xi);
			DInputEllipse.Center = new Point(di, h - di);
		}


		public float ConvertDInputToImagePosition(float v)
		{
			var di = ConvertHelper.ConvertRangeF(0f, ushort.MaxValue, 0f, w, v);
			return di;
		}

		public float ConvertXInputToImagePosition(float v)
		{
			var min = isThumb ? -32768f : 0f;
			var max = isThumb ? 32767f : 255f;
			var xi = ConvertHelper.ConvertRangeF(min, max, 0f, h, v);
			return xi;
		}

		public void InitPaintObjects()
		{
			xInputPath = new SolidColorBrush(Colors.Red);
			// Create thin lines.
			var xInputLineBrush = new SolidColorBrush(Colors.Blue);
			xInputLineBrush.Opacity = 0.125f;
			xInputLine = new Pen(xInputLineBrush, 1f);
			var dInputLineBrush = new SolidColorBrush(Colors.Green);
			dInputLineBrush.Opacity = 0.125f;
			dInputLine = new Pen(dInputLineBrush, 1f);
			var nInputLineBrush = new SolidColorBrush(Colors.Gray);
			nInputLineBrush.Opacity = 0.125f;
			nInputLine = new Pen(nInputLineBrush, 1f);
		}

		private SolidColorBrush xInputPath;
		private Pen xInputLine;
		private Pen dInputLine;
		private Pen nInputLine;

		private void CreateBackgroundPicture()
		{
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(new Action(() => CreateBackgroundPicture()));
				return;
			}
			var deadZone = DeadZoneUpDown.Value ?? 0;
			var antiDeadZone = AntiDeadZoneUpDown.Value ?? 0;
			var sensitivity = LinearUpDown.Value ?? 0;
			var visual = new DrawingVisual();
			// Retrieve the DrawingContext in order to create new drawing content.
			var g = visual.RenderOpen();
			// Persist the drawing content.
			g.DrawRectangle(Brushes.White, null, new Rect(new Point(0, 0), new Size(w, h)));
			// Draw vertical DInput line in the middle.
			var dim = (float)Math.Floor(w / 2);
			g.DrawLine(dInputLine, new Point(dim, 0), new Point(dim, h));
			// Draw vertical XInput line in the middle.
			var xim = (float)Math.Floor(h / 2);
			g.DrawLine(xInputLine, new Point(0, xim), new Point(w, xim));
			// Draw grey line from bottom-left to top-right.
			g.DrawLine(nInputLine, new Point(0f, h - 1f), new Point(w - 1f, 0f));
			for (var i = 0f; i <= w; i += 0.5f)
			{
				// Convert Image X position [0;w] to DInput position [0;65535].
				var dInputValue = ConvertHelper.ConvertRangeF(0f, w, ushort.MinValue, ushort.MaxValue, i);
				var xInputValue = ConvertHelper.GetThumbValue(dInputValue, deadZone, antiDeadZone, sensitivity, _invert, _half, isThumb);
				//var rounded = result >= -1f && result <= 1f;
				var di = ConvertDInputToImagePosition(dInputValue);
				var xi = ConvertXInputToImagePosition(xInputValue);
				// Put red dot where XInput dot must travel. Use radius to fix eclipse position.
				DrawDot(g, di, xi, xInputPath);
			}
			// Finish rendering.
			g.Close();
			var bmp = new RenderTargetBitmap((int)w, (int)h, 96, 96, PixelFormats.Pbgra32);
			bmp.Render(visual);
			// Encoding the RenderBitmapTarget as a PNG file.
			var png = new PngBitmapEncoder();
			png.Frames.Add(BitmapFrame.Create(bmp));
			var ms = new MemoryStream();
			png.Save(ms);
			var imageSource = new BitmapImage();
			imageSource.BeginInit();
			imageSource.StreamSource = ms;
			imageSource.EndInit();
			if (IsEnabled)
				MainPictureBox.Source = imageSource;
			else
				MainPictureBox.Source = null;
		}

		void DrawDot(DrawingContext g, float x, float y, Brush brush, bool snap = false)
		{
			// Half pixel.
			var p = 0.5f;
			// If snap all.
			if (snap)
			{
				// Snap to pixels.
				x = (float)Math.Floor(x);
				// Make sure last line is not snapped outside.
				if (x == w)
					x -= 1f;
				x += p;
			}
			else
			{
				var wm = (w / 2f);
				var hm = (h / 2f);
				// Snap X to start, center and end.
				if (x < 1f)
					x = p;
				if (x >= wm - p && x <= wm + p)
					x = wm;
				if (x > w - 1f)
					x = w - p;
				// Snap Y to top, middle and bottom.
				if (y < 1f)
					y = p;
				if (y >= hm - p && y <= hm + p)
					y = hm;
				if (y > h - 1f)
					y = h - p;
			}
			g.DrawEllipse(brush, null, new Point(x, y), 0.5f, 0.5f);
		}

		private void P_X_Y_Z_MenuItem_Click(object sender, EventArgs e)
		{
			var c = (MenuItem)sender;
			var values = c.Name.Split('_');
			decimal xDeadZone = 0;
			switch (TargetType)
			{
				case TargetType.LeftTrigger:
				case TargetType.RightTrigger:
					xDeadZone = Controller.XINPUT_GAMEPAD_TRIGGER_THRESHOLD;
					break;
				case TargetType.LeftThumbX:
				case TargetType.LeftThumbY:
					xDeadZone = Controller.XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE;
					break;
				case TargetType.RightThumbX:
				case TargetType.RightThumbY:
					xDeadZone = Controller.XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE;
					break;
				default:
					break;
			}
			var deadZone = int.Parse(values[1]);
			var antiDeadZone = decimal.Parse(values[2]);
			var sensitivity = int.Parse(values[3]);
			// Move focus away from below controls, so that their value can be changed.
			//ActiveControl = SensitivityCheckBox;
			DeadZoneTrackBar.Value = deadZone;
			AntiDeadZoneUpDown.Value = (int)(xDeadZone * antiDeadZone / 100m);
			LinearTrackBar.Value = sensitivity;
		}

		private void LinearUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			var text = (int)(e.NewValue ?? 0) < 0
				? "Sensitivity - Make more sensitive in the center:"
				: "Sensitivity - Make less sensitive in the center:";
			ControlsHelper.SetText(SensitivityLabel, text);
		}
	}
}

