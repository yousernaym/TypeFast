using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
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
	[Serializable]
	public class TypingSession : TyperLib.TypingSession, ISerializable
	{
		public static readonly Type[] SerializeTypes = new Type[] { typeof(Color), typeof(TyperLib.TextEntry) };
		public TypingSessionView View { get; set; }
		public SolidColorBrush BackgroundBrush { get; private set; }
		public Color Background
		{
			get => BackgroundBrush.Color;
			set
			{
				BackgroundBrush = new SolidColorBrush(value);
				View?.applyStyle();
			}
		}
		public SolidColorBrush ForegroundBrush { get; private set; }
		public Color Foreground
		{
			get => ForegroundBrush.Color;
			set
			{
				ForegroundBrush = new SolidColorBrush(value);
				View?.applyStyle();
			}
		}
		public SolidColorBrush CorrectForegroundBrush { get; private set; }
		public Color CorrectForeground
		{
			get => CorrectForegroundBrush.Color;
			set
			{
				CorrectForegroundBrush = new SolidColorBrush(value);
				View?.applyStyle();
			}
		}

		public SolidColorBrush ErrorForegroundBrush { get; private set; }
		public Color ErrorForeground
		{
			get => ErrorForegroundBrush.Color;
			set
			{
				ErrorForegroundBrush = new SolidColorBrush(value);
				View?.applyStyle();
			}
		}

		double fontSize;
		public double FontSize
		{
			get => fontSize;
			set
			{
				fontSize = value;
				View?.applyStyle();
			}
		}

		public FontFamily FontFamily { get; private set; }
		public string FontName
		{
			get => FontFamily.Source;
			set
			{
				FontFamily = new FontFamily(value);
				View?.applyStyle();
			}
		}

		public TypingSession()
		{
			FontName = "Gadugi";
			FontSize = 35;
			Background = Colors.Black;
			Foreground = Color.FromArgb(255, 255, 255, 220);
			ErrorForeground = Colors.Red;
			CorrectForeground = Colors.Green;
		}

		public TypingSession(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "background")
					Background = (Color)entry.Value;
				else if (entry.Name == "foreground")
					Foreground = (Color)entry.Value;
				else if (entry.Name == "correctForeground")
					CorrectForeground = (Color)entry.Value;
				else if (entry.Name == "errorForeground")
					ErrorForeground = (Color)entry.Value;
				else if (entry.Name == "fontName")
					FontName = (string)entry.Value;
				else if (entry.Name == "fontSize")
					fontSize = (double)entry.Value;
			}
		}

		override public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("background", Background);
			info.AddValue("foreground", Foreground);
			info.AddValue("correctForeground", CorrectForeground);
			info.AddValue("errorForeground", ErrorForeground);
			info.AddValue("fontName", FontName);
			info.AddValue("fontSize", fontSize);
		}
	}

	public class TypingSessionView
	{
		Panel textPanel;
		TextBlockEx[] writtenTextControls;
		TextBlockEx currentCharControl;
		TextBlock unwrittenTextControl;
		double spaceWidth;
		TypingSession session;
		public TypingSession Session
		{
			get => session;
			set
			{
				session = value;
				session.View = this;
				applyStyle();
			}
		}

		public TypingSessionView(Panel _textPanel, StackPanel writtenTextPanel, TextBlockEx _currentCharControl, TextBlock _unwrittenTextControl, TypingSession session)
		{
			writtenTextControls = new TextBlockEx[TypingSession.NumCharsFromCenter];
			textPanel = _textPanel;
			for (int i = writtenTextControls.Length - 1; i >= 0; i--)
			{
				writtenTextControls[i] = new TextBlockEx();
				writtenTextPanel.Children.Add(writtenTextControls[i]);
			}
			
			currentCharControl = _currentCharControl;
			unwrittenTextControl = _unwrittenTextControl;
			Session = session;
		}

		public void applyStyle()
		{
			//Set panel background
			textPanel.Background = Session.BackgroundBrush;
			
			//Written text font
			foreach (var control in writtenTextControls)
			{
				control.FontFamily = Session.FontFamily;
				control.FontSize = Session.FontSize; 
				control.Foreground = Session.ForegroundBrush;
			}

			//Current character
			currentCharControl.Background = Session.ForegroundBrush;
			currentCharControl.Foreground = Session.BackgroundBrush;
			currentCharControl.FontFamily = Session.FontFamily;
			currentCharControl.FontSize = Session.FontSize;

			//Set color of unwritten text
			unwrittenTextControl.Foreground = Session.ForegroundBrush;
			unwrittenTextControl.FontFamily = Session.FontFamily;
			unwrittenTextControl.FontSize = Session.FontSize;

			//Determine width of text blocks with just a space
			var tb = new TextBlock();
			tb.FontFamily = Session.FontFamily;
			tb.FontSize = Session.FontSize;
			tb.Text = "a a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a_a = tb.DesiredSize.Width;
			tb.Text = "a";
			tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
			double a = tb.DesiredSize.Width;
			spaceWidth = a_a - (a - 1) * 2;
			reset();
		}

		public void reset()
		{
			Session.reset();
			draw();
		}

		public void draw()
		{
			//Draw written chars
			var currentChar = Session.WrittenChars.First;
			foreach (var control in writtenTextControls)
			{
				if (currentChar == null)
				{
					control.Text = "";
					control.Background = Session.BackgroundBrush;
					continue;
				}
				bool isCorrect = currentChar.Value.Item1;
				char c = currentChar.Value.Item2;
				if (c == ' ')
					control.Background = isCorrect ? Session.BackgroundBrush : Session.ErrorForegroundBrush;
				else
				{
					control.Foreground = isCorrect ? Session.CorrectForegroundBrush : Session.ErrorForegroundBrush;
					control.Background = Session.BackgroundBrush;
				}

					control.Text = c.ToString();
				control.Width = c == ' ' ? spaceWidth : Double.NaN;

				currentChar = currentChar.Next;
			}
			if (string.IsNullOrEmpty(Session.UnwrittenTextToDraw))
			{
				unwrittenTextControl.Text = currentCharControl.Text = "";
			}
			else
			{
				char c = Session.UnwrittenTextToDraw[0];
				currentCharControl.Text = c.ToString();
				currentCharControl.Width = c == ' ' ? spaceWidth : Double.NaN;
				unwrittenTextControl.Text = Session.UnwrittenTextToDraw.Substring(1, Session.UnwrittenTextToDraw.Length - 1);
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
