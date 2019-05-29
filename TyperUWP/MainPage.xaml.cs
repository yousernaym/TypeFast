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
using Windows.Storage.Pickers;
using System.Runtime.Serialization;
using Windows.ApplicationModel;
using System.Threading.Tasks;

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
		TypingSessionView typingSessionView;
		TypingSession typingSession
		{
			get => typingSessionView.Session;
			set => typingSessionView.Session = value;
		}
		Texts texts;
		bool dialogOpen = false;

		public MainPage()
		{
			this.InitializeComponent();
			//var importIcon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uEA52" };
			//textsOptionsImport.Icon = importIcon;
			//var exportIcon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uEDE2" };
			//textsOptionsExport.Icon = exportIcon;

			//ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(1000, 500));
			
			//ApplicationView.PreferredLaunchViewSize = new Size(1000, );
			//ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

			//ApplicationData.Current.DataChanged += Current_DataChanged;
			//ApplicationData.Current.DataChanged += new TypedEventHandler<ApplicationData, object>(DataChangeHandler);

			Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
			Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
			Application.Current.Suspending += Current_Suspending;
			texts = new Texts(LocalDataDir, ()=>loadPresets());
			SettingsPath = Path.Combine(RoamingDataDir, "settings");

			string[] fonts = CanvasTextFormat.GetSystemFontFamilies();
			foreach (string font in fonts)
				fontCombo.Items.Add(font);

			typingSessionView = new TypingSessionView(textPanel, writtenTextPanel, currentCharControl, unwrittenTextControl, new TypingSession());

			//saveSettings();
			loadSettings();
		}

		async Task<Stream> getResourceStream(string path)
		{
			var uri = new Uri("ms-appx:///"+path);
			var sampleFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
			return await sampleFile.OpenStreamForReadAsync();
		}

		private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
		{
			saveSettings();
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

		async private void loadSettings()
		{
			TypingSession session;
			try
			{
				using (var stream = File.Open(SettingsPath, FileMode.Open))
				{
					var dcs = new DataContractSerializer(typeof(TypingSession), TypingSession.SerializeTypes);
					session = (TypingSession)dcs.ReadObject(stream);
					typingSession = session;
				}
			}
			catch (FileNotFoundException)
			{
				//It''s probably the first time the app is opened, meaning no settings file has been created yet.
				//Use default settings
				textColorBtn.Background = typingSession.ForegroundBrush;
				textBkgColorBtn.Background = typingSession.BackgroundBrush;
				typingSession.FontSize = 50;
			}

			//Load bible
			var bibleStream = await getResourceStream("texts/bible_EN.xml");
			typingSession.Bible = new Bible(bibleStream);
			bibleStream.Dispose();

			fontCombo.SelectedItem = typingSession.FontName;
			fontSizeTb.Text = typingSession.FontSize.ToString();
			typingSession.TimeChecked += Text_TimeChecked;
			typingSession.Finished += Text_Finished;
			if (texts.containsTitle(typingSession.StartText))
				selectText(typingSession.StartText); //Text from last time app was used
			else
				selectText(null); //Random text
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
			if (isKeyDown(VirtualKey.Control))
			{
				if (args.VirtualKey == VirtualKey.R)
					clickResetBtn();
				else if (args.VirtualKey == VirtualKey.T)
					textsCombo.Focus(FocusState.Programmatic);
			}
		}

		private void Text_Finished(object sender, EventArgs e)
		{
			if (texts.Current != null)
				 texts.addRecord(typingSession);
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
			if (args.KeyCode == 27) //Esc key should remove focus from the AutoSuggestBox
				focusOnTyping();
			if (dialogOpen || isKeyDown(VirtualKey.Control) || isKeyDown(VirtualKey.Menu) || args.KeyCode == 13)
				return;

			//Ignore spaces if in the beginning of a text
			if (!typingSession.IsRunning && args.KeyCode == (uint)' ')
				return;

			//args.Handled = true; //needed?
			if (!typingSession.typeChar(args.KeyCode))
				return;
			focusOnTyping();
			typingSessionView.draw();
			updateTypingStats();
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
			if (typingSession.Shuffle)
			{
				selectText(null);
			}
			else
			{
				typingSession.TextEntry = texts.Current;
				reset();
			}
		}

		void reset()
		{
			if (dialogOpen)
				return;

			typingSessionView.reset();
			updateTypingStats();
			//focusOnTyping();
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			currentCharControl.Focus(FocusState.Programmatic);
			textsCombo.ItemSource = texts.Titles;
		}

		async private void TextsOptionsNew_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			NewTextDialog newTextDialog = new NewTextDialog(texts, false, textsCombo);
			newTextDialog.Title = "Add new text";
			newTextDialog.TitleEntry = typingSession.TextEntry.Title;
			newTextDialog.TextEntry = typingSession.TextEntry.Text;
			dialogOpen = true;
			ContentDialogResult result = await newTextDialog.ShowAsync();
			dialogOpen = false;

			if (result == ContentDialogResult.Primary)
				selectText(newTextDialog.TitleEntry);
		}

		async private void TextsOptionsEdit_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			NewTextDialog newTextDialog = new NewTextDialog(texts, true, textsCombo);
			newTextDialog.Title = "Edit text";
			newTextDialog.TitleEntry = texts.Current.Title;
			newTextDialog.TextEntry = texts.Current.Text;
			dialogOpen = true;
			ContentDialogResult result = await newTextDialog.ShowAsync();
			dialogOpen = false;

			if (result == ContentDialogResult.Primary)
				selectText(newTextDialog.TitleEntry);
		}

		async private void TextsOptionsDelete_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			var dlg = new ContentDialog { PrimaryButtonText = "Yes", CloseButtonText = "No", Content = "Are you sure you want to permanently delete this text and all its associated records?" };
			ContentDialogResult result = await dlg.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				texts.removeCurrent();
				textsCombo.ItemSource = texts.Titles;
				selectText(texts.Current?.Title);
			}
		}

		async private void TextCmPaste_Click(object sender, RoutedEventArgs e)
		{
			if (dialogOpen)
				return;
			DataPackageView dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				typingSession.TextEntry = new TextEntry("", await dataPackageView.GetTextAsync());
				reset();
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
			if (typingSession.TimeLimit.TotalSeconds < 30)
			{
				timeLimitTb.Text = "00:30";
				typingSession.TimeLimit = TimeSpan.FromSeconds(30);
			}
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
			typingSession.TextEntry = texts.Current;
			textsCombo.resetFilter();
			textsCombo.SelectedItem = texts.Current?.Title;
			reset();
		}

		void focusOnTyping()
		{
			dialogOpen = false;
			currentCharControl.Focus(FocusState.Programmatic);
		}

		async private void TextsOptionsExport_Click(object sender, RoutedEventArgs e)
		{
			var fsp = new FileSavePicker();
			fsp.FileTypeChoices.Add("Typer Texts", new List<string>() { ".tts" });
			fsp.SuggestedFileName = "Typer Texts";
			StorageFile file = await fsp.PickSaveFileAsync();
			if (file != null)
			{
				var stream = await file.OpenStreamForWriteAsync();
				stream.SetLength(0);
				texts.saveUserTexts(stream);
				//Todo: save complete user data (texts + records) before releasing app (or not?)
				//texts.saveUserData(stream); 
				stream.Dispose();
			}
		}

		async private void TextsOptionsImport_Click(object sender, RoutedEventArgs e)
		{
			var fop = new FileOpenPicker();
			//fop.FileTypeFilter.Add("*");
			fop.FileTypeFilter.Add(".tts");
			StorageFile file = await fop.PickSingleFileAsync();
			if (file != null)
			{
				var stream = await file.OpenStreamForReadAsync();
				try
				{
					texts.importUserData(stream, false);
				}
				catch
				{
					var dlg = await new ContentDialog { PrimaryButtonText = "Ok", Content = "Incorrect file format." }.ShowAsync();
					return;
				}
				finally
				{
					stream.Dispose();
				}
				textsCombo.ItemSource = texts.Titles;
				selectText(texts.Current?.Title);
			}
		}

		private void TextsOptionsRestore_Click(object sender, RoutedEventArgs e)
		{
			loadPresets();
		}

		async void loadPresets()
		{
			var stream = await getResourceStream("texts/presets.tts");
			texts.importUserData(stream, false);
			stream.Dispose();
			textsCombo.ItemSource = texts.Titles;
			selectText(texts.Current?.Title);
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
			dialogOpen = true;
		}

		private void TextsCombo_LostFocus(object sender, RoutedEventArgs e)
		{
			dialogOpen = false;
		}
	}
}
