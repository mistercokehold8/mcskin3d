﻿//
//    MCSkin3D, a 3d skin management studio for Minecraft
//    Copyright (C) 2013 Altered Softworks & MCSkin3D Team
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

using Devcorp.Controls.Design;
using System;
using System.Windows.Forms;

namespace Paril.Controls.Color
{
	public partial class ColorPicker : Form
	{
		private bool _skipSet;

		public ColorPicker()
		{
			InitializeComponent();
		}

		public HSL MyHSL
		{
			get { return new HSL(colorSquare1.CurrentHue, colorSquare1.CurrentSat / 240.0f, saturationSlider1.CurrentLum / 240.0f); }
		}

		public System.Drawing.Color CurrentColor
		{
			get { return System.Drawing.Color.FromArgb((int) numericUpDown4.Value, ColorSpaceHelper.HSLtoRGB(MyHSL).ToColor()); }
		}

		private void ColorPicker_Load(object sender, EventArgs e)
		{
			panel1.BackColor = ColorSpaceHelper.HSLtoRGB(MyHSL).ToColor();
			saturationSlider1.Color = new HSL(colorSquare1.CurrentHue, (double) colorSquare1.CurrentSat / 240.0f, 0);
			SetColors();
		}

		private void SetColors()
		{
			_skipSet = true;
			numericUpDown5.Value = CurrentColor.R;
			numericUpDown6.Value = CurrentColor.G;
			numericUpDown7.Value = CurrentColor.B;
			_skipSet = false;
		}

		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			if (_skipSet)
				return;

			colorSquare1.CurrentHue = (int) numericUpDown1.Value;
			SetColors();
		}

		private void numericUpDown2_ValueChanged(object sender, EventArgs e)
		{
			if (_skipSet)
				return;

			colorSquare1.CurrentSat = (int) numericUpDown2.Value;
			SetColors();
		}

		private void colorSquare1_HueChanged(object sender, EventArgs e)
		{
			if (_skipSet)
				return;

			_skipSet = true;
			numericUpDown1.Value = colorSquare1.CurrentHue;

			saturationSlider1.Color = new HSL(colorSquare1.CurrentHue, (double) colorSquare1.CurrentSat / 240.0f, 0);
			panel1.BackColor = ColorSpaceHelper.HSLtoRGB(MyHSL).ToColor();
			SetColors();
			_skipSet = false;
		}

		private void colorSquare1_SatChanged(object sender, EventArgs e)
		{
			if (_skipSet)
				return;

			_skipSet = true;
			numericUpDown2.Value = colorSquare1.CurrentSat;

			saturationSlider1.Color = new HSL(colorSquare1.CurrentHue, (double) colorSquare1.CurrentSat / 240.0f, 0);
			panel1.BackColor = ColorSpaceHelper.HSLtoRGB(MyHSL).ToColor();
			SetColors();
			_skipSet = false;
		}

		private void saturationSlider1_LumChanged(object sender, EventArgs e)
		{
			if (_skipSet)
				return;

			_skipSet = true;
			numericUpDown3.Value = saturationSlider1.CurrentLum;

			panel1.BackColor = ColorSpaceHelper.HSLtoRGB(MyHSL).ToColor();
			SetColors();
			_skipSet = false;
		}

		private void numericUpDown3_ValueChanged(object sender, EventArgs e)
		{
			if (_skipSet)
				return;

			saturationSlider1.CurrentLum = (int) numericUpDown3.Value;
			saturationSlider1.Color = new HSL(colorSquare1.CurrentHue, (double) colorSquare1.CurrentSat / 240.0f, 0);
			panel1.BackColor = ColorSpaceHelper.HSLtoRGB(MyHSL).ToColor();
		}

		private void numericUpDown5_ValueChanged(object sender, EventArgs e)
		{
			if (_skipSet)
				return;

			System.Drawing.Color asRGB = System.Drawing.Color.FromArgb((int) numericUpDown5.Value, (int) numericUpDown6.Value,
			                                                           (int) numericUpDown7.Value);
			HSL hsl = ColorSpaceHelper.RGBtoHSL(asRGB);

			_skipSet = true;

			numericUpDown1.Value = (int) hsl.Hue;
			numericUpDown2.Value = (int) (hsl.Saturation * 240.0f);
			numericUpDown3.Value = (int) (hsl.Luminance * 240.0f);

			colorSquare1.CurrentHue = (int) numericUpDown1.Value;
			colorSquare1.CurrentSat = (int) numericUpDown2.Value;
			saturationSlider1.CurrentLum = (int) numericUpDown3.Value;
			saturationSlider1.Color = new HSL(colorSquare1.CurrentHue, (double) colorSquare1.CurrentSat / 240.0f, 0);
			panel1.BackColor = asRGB;

			_skipSet = false;
		}

		private void numericUpDown6_ValueChanged(object sender, EventArgs e)
		{
			if (_skipSet)
				return;

			System.Drawing.Color asRGB = System.Drawing.Color.FromArgb((int) numericUpDown5.Value, (int) numericUpDown6.Value,
			                                                           (int) numericUpDown7.Value);
			HSL hsl = ColorSpaceHelper.RGBtoHSL(asRGB);

			_skipSet = true;

			numericUpDown1.Value = (int) hsl.Hue;
			numericUpDown2.Value = (int) (hsl.Saturation * 240.0f);
			numericUpDown3.Value = (int) (hsl.Luminance * 240.0f);

			colorSquare1.CurrentHue = (int) numericUpDown1.Value;
			colorSquare1.CurrentSat = (int) numericUpDown2.Value;
			saturationSlider1.CurrentLum = (int) numericUpDown3.Value;
			saturationSlider1.Color = new HSL(colorSquare1.CurrentHue, (double) colorSquare1.CurrentSat / 240.0f, 0);
			panel1.BackColor = asRGB;

			_skipSet = false;
		}

		private void numericUpDown7_ValueChanged(object sender, EventArgs e)
		{
			if (_skipSet)
				return;

			System.Drawing.Color asRGB = System.Drawing.Color.FromArgb((int) numericUpDown5.Value, (int) numericUpDown6.Value,
			                                                           (int) numericUpDown7.Value);
			HSL hsl = ColorSpaceHelper.RGBtoHSL(asRGB);

			_skipSet = true;

			numericUpDown1.Value = (int) hsl.Hue;
			numericUpDown2.Value = (int) (hsl.Saturation * 240.0f);
			numericUpDown3.Value = (int) (hsl.Luminance * 240.0f);

			colorSquare1.CurrentHue = (int) numericUpDown1.Value;
			colorSquare1.CurrentSat = (int) numericUpDown2.Value;
			saturationSlider1.CurrentLum = (int) numericUpDown3.Value;
			saturationSlider1.Color = new HSL(colorSquare1.CurrentHue, (double) colorSquare1.CurrentSat / 240.0f, 0);
			panel1.BackColor = asRGB;

			_skipSet = false;
		}

		private void numericUpDown4_ValueChanged(object sender, EventArgs e)
		{
		}
	}
}