using System;
using System.Drawing;
using System.Windows.Forms;

namespace MsgBox;

public static class InputBox
{
	public enum Icon
	{
		Error,
		Exclamation,
		Information,
		Question,
		Nothing
	}

	public enum Type
	{
		ComboBox,
		TextBox,
		Nothing
	}

	public enum Buttons
	{
		Ok,
		OkCancel,
		YesNo,
		YesNoCancel
	}

	public enum Language
	{
		Czech,
		English,
		German,
		Slovakian,
		Spanish,
		Italian
	}

	private static Form frm = new Form();

	public static string ResultValue;

	private static DialogResult DialogRes;

	private static string[] buttonTextArray = new string[4];

	public static DialogResult ShowDialog(string Message, string Title = "", Icon icon = Icon.Information, Buttons buttons = Buttons.Ok, Type type = Type.Nothing, string[] ListItems = null, bool ShowInTaskBar = false, Font FormFont = null)
	{
		frm.Controls.Clear();
		ResultValue = "";
		TableLayoutPanel tableLayoutPanel1 = new TableLayoutPanel();
		ComboBox ctrl = new ComboBox();
		Label label = new Label();
		Button button1 = new Button();
		Button button2 = new Button();
		Button button3 = new Button();
		tableLayoutPanel1.SuspendLayout();
		frm.SuspendLayout();
		tableLayoutPanel1.ColumnCount = 3;
		tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
		tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
		tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
		tableLayoutPanel1.Controls.Add(ctrl, 0, 1);
		tableLayoutPanel1.Controls.Add(label, 0, 0);
		tableLayoutPanel1.Controls.Add(button1, 0, 2);
		tableLayoutPanel1.Controls.Add(button2, 1, 2);
		tableLayoutPanel1.Controls.Add(button3, 2, 2);
		tableLayoutPanel1.Dock = DockStyle.Fill;
		tableLayoutPanel1.Location = new Point(0, 0);
		tableLayoutPanel1.Name = "tableLayoutPanel1";
		tableLayoutPanel1.RowCount = 3;
		tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
		tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
		tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
		tableLayoutPanel1.Size = new Size(426, 166);
		tableLayoutPanel1.TabIndex = 0;
		tableLayoutPanel1.SetColumnSpan(ctrl, 3);
		ctrl.Dock = DockStyle.Fill;
		ctrl.FormattingEnabled = true;
		ctrl.Location = new Point(3, 44);
		ctrl.Name = "ctrl";
		ctrl.Size = new Size(420, 32);
		if (ListItems != null)
		{
			foreach (string item in ListItems)
			{
				if (item != null)
				{
					ctrl.Items.Add(item);
				}
			}
			if (ctrl.Items.Count > 0)
			{
				ctrl.SelectedIndex = 0;
			}
		}
		ctrl.TabIndex = 0;
		label.AutoSize = true;
		tableLayoutPanel1.SetColumnSpan(label, 3);
		label.Dock = DockStyle.Fill;
		label.Location = new Point(3, 0);
		label.Name = "label";
		label.Size = new Size(420, 41);
		label.TabIndex = 1;
		label.Text = Message;
		label.TextAlign = ContentAlignment.MiddleCenter;
		button1.Dock = DockStyle.Fill;
		button1.Location = new Point(3, 85);
		button1.Name = "button1";
		button1.Size = new Size(136, 78);
		button1.TabIndex = 2;
		button1.Text = "button1";
		button1.UseVisualStyleBackColor = true;
		button1.Click += button_Click;
		if (buttons == Buttons.Ok || buttons == Buttons.OkCancel)
		{
			button1.Text = "Ok";
		}
		else
		{
			button1.Text = "Yes";
		}
		button2.Dock = DockStyle.Fill;
		button2.Location = new Point(145, 85);
		button2.Name = "button2";
		button2.Size = new Size(136, 78);
		button2.TabIndex = 3;
		button2.Text = "button2";
		button2.UseVisualStyleBackColor = true;
		button2.Click += button_Click;
		if (buttons == Buttons.YesNo || buttons == Buttons.YesNoCancel)
		{
			button2.Text = "No";
		}
		else
		{
			button2.Visible = false;
		}
		button3.Dock = DockStyle.Fill;
		button3.Location = new Point(287, 85);
		button3.Name = "button3";
		button3.Size = new Size(136, 78);
		button3.TabIndex = 4;
		button3.Text = "button3";
		button3.UseVisualStyleBackColor = true;
		button3.Click += button_Click;
		if (buttons == Buttons.OkCancel || buttons == Buttons.YesNoCancel)
		{
			button3.Text = "Cancel";
		}
		else
		{
			button3.Visible = false;
		}
		frm.AutoScaleDimensions = new SizeF(11f, 24f);
		frm.AutoScaleMode = AutoScaleMode.Font;
		frm.ClientSize = new Size(426, 166);
		frm.MaximizeBox = false;
		frm.MinimizeBox = false;
		frm.FormBorderStyle = FormBorderStyle.FixedDialog;
		frm.Controls.Add(tableLayoutPanel1);
		frm.Name = "InputBox";
		frm.Text = Title;
		frm.ShowIcon = false;
		frm.ShowInTaskbar = ShowInTaskBar;
		tableLayoutPanel1.ResumeLayout(performLayout: false);
		tableLayoutPanel1.PerformLayout();
		frm.FormClosing += frm_FormClosing;
		frm.StartPosition = FormStartPosition.CenterParent;
		frm.ResumeLayout(performLayout: false);
		frm.ShowDialog();
		if (type != Type.Nothing)
		{
			if (DialogRes == DialogResult.OK || DialogRes == DialogResult.Yes)
			{
				ResultValue = ctrl.Text;
			}
			else
			{
				ResultValue = "";
			}
		}
		return DialogRes;
	}

