using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using PufferFish.Properties;

namespace PufferFish;

public class MagCalBox : Form
{
	private IContainer components = null;

	private PictureBox pictureBox1;

	private Button button1;

	private TextBox textBox1;

	public MagCalBox()
	{
		InitializeComponent();
	}

	private void button1_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void textBox1_TextChanged(object sender, EventArgs e)
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
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		this.button1 = new System.Windows.Forms.Button();
		this.textBox1 = new System.Windows.Forms.TextBox();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		base.SuspendLayout();
		this.pictureBox1.Image = PufferFish.Properties.Resources.Screenshot_2;
		this.pictureBox1.Location = new System.Drawing.Point(12, 12);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(259, 247);
		this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
		this.pictureBox1.TabIndex = 0;
		this.pictureBox1.TabStop = false;
		this.button1.Location = new System.Drawing.Point(516, 281);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(148, 59);
		this.button1.TabIndex = 1;
		this.button1.Text = "Ok";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(button1_Click);
		this.textBox1.Location = new System.Drawing.Point(298, 12);
		this.textBox1.Multiline = true;
		this.textBox1.Name = "textBox1";
		this.textBox1.Size = new System.Drawing.Size(366, 247);
		this.textBox1.TabIndex = 2;
		this.textBox1.Text = "Move and tilt the board as in the left picture until the green led turns on for 3 seconds. \r\nTo terminate abruptly the calibration procedure, just press one time the board button.";
		this.textBox1.TextChanged += new System.EventHandler(textBox1_TextChanged);
		base.AutoScaleDimensions = new System.Drawing.SizeF(11f, 24f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(690, 365);
		base.Controls.Add(this.textBox1);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.pictureBox1);
		base.Name = "MagCalBox";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Magnetometer calibration";
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
