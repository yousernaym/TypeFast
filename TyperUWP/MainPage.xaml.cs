﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.System;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.ViewManagement;
using Windows.Storage;
using TyperLib;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Graphics.Canvas.Text;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TyperUWP
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		Text text;
		TextList textList = new TextList(ApplicationData.Current.LocalFolder.Path);
		private bool dialogOpen = false;

		public MainPage()
		{
			this.InitializeComponent();
			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(1000, 500));
			//ApplicationView.PreferredLaunchViewSize = new Size(1000, );
			//ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
			Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
			text = new Text(textPanel, writtenTextPanel, currentCharControl, unwrittenTextControl);
			text.TimeLimit = TimeSpan.FromSeconds(10);
			text.TimeChecked += Text_TimeChecked;
			//text.TheText = "abcdefghijklmnopqrstuvwxyzåäö";
			//text.loadText();
			text.Foreground = Colors.White;
			//text.Background =

			text.draw();
			
			updateTypingStats();
			textsCombo.Items.VectorChanged += textsCombo_Items_VectorChanged;

			foreach (var text in textList)
			{
				textsCombo.Items.Add(text.Title);
			}
			textsCombo.SelectedIndex = 0;

			string[] fonts = CanvasTextFormat.GetSystemFontFamilies();
			foreach (string font in fonts)
				fontCombo.Items.Add(font);
			fontCombo.SelectedItem = text.FontName;
			fontSizeTb.Text = text.FontSize.ToString();
		}

		private void textsCombo_Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			deleteTextBtn.IsEnabled = textsCombo.Items.Count > 0;
		}

		private async void Text_TimeChecked(object sender, EventArgs e)
		{
			await Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate
			{
				timeText.Content = "Time\n" + text.RemainingTimeString;
				wpmText.Text = "WPM\n" + text.Wpm;
				accuracyText.Text = "Accuracy\n" + text.Accuracy.ToString("0.00") + " %";

				if (text.IsFinished)
					timeText.Background = new SolidColorBrush(Colors.DarkRed);
				else if (text.IsRunning)
					timeText.Background = new SolidColorBrush(Color.FromArgb(255, 0, 80, 0));
				else
					timeText.Background = new SolidColorBrush(Color.FromArgb(10, 250, 250, 250));

			});
		}

		bool isKeyDown(VirtualKey key)
		{
			var state = CoreWindow.GetForCurrentThread().GetAsyncKeyState(key);
			return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
		}

		private bool isKeyLocked(VirtualKey key)
		{
			var state = CoreWindow.GetForCurrentThread().GetAsyncKeyState(key);
			return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Locked;
		}

		private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
		{
			if (dialogOpen || isKeyDown(VirtualKey.Control) || isKeyDown(VirtualKey.Menu))
				return;

			args.Handled = true; //needed?
			text.typeChar(args.KeyCode);
			text.draw();
			updateTypingStats();
		}

		private void updateTypingStats()
		{
			correctCharsText.Text = "Correct\n" + text.CorrectChars;
			incorrectCharsText.Text = "Incorrect\n" + text.IncorrectChars;
			fixedCharsText.Text = "Fixed\n" + text.FixedChars;
		}

		private void ResetBtn_Click(object sender, RoutedEventArgs e)
		{
			if ((bool)shuffleBtn.IsChecked)
			{
				text.TheText = textList.selectRandom().Text;
				textsCombo.SelectedItem = textList.Current.Title;
			}
			reset();
		}

		void reset()
		{
			if (dialogOpen)
				return;

			text.reset();
			updateTypingStats();
			//timeBorder.Background = new SolidColorBrush(Colors.Green);
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
		}

		async private void NewTextBtn_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			NewTextDialog newTextDialog = new NewTextDialog(textList);
			dialogOpen = true;
			ContentDialogResult result = await newTextDialog.ShowAsync();
			dialogOpen = false;
			
			if (result == ContentDialogResult.Primary)
			{
				textsCombo.Items.Add(newTextDialog.TitleEntry);
				textsCombo.SelectedIndex = textsCombo.Items.Count - 1;
			}
		}

		private void TextsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (dialogOpen)
				return;
			string title = (string)textsCombo.SelectedItem;
			if (string.IsNullOrEmpty(title))
				return;
			text.TheText = textList.select(title);
			reset();
		}

		async private void DeleteTextBtn_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			var dlg = new ContentDialog { PrimaryButtonText = "Yes", CloseButtonText = "No", Content = "Are you sure you want to delete this text? This action cannot be undone." };
			ContentDialogResult result = await dlg.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				textList.remove((string)textsCombo.SelectedItem);
				int selectedIndex = textsCombo.SelectedIndex;
				textsCombo.Items.RemoveAt(textsCombo.SelectedIndex);
				if (selectedIndex >= textsCombo.Items.Count)
					selectedIndex--;
				textsCombo.SelectedIndex = selectedIndex;
			}
		}

		async private void TextCmPaste_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			DataPackageView dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				text.TheText = await dataPackageView.GetTextAsync();
				reset();
				textsCombo.SelectedIndex = -1;
			}
		}

		private void TimeLimitTb_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
				timeLimitFlyout.Hide();
			else if (e.Key == VirtualKey.Left)
				timeLimitTb.SelectionStart = Math.Max(0, timeLimitTb.SelectionStart - 1);
		}

		private void TimeLimitFlyout_Opened(object sender, object e)
		{
			dialogOpen = true;
			timeLimitTb.SelectionStart = 1;
			timeLimitTb.SelectionLength = 1;
		}

		private void TimeLimitFlyout_Closed(object sender, object e)
		{
			dialogOpen = false;
			reset();
		}

		private void TimeLimitTb_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			bool digit = args.Character >= '0' && args.Character <= '9';
			int charPos = timeLimitTb.SelectionStart - 1;
			bool tooManySeconds = digit && charPos == 3 && args.Character > '5';
			if (!digit || tooManySeconds)
			{
				var strBuilder = new StringBuilder(timeLimitTb.Text);
				strBuilder.Remove(charPos, 1);
				strBuilder.Insert(charPos, tooManySeconds ? '5' : '0');
				timeLimitTb.Text = strBuilder.ToString();
				timeLimitTb.SelectionStart = charPos + 1;
			}
		}

		private void TimeLimitTb_SelectionChanging(TextBox sender, TextBoxSelectionChangingEventArgs args)
		{
			if (isKeyDown(VirtualKey.Left))
			{
				int caretPos = Math.Max(0, timeLimitTb.SelectionStart - 1);
				if (caretPos == 2)
					caretPos = 1; //Skip ':'
				timeLimitTb.SelectionStart = caretPos;
			}
		}

		private void TimeLimitTb_SelectionChanged(object sender, RoutedEventArgs e)
		{
			if (timeLimitTb.SelectionStart == 5)
				timeLimitTb.SelectionStart = 4;
			else if (timeLimitTb.SelectionStart == 2)
				timeLimitTb.SelectionStart = 3;
			timeLimitTb.SelectionLength = 1;
		}

		private void TimeLimitTb_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
		{
			if (args.NewText.Length < 5)
			{
				args.Cancel = true;
				return;
			}
			var caretPos = timeLimitTb.SelectionStart;
			for (int i = 0; i < args.NewText.Length; i++)
			{
				if (i != 2 && !char.IsDigit(args.NewText[i]) || i == 3 && args.NewText[i] > '5')
				{
					args.Cancel = true;
					return;
				}
			}
		}

		private void FontCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			text.FontName = (string)fontCombo.SelectedItem;
		}

		private void FontSizeTb_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (fontSizeTb.Text.Length > 0)
			{
				int size = int.Parse(fontSizeTb.Text);
				if (size > 0)
					text.FontSize = size;
			}
		}

		private void FontSizeTb_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
		{
			args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
		}

		private void FontStyleFlyout_Opened(object sender, object e)
		{
			dialogOpen = true;
		}

		private void FontStyleFlyout_Closed(object sender, object e)
		{
			dialogOpen = false;
		}

		private void TextColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
		{
			textColorBtn.Background = new SolidColorBrush(textColorPicker.Color);
		}

		private void TextBkgColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
		{
			textBkgColorBtn.Background = new SolidColorBrush(textBkgColorPicker.Color);

		}
	}
}
