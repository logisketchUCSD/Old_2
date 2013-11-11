using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.Ink;

using Sketch;
using Featurefy;

using System.IO;


namespace Fragmenter
{
	/// <summary>
	/// DebugForm class. Creates a DebugForm that is used for debugging purposes.
	/// Can display the Strokes, their corners, highlight individual Strokes, and display
	/// graphical information pertaining to the Stroke's speed and curvature.
	/// </summary>
	public class DebugForm : System.Windows.Forms.Form
	{
		#region INTERNALS

		/// <summary>
		/// FeatureStrokes to show debug information for.
		/// </summary>
		private Featurefy.FeatureStroke[] fStrokes = null;
		
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// InkOverlay so that we can select the Strokes, apply transformations, etc.
		/// </summary>
		private Microsoft.Ink.InkOverlay oInk;

		/// <summary>
		/// Panel to draw the Strokes on. Holds the InkOverlay.
		/// </summary>
		private System.Windows.Forms.Panel panel;
		
		/// <summary>
		/// If the panel should be showing Strokes or Graphical information.
		/// </summary>
		private enum PanelState { Stroke, Graph };
		
		/// <summary>
		/// The current state that the drawing panel is in.
		/// </summary>
		private PanelState panelState;
		
		/// <summary>
		/// Previous scaling factor. Used in resizing the Ink and the Form.
		/// </summary>
		private float prevScale = 1.0f;
		
		/// <summary>
		/// New scaling factor. Used in resizing the Ink and the Form.
		/// </summary>
		private float newScale = 1.0f;
		
		/// <summary>
		/// Total scaling factor. Used in resizing the Ink and the Form.
		/// </summary>
		private float totalScale = 1.0f;

		/// <summary>
		/// Current scroll bar positions for the horizontal (x) and vertical (y) scroll bars.
		/// </summary>
		private System.Drawing.Point ScrollPos = new System.Drawing.Point(0,0);
		
		/// <summary>
		/// Radio button to set the drawing mode to show Strokes.
		/// </summary>
		private System.Windows.Forms.RadioButton strokeRBtn;
		
		/// <summary>
		/// Radio button to set the drawing mode to show graphical information.
		/// </summary>
		private System.Windows.Forms.RadioButton graphRBtn;
		
		/// <summary>
		/// Combo box listing all of the Strokes. 
		/// </summary>
		private System.Windows.Forms.ComboBox strokeBox;
		
		/// <summary>
		/// Part of the legend for the graphical information
		/// </summary>
		private System.Windows.Forms.Label label1;
		
		/// <summary>
		/// Part of the legend for the graphical information
		/// </summary>
		private System.Windows.Forms.Label label2;
		
		/// <summary>
		/// Part of the legend for the graphical information
		/// </summary>
		private System.Windows.Forms.Label label3;
		
		/// <summary>
		/// Part of the legend for the graphical information
		/// </summary>
		private System.Windows.Forms.Label label4;
		
		/// <summary>
		/// Part of the legend for the graphical information
		/// </summary>
		private System.Windows.Forms.Label label5;
		
		/// <summary>
		/// Part of the legend for the graphical information
		/// </summary>
		private System.Windows.Forms.Label label6;
		
		/// <summary>
		/// Horizontal scroll bar for the panel
		/// </summary>
		private System.Windows.Forms.HScrollBar hScrollBar;
		
		/// <summary>
		/// Vertical scroll bar for the panel
		/// </summary>
		private System.Windows.Forms.VScrollBar vScrollBar;
		
		/// <summary>
		/// Zoom out button. Zooms the ink out by a certain factor.
		/// </summary>
		private System.Windows.Forms.Button zoomOutBtn;
		
		/// <summary>
		/// Zoom in button. Zooms the ink in by a certain factor.
		/// </summary>
		private System.Windows.Forms.Button zoomInBtn;
		
		/// <summary>
		/// Checkbox to see if we should keep the ink resized to the panel width.
		/// </summary>
		private System.Windows.Forms.CheckBox panelWidthBox;

		/// <summary>
		/// Textbox specifying the current zoom factor.
		/// </summary>
		private System.Windows.Forms.TextBox zoomTextBox;
		private System.Windows.Forms.Button matlabBtn;

		/// <summary>
		/// Label for some of the zoom objects.
		/// </summary>
		private System.Windows.Forms.Label zoomLabel;
		
		#endregion

		#region CONSTRUCTOR

