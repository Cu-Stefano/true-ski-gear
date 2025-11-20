using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PufferFish;

public class WaitFormDelete : Form
{
	private IContainer components = null;

	private Label label1;

	private ProgressBar progressBar1;

	private Label label2;

	private Button button1;

	private TableLayoutPanel tableLayoutPanel1;

	public WaitFormDelete()
	{
		InitializeComponent();
	}

	private void button1_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
	{
	}

	private void WaitFormDelete_Load(object sender, EventArgs e)
	{
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PufferFish.WaitFormDelete));
		this.label1 = new System.Windows.Forms.Label();
		this.progressBar1 = new System.Windows.Forms.ProgressBar();
		this.label2 = new System.Windows.Forms.Label();
		this.button1 = new System.Windows.Forms.Button();
		this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
		this.tableLayoutPanel1.SuspendLayout();
		base.SuspendLayout();
		this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(6, 35);
		this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(185, 25);
		this.label1.TabIndex = 0;
		this.label1.Text = "Erasing memory...";
		this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.progressBar1.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.progressBar1.Location = new System.Drawing.Point(6, 198);
		this.progressBar1.Margin = new System.Windows.Forms.Padding(6);
		this.progressBar1.Name = "progressBar1";
		this.progressBar1.Size = new System.Drawing.Size(598, 38);
		this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
		this.progressBar1.TabIndex = 1;
		this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(6, 131);
		this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(371, 25);
		this.label2.TabIndex = 2;
		this.label2.Text = "The operation can take a few minutes";
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Top;
		this.button1.Location = new System.Drawing.Point(230, 248);
		this.button1.Margin = new System.Windows.Forms.Padding(6);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(150, 59);
		this.button1.TabIndex = 3;
		this.button1.Text = "Cancel";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(button1_Click);
		this.tableLayoutPanel1.AutoSize = true;
		this.tableLayoutPanel1.ColumnCount = 1;
		this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
		this.tableLayoutPanel1.Controls.Add(this.button1, 0, 3);
		this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 2);
		this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
		this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
		this.tableLayoutPanel1.Name = "tableLayoutPanel1";
		this.tableLayoutPanel1.RowCount = 4;
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70f));
		this.tableLayoutPanel1.Size = new System.Drawing.Size(610, 313);
		this.tableLayoutPanel1.TabIndex = 4;
		this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(tableLayoutPanel1_Paint);
		base.AutoScaleDimensions = new System.Drawing.SizeF(12f, 25f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.AutoSize = true;
		base.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		base.ClientSize = new System.Drawing.Size(634, 337);
		base.ControlBox = false;
		base.Controls.Add(this.tableLayoutPanel1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Margin = new System.Windows.Forms.Padding(6);
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "WaitFormDelete";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Please Wait...";
		base.Load += new System.EventHandler(WaitFormDelete_Load);
		this.tableLayoutPanel1.ResumeLayout(false);
		this.tableLayoutPanel1.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
