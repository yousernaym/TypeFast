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
using Windows.Storage.Pickers;
using System.Runtime.Serialization;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Automation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TyperUWP
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		readonly string RoamingDataDir = ApplicationData.Current.RoamingFolder.Path;
		readonly string LocalDataDir = ApplicationData.Current.LocalFolder.Path;
		readonly string SettingsPath;
		const string TextsAssetsFolder = "texts/";
		
		TypingSessionView typingSessionView;
		TypingSession typingSession
		{
			get => typingSessionView.Session;
			set => typingSessionView.Session = value;
		}
		Texts texts;
		public bool DialogOpen = false;
		private bool clipboardChanged = true;
		AccessibilitySettings accessibilitySettings;
		Audio audio = new Audio();

		public MainPage()
		{
			this.InitializeComponent();
			
			//var importIcon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uEA52" };
			//textsOptionsImport.Icon = importIcon;
			//var exportIcon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uEDE2" };
			//textsOptionsExport.Icon = exportIcon;

			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(700, 480));

			//ApplicationView.PreferredLaunchViewSize = new Size(1000, 500);
			//ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;

			//ApplicationData.Current.DataChanged += Current_DataChanged;
			//ApplicationData.Current.DataChanged += new TypedEventHandler<ApplicationData, object>(DataChangeHandler);

			Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
			Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
			Application.Current.Suspending += Current_Suspending;
			
			SettingsPath = Path.Combine(RoamingDataDir, "settings");

			string[] fonts = CanvasTextFormat.GetSystemFontFamilies();
			foreach (string font in fonts)
				fontCombo.Items.Add(font);

			typingSessionView = new TypingSessionView(rootPanel, writtenTextPanel, currentCharControl, unwrittenTextControl, loadTypingSession());

			//saveSettings();

			Clipboard.ContentChanged += Clipboard_ContentChanged;
		}

		async private void Page_Loading(FrameworkElement sender, object args)
		{
			await audio.init();
			using (var presetsStream = await getPresetsStream())
				texts = new Texts(LocalDataDir, presetsStream);
			textsCombo.ItemSource = texts.Titles;
			await initTypingSession();

			accessibilitySettings = new AccessibilitySettings();
			accessibilitySettings.HighContrastChanged += AccessibilitySettings_HighContrastChanged;
			updateAccessibilitySettings();
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			currentCharControl.Focus(FocusState.Programmatic);
		}

		private void Clipboard_ContentChanged(object sender, object e)
		{
			try
			{
				DataPackageView dataPackageView = Clipboard.GetContent();
				textCmPaste.IsEnabled = dataPackageView.Contains(StandardDataFormats.Text);
			}
			catch (UnauthorizedAccessException)
			{
				clipboardChanged = true; //Handle clipboard changes when app gets focus
			}
		}

		async Task<Stream> getResourceStream(string path)
		{
			var uri = new Uri("ms-appx:///Assets/"+path);
			var sampleFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
			return await sampleFile.OpenStreamForReadAsync();
		}

		private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
		{
			saveSettings();
			audio.Dispose();
		}

		private void saveSettings()
		{
			//try
			//{
				using (var stream = File.Open(SettingsPath, FileMode.Create))
				{
					var dcs = new DataContractSerializer(typeof(TypingSession), TypingSession.SerializeTypes);
					dcs.WriteObject(stream, typingSession);
				}
			//}
			//catch 
			//{
				
			//}

		}

		private TypingSession loadTypingSession()
		{
			TypingSession session;
			try
			{
				using (var stream = File.Open(SettingsPath, FileMode.Open))
				{
					var dcs = new DataContractSerializer(typeof(TypingSession), TypingSession.SerializeTypes);
					session = (TypingSession)dcs.ReadObject(stream);
				}
			}
			catch (FileNotFoundException)
			{
				//It''s probably the first time the app is opened, meaning no settings file has been created yet.
				//Use default settings from session construtor
				session = new TypingSession();
			}
			return session;
		}

		async private Task initTypingSession()
		{
			//Invoke the ColorChanged events explicitly, because they won't be invoked if a session color is set to white since that's the default picker color.
			TextColorPicker_ColorChanged(null, null);
			TextBkgColorPicker_ColorChanged(null, null);

			//Load bible
			using (var bibleStream = await getResourceStream(TextsAssetsFolder + "bible_EN.xml")) 
				typingSession.Bible = new Bible(bibleStream);
			
			//Misc init
			fontCombo.SelectedItem = typingSession.FontName;
			fontSizeTb.Text = typingSession.FontSize.ToString();
			typingSession.TimeChecked += Text_TimeChecked;
			typingSession.Finished += Text_Finished;

			//Load char mapping file
			using (var charMapStream = await getResourceStream(TextsAssetsFolder + "charmap.txt"))
				typingSession.loadCharMap(charMapStream);
			
			//Restore text from last session 
			if (typingSession.StartText == null)
			{
				//No info saved about text from last session, so pick random text. Should only happen first time app starts.
				selectText(null);
			}
			else
			{
				if (string.IsNullOrWhiteSpace(typingSession.StartText.Title))
					selectTempText(typingSession.StartText.Text); //Text without title = temporary text (practice session or from clipboard).
				else if (texts.containsTitle(typingSession.StartText.Title))
					selectText(typingSession.StartText.Title); //Use title to select text from list.
				else
					selectText(null); //Text list no longer contains the text from last session for some reason, so select random text.
			}
		}			

		private void DataChangeHandler(ApplicationData sender, object args)
		{
			
		}

		private void Current_DataChanged(ApplicationData sender, object args)
		{
			//sender
		}

		private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			if (isKeyDown(VirtualKey.Control) && !DialogOpen)
			{
				//if (args.VirtualKey == VirtualKey.R)
				//	clickResetBtn();
				//if (args.VirtualKey == VirtualKey.T)
				//	textsCombo.Focus(FocusState.Programmatic);
			}
		}

		private void Text_Finished(object sender, EventArgs e)
		{
			audio.play(Audio.Type.Finished);
			if (texts.Current != null && !string.IsNullOrEmpty(texts.Current.Title))
			{
				texts.addRecord(typingSession);
				texts.saveUserData();
			}
		}


		private async void Text_TimeChecked(object sender, EventArgs e)
		{
			//if (text.IsFinished) //This hethod may be called a few times after finishing
			//	return;
			await Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate
			{
				timeText.Content = "Time\n" + typingSession.RemainingTimeString;
				wpmText.Text = "WPM\n" + typingSession.Wpm;
				accuracyText.Text = "Accuracy\n" + typingSession.Accuracy.ToString("0.0") + " %";

				if (typingSession.IsRunning)
					timeText.Background = new SolidColorBrush(Color.FromArgb(255, 0, 80, 0));
				else if (typingSession.IsFinished)
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
			//const uint Esc = 27;
			//if (args.KeyCode == Esc) //Esc should remove focus from the AutoSuggestBox
			//	focusOnTyping();
			if (DialogOpen)
				return;

			//Ignore spaces if in the beginning of a text
			if (!typingSession.IsRunning && args.KeyCode == (uint)' ')
				return;

			var result = typingSession.typeChar(args.KeyCode);
			if (result == TypingSession.KeyPressResult.NotTypable)
				return;
			focusOnTyping();
			typingSessionView.draw();
			updateTypingStats();
			if (result == TypingSession.KeyPressResult.Incorrect && typingSession.ErrorAudio)
				audio.play(Audio.Type.Error);
			else if (args.KeyCode == TypingSession.KeyCode_Space && typingSession.TypingAudio)
				audio.play(Audio.Type.Space);
			else if (result == TypingSession.KeyPressResult.DeleteIncorrect && typingSession.ErrorAudio)
				audio.play(Audio.Type.Fix);
			else if ((result == TypingSession.KeyPressResult.DeleteCorrect || result == TypingSession.KeyPressResult.DeleteIncorrect) && typingSession.TypingAudio)
				audio.play(Audio.Type.Backspace);
			else if (typingSession.TypingAudio)
				audio.play(Audio.Type.Typing);
		}

		private void updateTypingStats()
		{
			correctCharsText.Text = "Correct\n" + typingSession.CorrectChars;
			incorrectCharsText.Text = "Incorrect\n" + typingSession.IncorrectChars;
			fixedCharsText.Text = "Fixed\n" + typingSession.FixedChars;
		}

		private void RestartBtn_Click(object sender, RoutedEventArgs e)
		{
			clickResetBtn();
		}

		private void clickResetBtn()
		{
			//if (typingSession.Shuffle)
				//selectText(null);
			//else
				reset();
		}

		void reset()
		{
			if (DialogOpen)
				return;
			typingSession.TextEntry = texts.Current;
			textsCombo.resetFilter();
			textsCombo.SelectedItem = texts.Current?.Title;
			typingSessionView.reset();
			updateTypingStats();
			//focusOnTyping();
		}

		private void AccessibilitySettings_HighContrastChanged(AccessibilitySettings sender, object args)
		{
			updateAccessibilitySettings();
		}

		void updateAccessibilitySettings()
		{
			if (accessibilitySettings.HighContrast)
				hideWrittenCharsCb.IsChecked = underlineCurrentCharCb.IsChecked = true;
		}

		async private void TextsOptionsNew_Click(object sender, RoutedEventArgs e)
		{
			if (DialogOpen)
				return;
			TextDialog newTextDialog = new TextDialog(texts, typingSession, TextDialogType.New, textsCombo);
						
			DialogOpen = true;
			ContentDialogResult result = await newTextDialog.ShowAsync();
			DialogOpen = false;

			if (result == ContentDialogResult.Primary)
				selectText(newTextDialog.TitleField);
		}

		async private void TextsOptionsClone_Click(object sender, RoutedEventArgs e)
		{
			if (DialogOpen)
				return;
			TextDialog newTextDialog = new TextDialog(texts, typingSession, TextDialogType.Clone, textsCombo);

			DialogOpen = true;
			ContentDialogResult result = await newTextDialog.ShowAsync();
			DialogOpen = false;

			if (result == ContentDialogResult.Primary)
				selectText(newTextDialog.TitleField);
		}

		async private void TextsOptionsEdit_Click(object sender, RoutedEventArgs e)
		{
			if (DialogOpen)
				return;
			TextDialog newTextDialog = new TextDialog(texts, typingSession, TextDialogType.Edit, textsCombo);

			DialogOpen = true;
			ContentDialogResult result = await newTextDialog.ShowAsync();
			DialogOpen = false;

			if (result == ContentDialogResult.Primary)
				selectText(newTextDialog.TitleField);
		}

		async private void TextsOptionsDelete_Click(object sender, RoutedEventArgs e)
		{
			if (DialogOpen)
				return;
			var dlg = new ContentDialog { PrimaryButtonText = "Yes", CloseButtonText = "No", Content = "Are you sure you want to permanently delete this text?" };
			DialogOpen = true;
			ContentDialogResult result = await dlg.ShowAsync();
			DialogOpen = false;
			if (result == ContentDialogResult.Primary)
			{
				texts.removeCurrent();
				textsCombo.ItemSource = texts.Titles;
				selectText(texts.Current?.Title);
			}
		}

		async private void TextCmPaste_Click(object sender, RoutedEventArgs e)
		{
			if (DialogOpen)
				return;
			DataPackageView dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Text))
				selectTempText(await dataPackageView.GetTextAsync());
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
			DialogOpen = true;
			timeLimitTb.Text = timeToString(typingSession.TimeLimit);
			timeLimitTb.SelectionStart = 1;
			timeLimitTb.SelectionLength = 1;
		}

		public string timeToString(TimeSpan timeSpan)
		{
			return timeSpan.Minutes.ToString("d2") + ":" + timeSpan.Seconds.ToString("d2");
		}

		public TimeSpan stringToTime(string timeStr)
		{
			var timeSpan = new TimeSpan(0, int.Parse(timeStr.Substring(0, 2)), int.Parse(timeStr.Substring(3, 2)));
			return timeSpan;
		}

		private void TimeLimitFlyout_Closed(object sender, object e)
		{
			DialogOpen = false;
			var time = stringToTime(timeLimitTb.Text);
			if (time.TotalSeconds < 30)
				timeLimitTb.Text = "00:30";
			
			typingSession.TimeLimit = stringToTime(timeLimitTb.Text);
			reset();
			timeText.Content = "Time\n" + typingSession.RemainingTimeString;
		}

		private void TimeLimitTb_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			bool digit = args.Character >= '0' && args.Character <= '9';
			int charPos = timeLimitTb.SelectionStart;
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
			typingSession.FontName = (string)fontCombo.SelectedItem;
		}

		private void FontSizeTb_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (fontSizeTb.Text.Length > 0)
			{
				int size = int.Parse(fontSizeTb.Text);
				if (size > 0)
					typingSession.FontSize = size;
			}
		}

		private void FontSizeTb_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
		{
			args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
		}

		private void FontStyleFlyout_Opened(object sender, object e)
		{
			DialogOpen = true;
		}

		private void FontStyleFlyout_Closed(object sender, object e)
		{
			DialogOpen = false;
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
            fontStyleFlyout.Placement = FlyoutPlacementMode.Bottom;
            fontStyleFlyout.ShowAt(textPanel);
		}

		private void RecordsFlyout_Opened(object sender, object e)
		{
			recordsView.syncGrid(texts);
			DialogOpen = true;
		}

		private void RecordsFlyout_Closed(object sender, object e)
		{
			DialogOpen = false;
		}

		private void RecordsView_TextTitleClick(RecordsView recordsView, TextTitleClickEventArgs e)
		{
			recordsFlyout.Hide();
			DialogOpen = false;
			selectText(e.Title);
		}

		private void selectText(string title)
		{
			if (title == null)
				texts.selectRandom();
			else
				texts.select(title);
			textsOptionsEdit.IsEnabled = true;
			textsOptionsDelete.IsEnabled = true;
			reset();
		}

		void focusOnTyping()
		{
			DialogOpen = false;
			currentCharControl.Focus(FocusState.Programmatic);
		}

		async private void TextsOptionsExport_Click(object sender, RoutedEventArgs e)
		{
			if (DialogOpen)
				return;
			DialogOpen = true;
			var fsp = new FileSavePicker();
			fsp.FileTypeChoices.Add("Type Fast texts", new List<string>() { ".tft" });
			fsp.SuggestedFileName = "My texts";
			StorageFile file = await fsp.PickSaveFileAsync();
			if (file != null)
			{
				using (var stream = await file.OpenStreamForWriteAsync())
				{
					stream.SetLength(0);
					//texts.saveUserTexts(stream);
					texts.saveUserData(stream);
				}
			}
			DialogOpen = false;
		}

		async private void TextsOptionsImport_Click(object sender, RoutedEventArgs e)
		{
			if (DialogOpen)
				return;
			DialogOpen = true;
			try
			{
				var fop = new FileOpenPicker();
				//fop.FileTypeFilter.Add("*");
				fop.FileTypeFilter.Add(".tft");
				StorageFile file = await fop.PickSingleFileAsync();
				if (file != null)
				{
					using (var stream = await file.OpenStreamForReadAsync())
					{
						try
						{
							texts.importUserData(stream, true);
						}
						catch
						{
							var dlg = await new ContentDialog { PrimaryButtonText = "Ok", Content = "Couldn't load file." }.ShowAsync();
							return;
						}
					}
					textsCombo.ItemSource = texts.Titles;
					reset();
					//selectText(texts.Current?.Title);
				}
			}
			finally
			{
				DialogOpen = false;
			}
		}

		async private void TextsOptionsRestore_Click(object sender, RoutedEventArgs e)
		{
			await loadPresets();
            restorePresetsFlyout.Placement = FlyoutPlacementMode.Bottom;
            restorePresetsFlyout.ShowAt(resetButtonsPanel);
        }

		async Task<Stream> getPresetsStream()
		{
			return await getResourceStream(TextsAssetsFolder + "presets.tft");
		}

		async Task loadPresets()
		{
			using (var stream = await getPresetsStream())
				texts.importUserData(stream, false);
			
			textsCombo.ItemSource = texts.Titles;
			reset();
			//selectText(texts.Current?.Title);
		}

		private void TextsCombo_SelectionSubmitted(object sender, EventArgs args)
		{
			string title = (string)((ComboBoxEx)sender).SelectedItem;
			if (title == "")
			{
				focusOnTyping();
				return;
			}
			if (texts.containsTitle(title))
			{
				focusOnTyping();
				selectText(title);
			}
		}

		private void TextsCombo_GotFocus(object sender, RoutedEventArgs e)
		{
			DialogOpen = true;
		}

		private void TextsCombo_LostFocus(object sender, RoutedEventArgs e)
		{
			DialogOpen = false;
		}

		async private void TextCmPractice_Click(object sender, RoutedEventArgs e)
		{
			if (DialogOpen)
				return;
			var dlg = new PracticeDialog();
			DialogOpen = true;
			var result = await dlg.ShowAsync();
			DialogOpen = false;
			if (result == ContentDialogResult.Primary)
				selectTempText("__rnd 1-7__ " + dlg.Chars);
			}

		private void selectTempText(string text)
		{
			texts.Current = new TextEntry("", text, false); ;
			textsOptionsEdit.IsEnabled = false;
			textsOptionsDelete.IsEnabled = false;
			reset();
		}

		private void FontSizeTb_GotFocus(object sender, RoutedEventArgs e)
		{
			fontSizeTb.SelectionStart = 0;
			fontSizeTb.SelectionLength = fontSizeTb.Text.Length;
		}

		private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
		{
			selectText(null);
		}

		//The RecordsBtn is used to disable typing in the typing session when Alt is pressed to display the acces keys.
		//Any other control with an access key could have been used for this puruose.
		private void RecordsBtn_AccessKeyDisplayRequested(UIElement sender, AccessKeyDisplayRequestedEventArgs args)
		{
			DialogOpen = true;
		}

		private void RecordsBtn_AccessKeyDisplayDismissed(UIElement sender, AccessKeyDisplayDismissedEventArgs args)
		{
			DialogOpen = false;
		}

		private void Page_GotFocus(object sender, RoutedEventArgs e)
		{
			if (clipboardChanged)
			{
				DataPackageView dataPackageView = Clipboard.GetContent();
				textCmPaste.IsEnabled = dataPackageView.Contains(StandardDataFormats.Text);
				clipboardChanged = false;
			}
		}

		private void RestoreFontBtn_Click(object sender, RoutedEventArgs e)
		{
			var defaultSession = new TypingSession();
			typingSession.FontName = defaultSession.FontName;
			typingSession.FontName = defaultSession.FontName;
			textBkgColorPicker.Color = defaultSession.Background;
			textColorPicker.Color = defaultSession.Foreground;
		}

		private void TimeText_GotFocus(object sender, RoutedEventArgs e)
		{
			//var speechString = typingSession.RemainingTime.ToSpeechString(false);
			//timeText.SetValue(AutomationProperties.NameProperty, "Time: " + speechString);
		}

        private void InvertColBtn_Click(object sender, RoutedEventArgs e)
        {
            var fore = textColorPicker.Color;
            textColorPicker.Color = textBkgColorPicker.Color;
            textBkgColorPicker.Color = fore;
        }

    }

    public static class TimeSpanExt
	{
		public static string ToSpeechString(this TimeSpan time, bool showSecondFractions)
		{
			if (time.TotalSeconds == 0)
				return "0 seconds";
			var minutes = time.Minutes;
			string timeText = "";
			if (minutes > 0)
				timeText = minutes + "minute" + (minutes == 1 ? "" : "s");
			var seconds = time.Seconds;
			if (showSecondFractions)
				timeText = time.ToString("s\\.ff") + "seconds";
			else if (seconds > 0)
				timeText += seconds + "second" + (seconds == 1 ? "" : "s");
			return timeText;
		}
	}
}