		/// <summary>
		/// Constructs a debug display window for an array of FeatureStrokes.
		/// </summary>
		/// <param name="fStrokes">FeatureStrokes to provide debug information for</param>
		/// <param name="filename">Filename to display in the title</param>
		public DebugForm(FeatureStroke[] fStrokes, string filename)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// Double-buffering code
			this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
			this.UpdateStyles();

			// Store the strokes
			this.fStrokes = fStrokes;

			// Set the title
			this.Text = "Debug Form: " + filename;

			// Initialize the InkOverlay
			this.oInk = new InkOverlay(panel);
			this.oInk.EditingMode = InkOverlayEditingMode.Select;
			this.oInk.SelectionChanged += new InkOverlaySelectionChangedEventHandler(oInk_SelectionChanged);
			this.oInk.AutoRedraw = false;
			
			// Setup the Ink in the InkOverlay
			for (int i = 0; i < fStrokes.Length; i++)
			{
				System.Drawing.Point[] pts = new System.Drawing.Point[fStrokes[i].Points.Length];
				for (int k = 0; k < pts.Length; k++)
				{
					pts[k] = new System.Drawing.Point( (int)(fStrokes[i].Points[k].X), 
						(int)(fStrokes[i].Points[k].Y) );
				}
			
				oInk.Ink.CreateStroke(pts);
			}
			
			// Move center the ink's origin to the top-left corner
			this.oInk.Ink.Strokes.Move(-oInk.Ink.GetBoundingBox().X, -oInk.Ink.GetBoundingBox().Y);
			this.oInk.Enabled = true;
			
			// Set the panel state to show strokes when the debug window starts
			panelState = PanelState.Stroke;
			
			// Add the stroke labels to the combo box
			ArrayList strokeBoxLabels = new ArrayList();
			strokeBoxLabels.Add("All strokes");
			for (int i = 0; i < fStrokes.Length; i++)
			{
				strokeBoxLabels.Add("Stroke " + i.ToString());
			}
				
			strokeBox.DataSource = strokeBoxLabels;
			strokeBox.SelectedIndex = 0;
			graphRBtn.Enabled = false;

			// Hide the graphical display key
			hideKey();

