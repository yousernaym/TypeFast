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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TyperUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
		Text text;
       	public MainPage()
		{
			this.InitializeComponent();
			text = new Text(textPanel, writtenTextPanel, currentCharControl, unwrittenTextControl);
			Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
			Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

			text.TheText = "arsiteanrsotieanrsotiearnstoiaersntoiEn  missa text";
			text.draw();
		}

		bool isKeyPressed(VirtualKey key)
		{
			var state = CoreWindow.GetForCurrentThread().GetKeyState(key);
			return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
		}
		private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
		{
			if (isKeyPressed(VirtualKey.Control) && args.VirtualKey == VirtualKey.R)
				text.reset();
		}

		private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
		{
			unwrittenTextControl.Focus(FocusState.Pointer);

			char c = (char)args.KeyCode;
			if ((int)c == 13 || isKeyPressed(VirtualKey.Control))
				return;
		
			text.typeChar(c);
			text.draw();
		}

		private void ResetBtn_Click(object sender, RoutedEventArgs e)
		{
			text.reset();
		}
	}
}