	private static void button_Click(object sender, EventArgs e)
	{
		Button button = (Button)sender;
		switch (button.Text)
		{
		case "Yes":
			DialogRes = DialogResult.Yes;
			break;
		case "No":
			DialogRes = DialogResult.No;
			break;
		case "Cancel":
			DialogRes = DialogResult.Cancel;
			break;
		default:
			DialogRes = DialogResult.OK;
			break;
		}
		frm.Close();
	}

	private static void textBox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			DialogRes = DialogResult.OK;
			frm.Close();
		}
	}

	private static void frm_FormClosing(object sender, FormClosingEventArgs e)
	{
		_ = DialogRes;
		if (1 == 0)
		{
			DialogRes = DialogResult.None;
		}
	}

	private static Button[] Btns(Buttons button, Language lang = Language.English)
	{
		Button[] returnButtons = new Button[3];
		Button OkButton = new Button();
		Button StornoButton = new Button();
		Button AnoButton = new Button();
		Button NeButton = new Button();
		OkButton.Text = buttonTextArray[0];
		OkButton.Name = "OK";
		AnoButton.Text = buttonTextArray[1];
		AnoButton.Name = "Yes";
		NeButton.Text = buttonTextArray[2];
		NeButton.Name = "No";
		StornoButton.Text = buttonTextArray[3];
		StornoButton.Name = "Cancel";
		switch (button)
		{
		case Buttons.Ok:
			OkButton.Location = new Point(250, 101);
			returnButtons[0] = OkButton;
			break;
		case Buttons.OkCancel:
			OkButton.Location = new Point(170, 101);
			returnButtons[0] = OkButton;
			StornoButton.Location = new Point(250, 101);
			returnButtons[1] = StornoButton;
			break;
		case Buttons.YesNo:
			AnoButton.Location = new Point(170, 101);
			returnButtons[0] = AnoButton;
			NeButton.Location = new Point(250, 101);
			returnButtons[1] = NeButton;
			break;
		case Buttons.YesNoCancel:
			AnoButton.Location = new Point(90, 101);
			returnButtons[0] = AnoButton;
			NeButton.Location = new Point(170, 101);
			returnButtons[1] = NeButton;
			StornoButton.Location = new Point(250, 101);
			returnButtons[2] = StornoButton;
			break;
		}
		Button[] array = returnButtons;
		foreach (Button btn in array)
		{
			if (btn != null)
			{
				btn.Size = new Size(75, 23);
				btn.Click += button_Click;
			}
		}
		return returnButtons;
	}

	private static Control Cntrl(Type type, string[] ListItems)
	{
		Control returnControl = new Control();
		switch (type)
		{
		case Type.ComboBox:
		{
			ComboBox comboBox = new ComboBox();
			comboBox.Size = new Size(180, 22);
			comboBox.Location = new Point(90, 70);
			comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			comboBox.Name = "comboBox";
			if (ListItems != null)
			{
				foreach (string item in ListItems)
				{
					comboBox.Items.Add(item);
				}
				comboBox.SelectedIndex = 0;
			}
			return comboBox;
		}
		case Type.TextBox:
		{
			TextBox textBox = new TextBox();
			textBox.Size = new Size(180, 23);
			textBox.Location = new Point(90, 70);
			textBox.KeyDown += textBox_KeyDown;
			textBox.Name = "textBox";
			return textBox;
		}
		default:
			return new Control();
		}
	}

	public static void SetLanguage(Language lang)
	{
		switch (lang)
		{
		case Language.Italian:
			buttonTextArray = "OK,Si,No,cancella".Split(',');
			break;
		case Language.Czech:
			buttonTextArray = "OK,Ano,Ne,Storno".Split(',');
			break;
		case Language.German:
			buttonTextArray = "OK,Ja,Nein,Stornieren".Split(',');
			break;
		case Language.Spanish:
			buttonTextArray = "OK,S&iacute;,No,Cancelar".Split(',');
			break;
		case Language.Slovakian:
			buttonTextArray = "OK,&Aacute;no,Nie,Zru&scaron;it".Split(',');
			break;
		default:
			buttonTextArray = "OK,Yes,No,Cancel".Split(',');
			break;
		}
	}
}
