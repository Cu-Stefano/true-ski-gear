using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PufferFish;

public class FakeInputBox : Form
{
	private IContainer components = null;

	private TableLayoutPanel tableLayoutPanel1;

	private ComboBox ctrl;

	private Label label;

	private Button button1;

	private Button button2;

	private Button button3;

	public FakeInputBox()
	{
		InitializeComponent();
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
		this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
		this.ctrl = new System.Windows.Forms.ComboBox();
		this.label = new System.Windows.Forms.Label();
		this.button1 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.button3 = new System.Windows.Forms.Button();
		this.tableLayoutPanel1.SuspendLayout();
		base.SuspendLayout();
		this.tableLayoutPanel1.ColumnCount = 3;
		this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
		this.tableLayoutPanel1.Controls.Add(this.ctrl, 0, 1);
		this.tableLayoutPanel1.Controls.Add(this.label, 0, 0);
		this.tableLayoutPanel1.Controls.Add(this.button1, 0, 2);
		this.tableLayoutPanel1.Controls.Add(this.button2, 1, 2);
		this.tableLayoutPanel1.Controls.Add(this.button3, 2, 2);
		this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
		this.tableLayoutPanel1.Name = "tableLayoutPanel1";
		this.tableLayoutPanel1.RowCount = 3;
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25f));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.tableLayoutPanel1.Size = new System.Drawing.Size(426, 166);
		this.tableLayoutPanel1.TabIndex = 0;
		this.tableLayoutPanel1.SetColumnSpan(this.ctrl, 3);
		this.ctrl.Dock = System.Windows.Forms.DockStyle.Fill;
		this.ctrl.FormattingEnabled = true;
		this.ctrl.Location = new System.Drawing.Point(3, 44);
		this.ctrl.Name = "ctrl";
		this.ctrl.Size = new System.Drawing.Size(420, 32);
		this.ctrl.TabIndex = 0;
		this.label.AutoSize = true;
		this.tableLayoutPanel1.SetColumnSpan(this.label, 3);
		this.label.Dock = System.Windows.Forms.DockStyle.Fill;
		this.label.Location = new System.Drawing.Point(3, 0);
		this.label.Name = "label";
		this.label.Size = new System.Drawing.Size(420, 41);
		this.label.TabIndex = 1;
		this.label.Text = "label1";
		this.label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.button1.Location = new System.Drawing.Point(3, 85);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(136, 78);
		this.button1.TabIndex = 2;
		this.button1.Text = "button1";
		this.button1.UseVisualStyleBackColor = true;
		this.button2.Dock = System.Windows.Forms.DockStyle.Fill;
		this.button2.Location = new System.Drawing.Point(145, 85);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(136, 78);
		this.button2.TabIndex = 3;
		this.button2.Text = "button2";
		this.button2.UseVisualStyleBackColor = true;
		this.button3.Dock = System.Windows.Forms.DockStyle.Fill;
		this.button3.Location = new System.Drawing.Point(287, 85);
		this.button3.Name = "button3";
		this.button3.Size = new System.Drawing.Size(136, 78);
		this.button3.TabIndex = 4;
		this.button3.Text = "button3";
		this.button3.UseVisualStyleBackColor = true;
		base.AutoScaleDimensions = new System.Drawing.SizeF(11f, 24f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(426, 166);
		base.Controls.Add(this.tableLayoutPanel1);
		base.Name = "FakeInputBox";
		this.Text = "FakeInputBox";
		this.tableLayoutPanel1.ResumeLayout(false);
		this.tableLayoutPanel1.PerformLayout();
		base.ResumeLayout(false);
	}
}
