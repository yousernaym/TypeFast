﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace TyperUWP
{
	class Text : TyperLib.Text
	{
		Panel textPanel;
		TextBlockEx[] writtenTextControls = new TextBlockEx[NumCharsFromCenter];
		TextBlockEx currentCharControl;
		TextBlock unwrittenTextControl;
		double spaceWidth;
		SolidColorBrush backgroundBrush;
		public Color Background
		{
			get => backgroundBrush.Color;
			set
			{
				backgroundBrush = new SolidColorBrush(value);
				applyStyle();
			}
		}
		SolidColorBrush foregroundBrush;
		public Color Foreground
		{
			get => foregroundBrush.Color;
			set
			{
				foregroundBrush = new SolidColorBrush(value);
				applyStyle();
			}
		}
		SolidColorBrush correctBrush;
		SolidColorBrush errorBrush;
		double fontSize;
		public double FontSize
		{
			get => fontSize;
			set
			{
				fontSize = value;
				applyStyle();
			}
		}
		FontFamily fontFamily;
		public string FontName
		{
			get => fontFamily.Source;
			set
			{
				fontFamily = new FontFamily(value);
				applyStyle();
			}
		}
		
		public Text(Panel _textPanel, StackPanel writtenTextPanel, TextBlockEx _currentCharControl, TextBlock _unwrittenTextControl)
		{
			textPanel = _textPanel;
			for (int i = writtenTextControls.Length - 1; i >= 0; i--)
			{
				writtenTextControls[i] = new TextBlockEx();
				writtenTextPanel.Children.Add(writtenTextControls[i]);
			}
			
			currentCharControl = _currentCharControl;
			unwrittenTextControl = _unwrittenTextControl;
			fontFamily = new FontFamily(currentCharControl.FontFamily.Source);
			fontSize = 30;
			backgroundBrush = new SolidColorBrush(Colors.Black);
			foregroundBrush = new SolidColorBrush(Colors.White);
			applyStyle();

			errorBrush = new SolidColorBrush(Colors.Red);
			correctBrush = new SolidColorBrush(Colors.Green);
		}

		public void applyStyle()
		{
			//Set panel background
			textPanel.Background = backgroundBrush;
			
			//Written text font
			foreach (var control in writtenTextControls)
			{
				control.FontFamily = fontFamily;
				control.FontSize = fontSize;
				control.Foreground = foregroundBrush;
			}

			//Current character
			currentCharControl.Background = foregroundBrush;
			currentCharControl.Foreground = backgroundBrush;
			currentCharControl.FontFamily = fontFamily;
			currentCharControl.FontSize = fontSize;

			//Set color of unwritten text
			unwrittenTextControl.Foreground = foregroundBrush;
			unwrittenTextControl.FontFamily = fontFamily;
			unwrittenTextControl.FontSize = fontSize;

			//Determine width of text blocks with just a space
			var tb = new TextBlock();
			tb.FontFamily = fontFamily;
			tb.FontSize = fontSize;
			tb.Text = "a a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a_a = tb.DesiredSize.Width;
			tb.Text = "a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a = tb.DesiredSize.Width;
			spaceWidth = a_a - (a - 1) * 2;
			reset();
		}

		new public void reset()
		{
			base.reset();
			draw();
		}

		public void draw()
		{
			//Draw written chars
			var currentChar = writtenChars.First;
			foreach (var control in writtenTextControls)
			{
				if (currentChar == null)
				{
					control.Text = "";
					control.Background = backgroundBrush;
					continue;
				}
				bool isCorrect = currentChar.Value.Item1;
				char c = currentChar.Value.Item2;
				if (c == ' ')
					control.Background = isCorrect ? backgroundBrush : errorBrush;
				else
				{
					control.Foreground = isCorrect ? correctBrush : errorBrush;
					control.Background = backgroundBrush;
				}

					control.Text = c.ToString();
				control.Width = c == ' ' ? spaceWidth : Double.NaN;

				currentChar = currentChar.Next;
			}
			if (string.IsNullOrEmpty(unwrittenTextToDraw))
			{
				unwrittenTextControl.Text = currentCharControl.Text = "";
			}
			else
			{
				char c = unwrittenTextToDraw[0];
				currentCharControl.Text = c.ToString();
				currentCharControl.Width = c == ' ' ? spaceWidth : Double.NaN;
				unwrittenTextControl.Text = unwrittenTextToDraw.Substring(1, unwrittenTextToDraw.Length - 1);
			}
		}
	}

	public class TimeLimitConverter : Windows.UI.Xaml.Data.IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var time = (TimeSpan)value;
			return time.Minutes.ToString("d2") + ":" + time.Seconds.ToString("d2");
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			var time = (string)value;
			return new TimeSpan(0, int.Parse(time.Substring(0, 2)), int.Parse(time.Substring(3, 2)));
		}
	}
}
