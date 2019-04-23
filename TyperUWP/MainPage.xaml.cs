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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TyperUWP
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		Text text;
		TextList textList = new TextList();
		public MainPage()
		{
			this.InitializeComponent();
			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(1000, 300));
			//ApplicationView.PreferredLaunchViewSize = new Size(1000, );
			//ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
			Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
			Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
			text = new Text(textPanel, writtenTextPanel, currentCharControl, unwrittenTextControl);
			text.TimeChecked += Text_TimeChecked;
			//text.TheText = "abcdefghijklmnopqrstuvwxyzåäö";
			text.loadText();
			text.draw();
			updateTypingStats();
			foreach (var text in textList)
			{
				textsCombo.Items.Add(text.Title);
			}
			textsCombo.SelectedIndex = 0;
			//Clipboard.ContentChanged += async (s, e) =>
			//{
			//};
		}

		private async void Text_TimeChecked(object sender, EventArgs e)
		{
			await Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate
			{
				timeText.Text = "Time\n" + text.RemainingTimeString;
				wpmText.Text = "WPM\n" + text.Wpm;
				accuracyText.Text = "Accuracy\n" + text.Accuracy.ToString("0.00") + " %";
			});
		}

		bool isKeyPressed(VirtualKey key)
		{
			var state = CoreWindow.GetForCurrentThread().GetKeyState(key);
			return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
		}

		private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			if (isKeyPressed(VirtualKey.Control))
			{
				if (args.VirtualKey == VirtualKey.R)
					reset();
				else if (args.VirtualKey == VirtualKey.V)
					pasteText();
			}
		}

		async void pasteText()
		{
			DataPackageView dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				text.TheText = await dataPackageView.GetTextAsync();
				reset();
			}
		}
	
		private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
		{
			if (isKeyPressed(VirtualKey.Control))
				return;
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
			reset();
		}

		void reset()
		{
			text.reset();
			updateTypingStats();
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private void PasteBtn_Click(object sender, RoutedEventArgs e)
		{
			pasteText();
		}

		async private void NewTextBtn_Click(object sender, RoutedEventArgs e)
		{
			NewTextDialog newTextDialog = new NewTextDialog(textList);
			await newTextDialog.ShowAsync();
			textsCombo.Items.Add(newTextDialog.TitleEntry);
			textsCombo.SelectedIndex = textsCombo.Items.Count - 1;
		}

		private void TextsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			text.TheText = textList.getText((string)textsCombo.SelectedItem);
			reset();
		}
	}
}
