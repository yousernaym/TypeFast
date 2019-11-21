using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TyperUWP
{
	public sealed partial class InfoPanelInfo : UserControl
	{
		public Brush ForeGround
		{
			get => label.Foreground;
			set => label.Foreground = this.value.Foreground = value;
		}
		public string Label
		{
			get => label.Text;
			set => label.Text = value;
		}
		public string Value
		{
			get => value.Text;
			set => this.value.Text = value;
		}

		new public Thickness Margin
		{
			get => panel.Margin;
			set => panel.Margin = value;
		}
		
		public InfoPanelInfo()
		{
			this.InitializeComponent();
		}
	}
}