			// Resize the form
			FormResize();
		}


		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.panel = new System.Windows.Forms.Panel();
			this.strokeRBtn = new System.Windows.Forms.RadioButton();
			this.graphRBtn = new System.Windows.Forms.RadioButton();
			this.strokeBox = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.hScrollBar = new System.Windows.Forms.HScrollBar();
			this.vScrollBar = new System.Windows.Forms.VScrollBar();
			this.zoomOutBtn = new System.Windows.Forms.Button();
			this.zoomInBtn = new System.Windows.Forms.Button();
			this.panelWidthBox = new System.Windows.Forms.CheckBox();
			this.zoomTextBox = new System.Windows.Forms.TextBox();
			this.zoomLabel = new System.Windows.Forms.Label();
			this.matlabBtn = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// panel
			// 
			this.panel.BackColor = System.Drawing.SystemColors.Window;
			this.panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel.Location = new System.Drawing.Point(16, 48);
			this.panel.Name = "panel";
			this.panel.Size = new System.Drawing.Size(584, 312);
			this.panel.TabIndex = 0;
			this.panel.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_Paint);
			// 
			// strokeRBtn
			// 
			this.strokeRBtn.Checked = true;
			this.strokeRBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.strokeRBtn.ForeColor = System.Drawing.Color.Indigo;
			this.strokeRBtn.Location = new System.Drawing.Point(16, 400);
			this.strokeRBtn.Name = "strokeRBtn";
			this.strokeRBtn.TabIndex = 2;
			this.strokeRBtn.TabStop = true;
			this.strokeRBtn.Text = "Stroke Display";
			this.strokeRBtn.CheckedChanged += new System.EventHandler(this.strokeRBtn_CheckedChanged);
			// 
			// graphRBtn
			// 
			this.graphRBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.graphRBtn.ForeColor = System.Drawing.Color.DarkCyan;
			this.graphRBtn.Location = new System.Drawing.Point(128, 400);
			this.graphRBtn.Name = "graphRBtn";
			this.graphRBtn.TabIndex = 3;
			this.graphRBtn.Text = "Graphical Info";
			this.graphRBtn.CheckedChanged += new System.EventHandler(this.graphRBtn_CheckedChanged);
			// 
			// strokeBox
			// 
			this.strokeBox.Location = new System.Drawing.Point(440, 400);
			this.strokeBox.Name = "strokeBox";
			this.strokeBox.Size = new System.Drawing.Size(170, 21);
			this.strokeBox.TabIndex = 4;
			this.strokeBox.Text = "No Strokes Loaded";
			this.strokeBox.SelectedIndexChanged += new System.EventHandler(this.strokeBox_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.SystemColors.Control;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.ForeColor = System.Drawing.Color.Black;
			this.label1.Location = new System.Drawing.Point(264, 392);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 16);
			this.label1.TabIndex = 5;
			this.label1.Text = "Speed  - ";
			// 
			// label4
			// 
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label4.ForeColor = System.Drawing.Color.Orange;
			this.label4.Location = new System.Drawing.Point(336, 392);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(56, 16);
			this.label4.TabIndex = 6;
			this.label4.Text = "Orange";
			// 
			// label5
			// 
			this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label5.ForeColor = System.Drawing.Color.ForestGreen;
			this.label5.Location = new System.Drawing.Point(336, 408);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(48, 16);
			this.label5.TabIndex = 7;
			this.label5.Text = "Green";
			this.label5.Visible = false;
			// 
			// label2
			// 
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label2.Location = new System.Drawing.Point(264, 408);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 16);
			this.label2.TabIndex = 8;
			this.label2.Text = "Curvature  -";
			this.label2.Visible = false;
			// 
			// label3
			// 
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label3.Location = new System.Drawing.Point(264, 424);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(72, 16);
			this.label3.TabIndex = 9;
			this.label3.Text = "Corners  -";
			this.label3.Visible = false;
			// 
			// label6
			// 
			this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label6.ForeColor = System.Drawing.Color.Red;
			this.label6.Location = new System.Drawing.Point(336, 424);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(48, 16);
			this.label6.TabIndex = 10;
			this.label6.Text = "Red";
			this.label6.Visible = false;
			// 
			// hScrollBar
			// 
			this.hScrollBar.Location = new System.Drawing.Point(16, 360);
			this.hScrollBar.Name = "hScrollBar";
			this.hScrollBar.Size = new System.Drawing.Size(584, 17);
			this.hScrollBar.TabIndex = 11;
			this.hScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hScrollBar_Scroll);
			// 
			// vScrollBar
			// 
			this.vScrollBar.Location = new System.Drawing.Point(600, 48);
			this.vScrollBar.Name = "vScrollBar";
			this.vScrollBar.Size = new System.Drawing.Size(17, 312);
			this.vScrollBar.TabIndex = 12;
			this.vScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBar_Scroll);
			// 
			// zoomOutBtn
			// 
			this.zoomOutBtn.Enabled = false;
			this.zoomOutBtn.Font = new System.Drawing.Font("Arial Black", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.zoomOutBtn.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(192)), ((System.Byte)(0)), ((System.Byte)(0)));
			this.zoomOutBtn.Location = new System.Drawing.Point(592, 16);
			this.zoomOutBtn.Name = "zoomOutBtn";
			this.zoomOutBtn.Size = new System.Drawing.Size(24, 23);
			this.zoomOutBtn.TabIndex = 22;
			this.zoomOutBtn.Text = "-";
			this.zoomOutBtn.Click += new System.EventHandler(this.zoomOutBtn_Click);
			// 
			// zoomInBtn
			// 
			this.zoomInBtn.Enabled = false;
			this.zoomInBtn.Font = new System.Drawing.Font("Arial Black", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.zoomInBtn.ForeColor = System.Drawing.Color.Green;
			this.zoomInBtn.Location = new System.Drawing.Point(560, 16);
			this.zoomInBtn.Name = "zoomInBtn";
			this.zoomInBtn.Size = new System.Drawing.Size(24, 23);
			this.zoomInBtn.TabIndex = 21;
			this.zoomInBtn.Text = "+";
			this.zoomInBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.zoomInBtn.Click += new System.EventHandler(this.zoomInBtn_Click);
			// 
			// panelWidthBox
			// 
			this.panelWidthBox.Checked = true;
			this.panelWidthBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.panelWidthBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.panelWidthBox.Location = new System.Drawing.Point(326, 15);
			this.panelWidthBox.Name = "panelWidthBox";
			this.panelWidthBox.TabIndex = 20;
			this.panelWidthBox.Text = "Page Width";
			this.panelWidthBox.CheckedChanged += new System.EventHandler(this.panelWidthBox_CheckedChanged);
			// 
			// zoomTextBox
			// 
			this.zoomTextBox.Enabled = false;
			this.zoomTextBox.Location = new System.Drawing.Point(496, 16);
			this.zoomTextBox.Name = "zoomTextBox";
			this.zoomTextBox.Size = new System.Drawing.Size(55, 20);
			this.zoomTextBox.TabIndex = 18;
			this.zoomTextBox.Text = "1.0";
			this.zoomTextBox.TextChanged += new System.EventHandler(this.zoomTextBox_TextChanged);
			// 
			// zoomLabel
			// 
			this.zoomLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.zoomLabel.Location = new System.Drawing.Point(442, 14);
			this.zoomLabel.Name = "zoomLabel";
			this.zoomLabel.Size = new System.Drawing.Size(48, 24);
			this.zoomLabel.TabIndex = 19;
			this.zoomLabel.Text = "Zoom:";
			this.zoomLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// matlabBtn
			// 
			this.matlabBtn.Enabled = false;
			this.matlabBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.matlabBtn.Location = new System.Drawing.Point(14, 12);
			this.matlabBtn.Name = "matlabBtn";
			this.matlabBtn.Size = new System.Drawing.Size(128, 24);
			this.matlabBtn.TabIndex = 23;
			this.matlabBtn.Text = "MATLAB Output";
			this.matlabBtn.Click += new System.EventHandler(this.matlabBtn_Click);
			// 
			// DebugForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(632, 446);
			this.Controls.Add(this.matlabBtn);
			this.Controls.Add(this.zoomOutBtn);
			this.Controls.Add(this.zoomInBtn);
			this.Controls.Add(this.panelWidthBox);
			this.Controls.Add(this.zoomTextBox);
			this.Controls.Add(this.strokeRBtn);
			this.Controls.Add(this.zoomLabel);
			this.Controls.Add(this.vScrollBar);
			this.Controls.Add(this.hScrollBar);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.strokeBox);
			this.Controls.Add(this.graphRBtn);
			this.Controls.Add(this.panel);
			this.MinimumSize = new System.Drawing.Size(640, 480);
			this.Name = "DebugForm";
			this.Text = "Debug Display";
			this.Resize += new System.EventHandler(this.GraphForm_Resize);
			this.ResumeLayout(false);

		}

		#endregion		

		#region VARIOUS FORM OBJECTS

		/// <summary>
		/// Occurs whenever the form is resized.
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void GraphForm_Resize(object sender, System.EventArgs e)
		{
			FormResize();
		}


		/// <summary>
		/// Resizes the window such that all of the buttons and panels remain in their relative
		/// places, and the ink stretches with the window.
		/// </summary>
		private void FormResize()
		{
			// Moves all of the Form objects based on my calculated positions.
			// It's ugly, but it works.
			this.panel.Size = new Size(this.Width - 56, this.Height - 160);
			this.hScrollBar.Location = new System.Drawing.Point(panel.Location.X, panel.Location.Y + panel.Height);
			this.vScrollBar.Location = new System.Drawing.Point(panel.Location.X + panel.Width, this.panel.Location.Y);

			this.hScrollBar.Size = new Size(panel.Width, 17);
			this.vScrollBar.Size = new Size(17, panel.Height);

			this.strokeRBtn.Location = new System.Drawing.Point(16, this.Height - 80);
			this.graphRBtn.Location = new System.Drawing.Point(128, this.Height - 80);

			this.strokeBox.Location = new System.Drawing.Point(this.Width - 200, this.Height - 80);

			this.label1.Location = new System.Drawing.Point(this.Width / 2 - 56, this.Height - 88);
			this.label2.Location = new System.Drawing.Point(this.Width / 2 - 56, this.Height - 72);
			this.label3.Location = new System.Drawing.Point(this.Width / 2 - 56, this.Height - 56);
			this.label4.Location = new System.Drawing.Point(this.Width / 2 + 16, this.Height - 88);
			this.label5.Location = new System.Drawing.Point(this.Width / 2 + 16, this.Height - 72);
			this.label6.Location = new System.Drawing.Point(this.Width / 2 + 16, this.Height - 56);

			this.panelWidthBox.Location = new System.Drawing.Point(this.Width - 314, 15);
			this.zoomLabel.Location = new System.Drawing.Point(this.Width - 198, 14);
			this.zoomTextBox.Location = new System.Drawing.Point(this.Width - 144, 16);
			this.zoomInBtn.Location = new System.Drawing.Point(this.Width - 80, 16);
			this.zoomOutBtn.Location = new System.Drawing.Point(this.Width - 48, 16);
			
			// Actual stroke bounding box (in Ink Space)
			int strokeWidth  = oInk.Ink.Strokes.GetBoundingBox().Width;
			int strokeHeight = oInk.Ink.Strokes.GetBoundingBox().Height;
			
			int inkWidth  = panel.Width - 60;
			int inkHeight = panel.Height - 60;
		
			Graphics g = panel.CreateGraphics();

			if (strokeWidth != 0 && strokeHeight != 0)
			{
				// If we want to scale by the panel's width
				if (this.panelWidthBox.Checked == true)
				{
					// Convert the rendering space from Ink Space to Pixels
					System.Drawing.Point botRight = new System.Drawing.Point(strokeWidth, strokeHeight);
					oInk.Renderer.InkSpaceToPixel(g, ref botRight); 				
				
					System.Drawing.Point topLeft = new System.Drawing.Point(0, 0);
					oInk.Renderer.InkSpaceToPixel(g, ref topLeft); 				
				
					System.Drawing.Point scalePt = new System.Drawing.Point(botRight.X - topLeft.X, botRight.Y - topLeft.Y);
				
					// Scale the rendered strokes by the smallest (x or y) scaling factor
					float xScale = (float)inkWidth / (float)scalePt.X;
					float yScale = (float)inkHeight / (float)scalePt.Y;
		
					//float scale = xScale < yScale ? xScale : yScale;
					float scale = xScale;

					oInk.Renderer.Scale(scale, scale, false);
				
					// Update the scaling factors
					totalScale *= scale;
					prevScale = totalScale;

					// Update the user-displayed zoom
					zoomTextBox.Text = totalScale.ToString();
				}
				else
				{
					if (prevScale != 0.0f)
						oInk.Renderer.Scale(newScale / prevScale, newScale / prevScale, false);
					
					totalScale = prevScale = newScale;	
				}
			}

			// Re-map the scroll bars		
			System.Drawing.Point temp   = new System.Drawing.Point(strokeWidth, strokeHeight);
			System.Drawing.Point origin = new System.Drawing.Point(0, 0);
			oInk.Renderer.InkSpaceToPixel(g, ref temp);
			oInk.Renderer.InkSpaceToPixel(g, ref origin);
		
			this.hScrollBar.Maximum = Math.Max(0, (temp.X - origin.X) - inkWidth);
			this.vScrollBar.Maximum = Math.Max(0, (temp.Y - origin.Y) - inkHeight);

			this.ScrollPos.X = Math.Min(this.hScrollBar.Value, this.hScrollBar.Maximum);
			this.ScrollPos.Y = Math.Min(this.vScrollBar.Value, this.vScrollBar.Maximum);
			HandleScroll();

			// Paint the panel
			panel.Refresh();
		}

		
		/// <summary>
		/// Sets the selected index of the stroke box to be the (first) selected stroke's Id.
		/// This in turn updates what stroke is highlighted once the panel is refreshed.
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void oInk_SelectionChanged(object sender, System.EventArgs e)
		{
			if (oInk.Selection.Count > 0)
			{
				this.strokeBox.SelectedIndex = oInk.Selection[0].Id;
				panel.Refresh();
			}
		}

		
		/// <summary>
		/// Switches the panel's drawing state to be for a stroke display.
		/// The stroke display shows the stroke with the found corners to fragment.
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void strokeRBtn_CheckedChanged(object sender, System.EventArgs e)
		{
			if (strokeRBtn.Checked == true)
			{
				panelState = PanelState.Stroke;
			
				// Hide the labels (graph key)
				hideKey();

				oInk.AttachedControl = this.panel;
				oInk.Enabled = true;

				this.matlabBtn.Enabled = false;

				panel.Refresh();
			}
		}
		
		
		/// <summary>
		/// Switches the panel's drawing state to be for a graphical display.
		/// The graphical display shows the speed and curvature information.
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void graphRBtn_CheckedChanged(object sender, System.EventArgs e)
		{
			if (graphRBtn.Checked == true)
			{
				panelState = PanelState.Graph;
			
				// Show the labels (graph key)
				showKey();

				oInk.Enabled = false;
				oInk.AttachedControl = null;

				this.matlabBtn.Enabled = true;

				panel.Refresh();
			}
		}

		
		/// <summary>
		/// Updates the panel depending on what stroke is selected
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void strokeBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// If the index is 0, we want to show all of the Strokes
			if (this.strokeBox.SelectedIndex == 0)
			{
				strokeRBtn.Checked = true;
				graphRBtn.Enabled = false;
			}
			else
			{
				graphRBtn.Enabled = true;
			}

			panel.Refresh();
		}


		/// <summary>
		/// Hides the key (or labels) for the Graphical Display
		/// </summary>
		private void showKey()
		{
			label1.Show();
			label2.Show();
			label3.Show();
			label4.Show();
			label5.Show();
			label6.Show();
		}

		
		/// <summary>
		/// Shows the key (or labels) for the Graphical Display
		/// </summary>
		private void hideKey()
		{
			label1.Hide();
			label2.Hide();
			label3.Hide();
			label4.Hide();
			label5.Hide();
			label6.Hide();
		}
		
	
		/// <summary>
		/// Activates whenever the horizontal scroll bar is used.
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes a scroll object specific to the event that is being handled</param>
		private void hScrollBar_Scroll(object sender, System.Windows.Forms.ScrollEventArgs e)
		{
			this.ScrollPos.X = e.NewValue;
			HandleScroll();
			panel.Refresh();
		}


		/// <summary>
		/// Activates whenever the vertical scroll bar is used.
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes a scroll object specific to the event that is being handled</param>
		private void vScrollBar_Scroll(object sender, System.Windows.Forms.ScrollEventArgs e)
		{
			this.ScrollPos.Y = e.NewValue;
			HandleScroll();
			panel.Refresh();
		}


		/// <summary>
		/// Creates a new ViewTransform matrix that shifts the image based on the scrollbar positions.
		/// Converts the scrollbar pixels into Ink Space coordinates and then shifts the rendered ink
		/// by that amount.
		/// </summary>
		private void HandleScroll() 
		{
			// Convert to ink space.  Since we are updating the view 
			// transform of the renderer, the origin of the ink coordinates 
			// may have moved relative to the screen.   
			System.Drawing.Point scrollAmount = new System.Drawing.Point(-this.ScrollPos.X, -this.ScrollPos.Y);
			System.Drawing.Point origin = new System.Drawing.Point(0, 0);

			Graphics g = panel.CreateGraphics();
			oInk.Renderer.PixelToInkSpace(g, ref scrollAmount);
			oInk.Renderer.PixelToInkSpace(g, ref origin);
			
			// Create a translation transform based on the current x and y 
			// scroll positions
			System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
			m.Translate((scrollAmount.X - origin.X) * this.totalScale, (scrollAmount.Y - origin.Y) * this.totalScale);
			
			oInk.Renderer.SetViewTransform(m);
		}

		
		/// <summary>
		/// If the checkbox is checked, then update the ink's zoom based on the panel width. Otherwise
		/// we will update the zoom based on the value in the zoom text box.
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void panelWidthBox_CheckedChanged(object sender, System.EventArgs e)
		{
			if (panelWidthBox.Checked == false) 
			{
				this.zoomTextBox.Enabled = true;
				this.zoomInBtn.Enabled   = true;
				this.zoomOutBtn.Enabled  = true;
				getScaleFromBox();
			}
			else
			{
				this.zoomTextBox.Enabled = false;
				this.zoomInBtn.Enabled   = false;
				this.zoomOutBtn.Enabled  = false;
				FormResize();
			}
		}


		/// <summary>
		/// Updates the zoom factor of the image from the text entered into the zoom text box
		/// every time the text is changed.
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void zoomTextBox_TextChanged(object sender, System.EventArgs e)
		{
			getScaleFromBox();	
		}


		/// <summary>
		/// Zooms in by a factor of 1 each time you click the button
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void zoomInBtn_Click(object sender, System.EventArgs e)
		{
			if (newScale >= 1.0)
				newScale = (float)Math.Floor(newScale + 1.4);	
			else
				newScale = (float)(newScale * 2.0);
		
			this.zoomTextBox.Text = newScale.ToString();
			FormResize();
		}
		
		
		/// <summary>
		/// Zooms out by a factor of 1 each time you click the button.
		/// When the zoom scale is less than 1, it divides the zoom by half each time.
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void zoomOutBtn_Click(object sender, System.EventArgs e)
		{
			if (newScale >= 2.0) 
				newScale = (float)Math.Floor(newScale - 1.0);
			else
				newScale = (float)(newScale / 2.0);
		
			this.zoomTextBox.Text = newScale.ToString();
			FormResize();
		}

		
		/// <summary>
		/// Updates the zoom factor of the image from the text entered into the zoom text box.
		/// </summary>
		private void getScaleFromBox()
		{
			try
			{
				newScale = (float)Convert.ToDouble(zoomTextBox.Text);
				FormResize();
			}
			catch
			{
				// Do nothing...
			}
		}

		
		/// <summary>
		/// Writes a .txt file that contains information to build a MATLAB graph from.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void matlabBtn_Click(object sender, System.EventArgs e)
		{
			FileInfo f = new FileInfo("matlab_stroke" + (this.strokeBox.SelectedIndex - 1) + ".txt");
			StreamWriter Tex = f.CreateText();
			
			FeatureStroke fStroke = this.fStrokes[this.strokeBox.SelectedIndex - 1];
			Sketch.Point[] points = fStroke.Points;
				
			int length = points.Length;
			
			Tex.WriteLine("ARC LENGTH: ");
			Tex.WriteLine();
			Tex.Write("x = [");
			for (int i = 0; i < length; i++)
			{
				Tex.Write(fStroke.ArcLength.Profile[i].ToString("#00.00000"));

				if (i < length - 1)
					Tex.Write(",");
				else
					Tex.WriteLine("];");
			}
				
			
			Tex.WriteLine();
			Tex.WriteLine();
			Tex.WriteLine("CURVATURE: ");
			Tex.WriteLine();
			Tex.Write("y = [");
			for (int i = 0; i < length; i++)
			{
				Tex.Write(fStroke.Curvature.Profile[i].ToString("#00.00000"));
				
				if (i < length - 1)
					Tex.Write(",");
				else
					Tex.WriteLine("];");
			}


			Tex.WriteLine();
			Tex.WriteLine();
			Tex.WriteLine("SPEED: ");
			Tex.WriteLine();
			Tex.Write("y = [");
			for (int i = 0; i < length; i++)
			{
				Tex.Write(fStroke.Speed.NormProfile[i].ToString("#00.00000"));

				if (i < length - 1)
					Tex.Write(",");
				else
					Tex.WriteLine("];");
			}

			Tex.WriteLine();
			Tex.WriteLine();
			Tex.WriteLine("plot(x,y,'-b');");

			Tex.Close();
		}

		#endregion
		
		#region DRAWING CODE

		/// <summary>
		/// Paints the panel according to the panel's drawing state
		/// </summary>
		/// <param name="sender">Reference to the object that raised the event</param>
		/// <param name="e">Passes an object specific to the event that is being handled</param>
		private void panel_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			int index = this.strokeBox.SelectedIndex - 1;
			
			// Paint the stroke
			if (this.panelState == PanelState.Stroke)
			{
				PaintStrokes(index);
					
				HighlightCorners(panel.CreateGraphics(), index, Color.Red);
			}

			// Paint the speed and curvature information
			else if (this.panelState == PanelState.Graph)
			{
				PaintCorners(index);
				PaintSpeedGraph(index);
				PaintCurveGraph(index);
			}
		}

		
		/// <summary>
		/// Paints all of the strokes.
		/// </summary>
		private void PaintStrokes(int index)
		{
			Graphics g = panel.CreateGraphics();

			// If we should draw all the Strokes...
			if (index == -1)
			{
				Microsoft.Ink.DrawingAttributes da = new Microsoft.Ink.DrawingAttributes(new System.Drawing.Pen(Color.Black, 1.0f));
				da.FitToCurve = true;
				oInk.DefaultDrawingAttributes = da;
				oInk.Renderer.Draw(g, oInk.Ink.Strokes);
			}

			else
			{
				// Draw the non-selected strokes
				Microsoft.Ink.DrawingAttributes da = new Microsoft.Ink.DrawingAttributes(new System.Drawing.Pen(Color.Gray, 1.0f));
				da.FitToCurve = true;
				
				foreach (Microsoft.Ink.Stroke s in oInk.Ink.Strokes)
					oInk.Renderer.Draw(g, s, da);

				// Draw the selected stroke
				da = new Microsoft.Ink.DrawingAttributes(new System.Drawing.Pen(Color.Black, 2.0f));
				da.FitToCurve = true;

				oInk.Renderer.Draw(g, oInk.Ink.Strokes[index], da);
			}
		}


		/// <summary>
		/// Highlights the found corners of a given FeatureStroke.
		/// </summary>
		/// <param name="g">Graphics for the object</param>
		/// <param name="index">Index of the FeatureStroke to highlight the corners for</param>
		/// <param name="color">Color of the corners</param>
		private void HighlightCorners(Graphics g, int index, Color color) 
		{
			ArrayList ptsArray = new ArrayList();
			
			// If we should draw all the corners...
			if (index == -1)
			{
				int init  = 0;
				int final = 0;

				for (int i = 0; i < this.fStrokes.Length; i++)
				{
					int[] corners = new Corners(this.fStrokes[i]).FindCorners(ref init);
					Microsoft.Ink.Stroke stroke = oInk.Ink.Strokes[i];

					for (int k = 0; k < corners.Length; k++)
					{
						ptsArray.Add(stroke.GetPoint(corners[k]));
					}

					final += corners.Length;
				}

				//Console.WriteLine("init: " + init);
				//Console.WriteLine("final: " + final);
			}

			else
			{
				int[] corners = new Corners(this.fStrokes[index]).FindCorners();
				Microsoft.Ink.Stroke stroke = oInk.Ink.Strokes[index];

				for (int i = 0; i < corners.Length; i++)
				{
					ptsArray.Add(stroke.GetPoint(corners[i]));
				}
			}

			System.Drawing.Point[] pts = (System.Drawing.Point[])ptsArray.ToArray(typeof(System.Drawing.Point));

			Microsoft.Ink.Ink highPts = new Ink();
					
			// Create strokes consisting of one point each
			// This is done so that we can draw the points in our InkOverlay and correctly scale the points
			// accordingly.
			for (int i = 0; i < pts.Length; i++)
			{
				System.Drawing.Point[] p = new System.Drawing.Point[1];
				p[0] = pts[i];
				highPts.CreateStroke(p);
			}

			// Display features of the point (Color, Width, and Height)
			Microsoft.Ink.DrawingAttributes da = new Microsoft.Ink.DrawingAttributes(color);
			da.Height = 125;
			da.Width  = 125;

			// Render each point
			foreach (Microsoft.Ink.Stroke s in highPts.Strokes)
			{
				oInk.Renderer.Draw(g, s, da);
			}
		}

		
		/// <summary>
		/// Draws a graphical display of the speed information of a stroke.
		/// </summary>
		/// <param name="index">Stroke index to draw the speed for</param>
		private void PaintSpeedGraph(int index)
		{
			FeatureStroke s = this.fStrokes[index];

			if (s != null)
			{
				double arcLength = s.ArcLength.TotalLength;
				double[] speed   = s.Speed.NormProfile;
				
				double xaxis = (double)panel.Width / (double)speed.Length;
				double yaxis = (double)panel.Height / (s.Speed.MaximumSpeed / s.Speed.AverageSpeed);

				Graphics g = panel.CreateGraphics();
				System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();

				// Draw a graphics path (graph) or the normalized speed
				for (int i = 1; i < speed.Length; i++)
				{
					gp.AddLine((float)((i-1) * xaxis), (float)(panel.Height - (speed[i-1] * yaxis)), 
						(float)(i * xaxis), (float)(panel.Height - (speed[i] * yaxis)));
				}
			
				g.DrawPath(new System.Drawing.Pen(Color.Orange, 1.0f), gp);
			}
		}


		/// <summary>
		/// Draws a graphical display of the curvature information of a stroke.
		/// </summary>
		/// <param name="index">Stroke index to draw the speed for</param>
		private void PaintCurveGraph(int index)
		{
			FeatureStroke s = this.fStrokes[index];

			if (s != null)
			{
				double arcLength   = s.ArcLength.TotalLength;
				double[] curvature = s.Curvature.Profile;
				
				double xaxis = (double)panel.Width / (double)curvature.Length;
				double yaxis = (double)panel.Height / (s.Curvature.MaximumCurvature * 1.2);

				Graphics g = panel.CreateGraphics();
				System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();

				// Draw a graphics path (graph) of the curvature
				for (int i = 1; i < curvature.Length; i++)
				{
					gp.AddLine((float)((i-1) * xaxis), (float)(panel.Height - (curvature[i-1] * yaxis)), 
						(float)(i * xaxis), (float)(panel.Height - (curvature[i] * yaxis)));
				}

				g.DrawPath(new System.Drawing.Pen(Color.ForestGreen, 1.0f), gp);
			}
		}

		
		/// <summary>
		/// Draws where the corners of a stroke should be, relative to the speed and curvature display.
		/// </summary>
		/// <param name="index">Stroke index to draw the speed for</param>
		private void PaintCorners(int index)
		{
			FeatureStroke s = this.fStrokes[index];

			if (s != null)
			{
				int[] corners = new Corners(s).FindCorners();
				double xaxis  = (double)panel.Width / s.Speed.Profile.Length;

				Graphics g = panel.CreateGraphics();

				// Draw a vertical line indicating where a corner is
				for (int i = 0; i < corners.Length; i++)
				{
					g.DrawLine(new System.Drawing.Pen(Color.Red, 2.0f),
						(int)(corners[i] * xaxis), 0,
						(int)(corners[i] * xaxis), panel.Height);
				}
			}
		}
		
		#endregion
	}
}
