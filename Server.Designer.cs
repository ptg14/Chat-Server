namespace Chat_Server
{
    partial class Server
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            LBox_user = new ListBox();
            rTB_log = new RichTextBox();
            BT_listen = new Button();
            SuspendLayout();
            // 
            // LBox_user
            // 
            LBox_user.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            LBox_user.FormattingEnabled = true;
            LBox_user.Location = new Point(12, 12);
            LBox_user.Name = "LBox_user";
            LBox_user.Size = new Size(150, 384);
            LBox_user.TabIndex = 0;
            // 
            // rTB_log
            // 
            rTB_log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rTB_log.Location = new Point(168, 12);
            rTB_log.Name = "rTB_log";
            rTB_log.Size = new Size(620, 384);
            rTB_log.TabIndex = 1;
            rTB_log.Text = "";
            // 
            // BT_listen
            // 
            BT_listen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            BT_listen.Location = new Point(694, 402);
            BT_listen.Name = "BT_listen";
            BT_listen.Size = new Size(94, 29);
            BT_listen.TabIndex = 2;
            BT_listen.Text = "Start";
            BT_listen.UseVisualStyleBackColor = true;
            BT_listen.Click += BT_listen_Click;
            // 
            // Server
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 442);
            Controls.Add(BT_listen);
            Controls.Add(rTB_log);
            Controls.Add(LBox_user);
            Name = "Server";
            Text = "Server";
            ResumeLayout(false);
        }

        #endregion

        private ListBox LBox_user;
        private RichTextBox rTB_log;
        private Button BT_listen;
    }
}