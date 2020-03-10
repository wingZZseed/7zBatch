using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _7zBatch
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{

		private const string CMD = "7z.exe";

		private const string CMD_ARGUMENTS_DEFAULT = "a -y -r -mx9";

		private const string CMD_ARGUMENTS = " \"{0}\" \"{1}\"";

		private const string CMD_ARGUMENTS_SOLID = " -ms=off";

		private const string CMD_ARGUMENTS_TIMESTAMP = " -mtm=off";

		private const string CMD_ARGUMENTS_ATTR = " -mtr=off";

		private readonly Dictionary<string, string> method = new Dictionary<string, string>() {
			{ " -m0=LZMA", "LZMA" },
			{ "", "LZMA2" }
		};

		private string _src;

		private string _target;

		private Process _process;

		private volatile bool _isExit;

		public MainWindow()
		{
			InitializeComponent();
			BindMethodDataSource();
			RefreshCmd();
		}

		/// <summary>
		/// 绑定压缩方法数据源
		/// </summary>
		private void BindMethodDataSource()
		{
			Cmb_Method.ItemsSource = method;
			Cmb_Method.SelectedValuePath = "Key";
			Cmb_Method.DisplayMemberPath = "Value";
			Cmb_Method.SelectedIndex = 0;
		}

		/// <summary>
		/// 压缩
		/// </summary>
		/// <param name="cmd_arguments"></param>
		private void Comproess(string cmd_arguments)
		{
			_process = new Process();
			_process.StartInfo.CreateNoWindow = true;
			_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			_process.StartInfo.FileName = CMD;
			_process.StartInfo.Arguments = cmd_arguments;
			_process.EnableRaisingEvents = true;
			_process.Start();
			_process.WaitForExit();
			_process.Dispose();
		}

		/// <summary>
		/// 开始压缩
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Btn_Compress_Click(object sender, RoutedEventArgs e)
		{
			if (!File.Exists(CMD))
			{
				MessageBox.Show("7z.exe not found.");
				return;
			}

			if (string.IsNullOrWhiteSpace(_src))
				return;

			// 如果未设置目标文件夹，则默认为源文件夹同目录
			if (string.IsNullOrWhiteSpace(_target))
				_target = _src;

			var cmdArguments = Txt_Cmd.Text + CMD_ARGUMENTS;
			var isRemoveOnCompleted = Chk_Remove.IsChecked.GetValueOrDefault(false);

			// 开启新线程
			Task.Factory.StartNew(() =>
			{
				_isExit = false;

				UI_OnStart();

				DirectoryInfo di = new DirectoryInfo(_src);

				var dirs = di.GetDirectories();
				var files = di.GetFiles();
				var size = files.Length + dirs.Length;

				UI_ProgressBar_Set(0, size);

				int i = 0;

				if (dirs.Length > 0)
				{
					foreach (var d in dirs)
					{
						if (_isExit)
							break;

						string archive = _target + "\\" + d.Name + ".7z";
						string fs = d.FullName + "\\*";

						Comproess(string.Format(cmdArguments, archive, fs));

						if (isRemoveOnCompleted)
							Directory.Delete(d.FullName, true);

						UI_ProgressBar_Set(++i);
					}
				}

				if (files.Length > 0)
				{
					foreach (FileInfo fi in files)
					{
						if (_isExit)
							break;

						string archive = _target + "\\" + System.IO.Path.GetFileNameWithoutExtension(fi.Name) + ".7z";

						Comproess(string.Format(cmdArguments, archive, fi.FullName));

						if (isRemoveOnCompleted)
							File.Delete(fi.FullName);

						UI_ProgressBar_Set(++i);
					}
				}

				_process = null;

				UI_OnStop(_isExit);

				_isExit = false;
			});
		}

		/// <summary>
		/// 停止压缩
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Btn_Stop_Click(object sender, RoutedEventArgs e)
		{
			_isExit = true;

			try
			{
				if (_process != null && !_process.HasExited)
				{
					_process.Kill();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void RefreshCmd()
		{
			var cmd = CMD_ARGUMENTS_DEFAULT;

			cmd += Cmb_Method.SelectedValue;

			if (!Chk_Soild.IsChecked.GetValueOrDefault(false))
				cmd += CMD_ARGUMENTS_SOLID;
			if (!Chk_Attr.IsChecked.GetValueOrDefault(false))
				cmd += CMD_ARGUMENTS_ATTR;
			if (!Chk_Timestamp.IsChecked.GetValueOrDefault(false))
				cmd += CMD_ARGUMENTS_TIMESTAMP;

			Txt_Cmd.Text = cmd;
		}

		/// <summary>
		/// 浏览源文件夹
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Btn_Browse_Src_Click(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true };
			if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
				return;
			Txt_Src.Text = _src = dialog.FileName;
		}

		/// <summary>
		/// 浏览目标文件夹
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Btn_Browse_Target_Click(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true };
			if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
				return;
			Txt_Target.Text = _target = dialog.FileName;
		}

		private void Chk_Remove_Checked(object sender, RoutedEventArgs e)
		{
			// donothing
		}

		private void Chk_Soild_Checked(object sender, RoutedEventArgs e)
		{
			RefreshCmd();
		}

		private void Chk_Attr_Checked(object sender, RoutedEventArgs e)
		{
			RefreshCmd();
		}

		private void Chk_Timestamp_Checked(object sender, RoutedEventArgs e)
		{
			RefreshCmd();
		}

		private void Cmb_Method_Changed(object sender, RoutedEventArgs e)
		{
			RefreshCmd();
		}

		private void UI_OnStart()
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				Btn_Compress.IsEnabled = false;
			}));
		}

		private void UI_OnStop(bool isExit)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				if (isExit)
					MessageBox.Show("Stop!");
				else
					MessageBox.Show("Compress Completed!");

				Btn_Compress.IsEnabled = true;
				ProgressBar.Value = 0;
			}));
		}

		private void UI_ProgressBar_Set(int value)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				ProgressBar.Value = value;
			}));
		}

		private void UI_ProgressBar_Set(int value, int maximum)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				ProgressBar.Maximum = maximum;
				ProgressBar.Value = value;
			}));
		}

	}
}
