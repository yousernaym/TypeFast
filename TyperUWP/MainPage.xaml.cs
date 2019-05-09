using System;
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
		Texts texts = new Texts(ApplicationData.Current.LocalFolder.Path);
		private bool dialogOpen = false;

		public MainPage()
		{
			this.InitializeComponent();
			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(1000, 500));
			//ApplicationView.PreferredLaunchViewSize = new Size(1000, );
			//ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
			Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
			Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown; ;

			text = new Text(textPanel, writtenTextPanel, currentCharControl, unwrittenTextControl);
            //text.TimeLimit = TimeSpan.FromSeconds(60);
			text.TimeChecked += Text_TimeChecked;
			text.Finished += Text_Finished;
			//text.Foreground = Colors.White;
			//text.Background = Colors.Black;
			textColorBtn.Background = new SolidColorBrush(text.Foreground);
			textBkgColorBtn.Background = new SolidColorBrush(text.Background);
			text.FontSize = 50;
			selectText(null);  //Select random text

			string[] fonts = CanvasTextFormat.GetSystemFontFamilies();
			foreach (string font in fonts)
				fontCombo.Items.Add(font);
			fontCombo.SelectedItem = text.FontName;
			fontSizeTb.Text = text.FontSize.ToString();
		}

		private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			if (isKeyDown(VirtualKey.Control))
			{
				if (args.VirtualKey == VirtualKey.R)
					clickResetBtn();
			}
		}

		private void Text_Finished(object sender, EventArgs e)
		{
			if (texts.Current != null)
				texts.addRecord(text.Wpm, text.Accuracy, texts.Current.Title);
		}

		private async void Text_TimeChecked(object sender, EventArgs e)
		{
			//if (text.IsFinished) //This hethod may be called a few times after finishing
			//	return;
			await Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate
			{
				timeText.Content = "Time\n" + text.RemainingTimeString;
				wpmText.Text = "WPM\n" + text.Wpm;
				accuracyText.Text = "Accuracy\n" + text.Accuracy.ToString("0.0") + " %";
			
				if (text.IsRunning)
					timeText.Background = new SolidColorBrush(Color.FromArgb(255, 0, 80, 0));
				else if (text.IsFinished)
					timeText.Background = new SolidColorBrush(Colors.DarkRed);
				else
					timeText.Background = new SolidColorBrush(Color.FromArgb(30, 250, 250, 250));

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
			if (args.KeyCode == 27) //Esc
				removeFocus(textsAsb);
			if (dialogOpen || isKeyDown(VirtualKey.Control) || isKeyDown(VirtualKey.Menu))
				return;

			args.Handled = true; //needed?
			if (!text.typeChar(args.KeyCode))
				return;
			currentCharControl.Focus(FocusState.Programmatic);
			text.draw();
			updateTypingStats();
		}

		private void updateTypingStats()
		{
			correctCharsText.Text = "Correct\n" + text.CorrectChars;
			incorrectCharsText.Text = "Incorrect\n" + text.IncorrectChars;
			fixedCharsText.Text = "Fixed\n" + text.FixedChars;
		}

		private void RestartBtn_Click(object sender, RoutedEventArgs e)
		{
			clickResetBtn();
		}

		private void clickResetBtn()
		{
			removeFocus(textsAsb);
			if ((bool)shuffleBtn.IsChecked)
				selectText(null);
			else
				reset();
		}

		void reset()
		{
			if (dialogOpen)
				return;

			text.reset();
			updateTypingStats();
			removeFocus(textsAsb);
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			currentCharControl.Focus(FocusState.Programmatic);
		}

		async private void TextsComboCmNew_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			NewTextDialog newTextDialog = new NewTextDialog(texts, false);
			newTextDialog.Title = "Add new text";
			dialogOpen = true;
			ContentDialogResult result = await newTextDialog.ShowAsync();
			dialogOpen = false;
			
			if (result == ContentDialogResult.Primary)
				selectText(newTextDialog.TitleEntry);
		}

		async private void TextsComboCmEdit_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			NewTextDialog newTextDialog = new NewTextDialog(texts, true);
			newTextDialog.Title = "Edit text";
			newTextDialog.TitleEntry = texts.Current.Title;
			newTextDialog.TextEntry = texts.Current.Text;
			dialogOpen = true;
			ContentDialogResult result = await newTextDialog.ShowAsync();
			dialogOpen = false;

			if (result == ContentDialogResult.Primary)
				selectText(newTextDialog.TitleEntry);
		}

		async private void TextsComboCmDelete_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			var dlg = new ContentDialog { PrimaryButtonText = "Yes", CloseButtonText = "No", Content = "Are you sure you want to permanently delete this text and all its associated records?" };
			ContentDialogResult result = await dlg.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				texts.removeCurrent();
				selectText(texts.Current.Title);
			}
		}

		async private void TextCmPaste_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			DataPackageView dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				text.Text = await dataPackageView.GetTextAsync();
				reset();
				//selectedTextTbk.Text = "Clipboard contents"; ;
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

		private void FontMFI_Click(object sender, RoutedEventArgs e)
		{
			var showOptions = new FlyoutShowOptions();
			showOptions.Placement = FlyoutPlacementMode.Bottom;
			fontStyleFlyout.ShowAt(textPanel, showOptions);
		}

		private void RecordsFlyout_Opened(object sender, object e)
		{
			recordsView.syncGrid(texts);
			dialogOpen = true;
		}

		private void RecordsFlyout_Closed(object sender, object e)
		{
			dialogOpen = false;
		}

		private void RecordsView_TextTitleClick(RecordsView recordsView, TextTitleClickEventArgs e)
		{
			recordsFlyout.Hide();
			dialogOpen = false;
			selectText(e.Title);
		}

		private void selectText(string title)
		{
			if (title == null)
				texts.selectRandom();
			else
				texts.select(title);
			text.Text = texts.Current?.Text;
			textsAsb.PlaceholderText = texts.Current == null ? "" : texts.Current.Title;
			textsAsb.Text = "";
			reset();

			textsComb.ItemSource = texts.Titles;
		}

		private void TextsASB_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				findAsbTitleMatches();
		}

		void findAsbTitleMatches()
		{
			var matchingTexts = new LinkedList<string>();
			foreach (var text in texts)
			{
				if (text.Title.ToLower().Contains(textsAsb.Text.ToLower()))
					matchingTexts.AddFirst(text.Title);
			}
			if (matchingTexts.Count == 0)
				matchingTexts.AddFirst("No matching titles found.");
			textsAsb.ItemsSource = matchingTexts;
		}

		private void TextsAsb_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			removeFocus(sender);
			if (texts.containsTitle(args.QueryText))
				selectText(args.QueryText);
		}

		private void TextsAsb_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			sender.Text = (string)args.SelectedItem;
		}

		private void TextsAsb_GotFocus(object sender, RoutedEventArgs e)
		{
			dialogOpen = true;
			findAsbTitleMatches();
		}

		private void TextsAsb_LostFocus(object sender, RoutedEventArgs e)
		{
			dialogOpen = false;
		}

		void removeFocus(Control control)
		{
			control.IsEnabled = false;
			control.IsEnabled = true;
			dialogOpen = false;
		}
	}
}
