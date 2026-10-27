namespace R_Flash
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_log = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button_reset = new System.Windows.Forms.Button();
            this.button_check = new System.Windows.Forms.Button();
            this.button_write = new System.Windows.Forms.Button();
            this.button_earse = new System.Windows.Forms.Button();
            this.label_erase_mode = new System.Windows.Forms.Label();
            this.comboBox_erase_mode = new System.Windows.Forms.ComboBox();
            this.label_mcu = new System.Windows.Forms.Label();
            this.comboBox_mcu = new System.Windows.Forms.ComboBox();
            this.label_mcu_group = new System.Windows.Forms.Label();
            this.comboBox_mcu_group = new System.Windows.Forms.ComboBox();
            this.button_connect = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_speed = new System.Windows.Forms.ComboBox();
            this.button_burn_one_click = new System.Windows.Forms.Button();
            this.comboBox_device_list = new System.Windows.Forms.ComboBox();
            this.button_refresh = new System.Windows.Forms.Button();
            this.hexBox_hex_data = new Be.Windows.Forms.HexBox();
            this.button_open_file = new System.Windows.Forms.Button();
            this.textBox_file_path = new System.Windows.Forms.TextBox();
            this.panel_splitter = new System.Windows.Forms.Panel();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox_log
            // 
            this.textBox_log.Font = new System.Drawing.Font("思源黑体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBox_log.Location = new System.Drawing.Point(4, 592);
            this.textBox_log.Multiline = true;
            this.textBox_log.Name = "textBox_log";
            this.textBox_log.ReadOnly = true;
            this.textBox_log.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_log.Size = new System.Drawing.Size(1198, 236);
            this.textBox_log.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button_reset);
            this.groupBox1.Controls.Add(this.button_check);
            this.groupBox1.Controls.Add(this.button_write);
            this.groupBox1.Controls.Add(this.button_earse);
            this.groupBox1.Controls.Add(this.label_erase_mode);
            this.groupBox1.Controls.Add(this.comboBox_erase_mode);
            this.groupBox1.Controls.Add(this.label_mcu);
            this.groupBox1.Controls.Add(this.comboBox_mcu);
            this.groupBox1.Controls.Add(this.label_mcu_group);
            this.groupBox1.Controls.Add(this.comboBox_mcu_group);
            this.groupBox1.Controls.Add(this.button_connect);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.comboBox_speed);
            this.groupBox1.Controls.Add(this.button_burn_one_click);
            this.groupBox1.Controls.Add(this.comboBox_device_list);
            this.groupBox1.Controls.Add(this.button_refresh);
            this.groupBox1.Font = new System.Drawing.Font("思源黑体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox1.Location = new System.Drawing.Point(4, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(341, 574);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "DAP-Link";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // button_reset
            // 
            this.button_reset.Font = new System.Drawing.Font("思源黑体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_reset.Location = new System.Drawing.Point(11, 511);
            this.button_reset.Name = "button_reset";
            this.button_reset.Size = new System.Drawing.Size(182, 40);
            this.button_reset.TabIndex = 14;
            this.button_reset.Text = "复位";
            this.button_reset.UseVisualStyleBackColor = true;
            this.button_reset.Click += new System.EventHandler(this.button_reset_Click);
            // 
            // button_check
            // 
            this.button_check.Font = new System.Drawing.Font("思源黑体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_check.Location = new System.Drawing.Point(11, 453);
            this.button_check.Name = "button_check";
            this.button_check.Size = new System.Drawing.Size(182, 40);
            this.button_check.TabIndex = 14;
            this.button_check.Text = "校验";
            this.button_check.UseVisualStyleBackColor = true;
            this.button_check.Click += new System.EventHandler(this.button_check_Click);
            // 
            // button_write
            // 
            this.button_write.Font = new System.Drawing.Font("思源黑体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_write.Location = new System.Drawing.Point(11, 395);
            this.button_write.Name = "button_write";
            this.button_write.Size = new System.Drawing.Size(182, 40);
            this.button_write.TabIndex = 14;
            this.button_write.Text = "写入";
            this.button_write.UseVisualStyleBackColor = true;
            this.button_write.Click += new System.EventHandler(this.button_write_Click);
            // 
            // button_earse
            // 
            this.button_earse.Font = new System.Drawing.Font("思源黑体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_earse.Location = new System.Drawing.Point(11, 337);
            this.button_earse.Name = "button_earse";
            this.button_earse.Size = new System.Drawing.Size(182, 40);
            this.button_earse.TabIndex = 14;
            this.button_earse.Text = "擦除Flash";
            this.button_earse.UseVisualStyleBackColor = true;
            this.button_earse.Click += new System.EventHandler(this.button_earse_Click);
            // 
            // label_erase_mode
            // 
            this.label_erase_mode.AutoSize = true;
            this.label_erase_mode.Location = new System.Drawing.Point(8, 223);
            this.label_erase_mode.Name = "label_erase_mode";
            this.label_erase_mode.Size = new System.Drawing.Size(102, 26);
            this.label_erase_mode.TabIndex = 13;
            this.label_erase_mode.Text = "擦除方式：";
            // 
            // comboBox_erase_mode
            // 
            this.comboBox_erase_mode.FormattingEnabled = true;
            this.comboBox_erase_mode.Location = new System.Drawing.Point(116, 218);
            this.comboBox_erase_mode.Name = "comboBox_erase_mode";
            this.comboBox_erase_mode.Size = new System.Drawing.Size(219, 34);
            this.comboBox_erase_mode.TabIndex = 12;
            // 
            // label_mcu
            // 
            this.label_mcu.AutoSize = true;
            this.label_mcu.Location = new System.Drawing.Point(8, 174);
            this.label_mcu.Name = "label_mcu";
            this.label_mcu.Size = new System.Drawing.Size(102, 26);
            this.label_mcu.TabIndex = 11;
            this.label_mcu.Text = "芯片型号：";
            // 
            // comboBox_mcu
            // 
            this.comboBox_mcu.FormattingEnabled = true;
            this.comboBox_mcu.Location = new System.Drawing.Point(116, 169);
            this.comboBox_mcu.Name = "comboBox_mcu";
            this.comboBox_mcu.Size = new System.Drawing.Size(219, 34);
            this.comboBox_mcu.TabIndex = 10;
            this.comboBox_mcu.SelectedIndexChanged += new System.EventHandler(this.comboBox_mcu_SelectedIndexChanged);
            // 
            // label_mcu_group
            // 
            this.label_mcu_group.AutoSize = true;
            this.label_mcu_group.Location = new System.Drawing.Point(8, 125);
            this.label_mcu_group.Name = "label_mcu_group";
            this.label_mcu_group.Size = new System.Drawing.Size(102, 26);
            this.label_mcu_group.TabIndex = 9;
            this.label_mcu_group.Text = "芯片系列：";
            this.label_mcu_group.Click += new System.EventHandler(this.label_mcu_group_Click);
            // 
            // comboBox_mcu_group
            // 
            this.comboBox_mcu_group.FormattingEnabled = true;
            this.comboBox_mcu_group.Location = new System.Drawing.Point(116, 120);
            this.comboBox_mcu_group.Name = "comboBox_mcu_group";
            this.comboBox_mcu_group.Size = new System.Drawing.Size(219, 34);
            this.comboBox_mcu_group.TabIndex = 8;
            this.comboBox_mcu_group.SelectedIndexChanged += new System.EventHandler(this.comboBox_mcu_group_SelectedIndexChanged);
            // 
            // button_connect
            // 
            this.button_connect.Font = new System.Drawing.Font("思源黑体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.button_connect.Location = new System.Drawing.Point(11, 279);
            this.button_connect.Name = "button_connect";
            this.button_connect.Size = new System.Drawing.Size(182, 40);
            this.button_connect.TabIndex = 7;
            this.button_connect.Text = "连接芯片";
            this.button_connect.UseVisualStyleBackColor = true;
            this.button_connect.Click += new System.EventHandler(this.button_connect_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 79);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(110, 26);
            this.label1.TabIndex = 4;
            this.label1.Text = "速度(kHz)：";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // comboBox_speed
            // 
            this.comboBox_speed.FormattingEnabled = true;
            this.comboBox_speed.Location = new System.Drawing.Point(116, 74);
            this.comboBox_speed.Name = "comboBox_speed";
            this.comboBox_speed.Size = new System.Drawing.Size(138, 34);
            this.comboBox_speed.TabIndex = 3;
            // 
            // button_burn_one_click
            // 
            this.button_burn_one_click.Font = new System.Drawing.Font("思源黑体", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_burn_one_click.Location = new System.Drawing.Point(235, 279);
            this.button_burn_one_click.Name = "button_burn_one_click";
            this.button_burn_one_click.Size = new System.Drawing.Size(100, 268);
            this.button_burn_one_click.TabIndex = 6;
            this.button_burn_one_click.Text = "一键烧录";
            this.button_burn_one_click.UseVisualStyleBackColor = true;
            this.button_burn_one_click.Click += new System.EventHandler(this.button_burn_one_click_Click);
            // 
            // comboBox_device_list
            // 
            this.comboBox_device_list.FormattingEnabled = true;
            this.comboBox_device_list.Location = new System.Drawing.Point(8, 27);
            this.comboBox_device_list.Name = "comboBox_device_list";
            this.comboBox_device_list.Size = new System.Drawing.Size(246, 34);
            this.comboBox_device_list.TabIndex = 1;
            // 
            // button_refresh
            // 
            this.button_refresh.Font = new System.Drawing.Font("思源黑体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_refresh.Location = new System.Drawing.Point(260, 26);
            this.button_refresh.Name = "button_refresh";
            this.button_refresh.Size = new System.Drawing.Size(75, 74);
            this.button_refresh.TabIndex = 0;
            this.button_refresh.Text = "刷新";
            this.button_refresh.UseVisualStyleBackColor = true;
            this.button_refresh.Click += new System.EventHandler(this.button_refresh_Click);
            // 
            // hexBox_hex_data
            // 
            this.hexBox_hex_data.Font = new System.Drawing.Font("思源黑体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.hexBox_hex_data.LineInfoVisible = true;
            this.hexBox_hex_data.Location = new System.Drawing.Point(351, 54);
            this.hexBox_hex_data.Name = "hexBox_hex_data";
            this.hexBox_hex_data.ReadOnly = true;
            this.hexBox_hex_data.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(60)))), ((int)(((byte)(188)))), ((int)(((byte)(255)))));
            this.hexBox_hex_data.Size = new System.Drawing.Size(851, 532);
            this.hexBox_hex_data.StringViewVisible = true;
            this.hexBox_hex_data.TabIndex = 2;
            this.hexBox_hex_data.UseFixedBytesPerLine = true;
            this.hexBox_hex_data.VScrollBarVisible = true;
            // 
            // button_open_file
            // 
            this.button_open_file.Font = new System.Drawing.Font("思源黑体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_open_file.Location = new System.Drawing.Point(1076, 12);
            this.button_open_file.Name = "button_open_file";
            this.button_open_file.Size = new System.Drawing.Size(118, 40);
            this.button_open_file.TabIndex = 3;
            this.button_open_file.Text = "打开文件";
            this.button_open_file.UseVisualStyleBackColor = true;
            this.button_open_file.Click += new System.EventHandler(this.button_open_file_Click);
            // 
            // textBox_file_path
            // 
            this.textBox_file_path.Font = new System.Drawing.Font("思源黑体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBox_file_path.Location = new System.Drawing.Point(351, 12);
            this.textBox_file_path.Name = "textBox_file_path";
            this.textBox_file_path.ReadOnly = true;
            this.textBox_file_path.Size = new System.Drawing.Size(719, 34);
            this.textBox_file_path.TabIndex = 4;
            // 
            // panel_splitter
            // 
            this.panel_splitter.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panel_splitter.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.panel_splitter.Location = new System.Drawing.Point(351, 586);
            this.panel_splitter.Name = "panel_splitter";
            this.panel_splitter.Size = new System.Drawing.Size(851, 8);
            this.panel_splitter.TabIndex = 5;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1206, 831);
            this.Controls.Add(this.panel_splitter);
            this.Controls.Add(this.textBox_file_path);
            this.Controls.Add(this.button_open_file);
            this.Controls.Add(this.hexBox_hex_data);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBox_log);
            this.Name = "Form1";
            this.Text = "DAP-Flash";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_log;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox comboBox_device_list;
        private System.Windows.Forms.Button button_refresh;
        private Be.Windows.Forms.HexBox hexBox_hex_data;
        private System.Windows.Forms.Button button_open_file;
        private System.Windows.Forms.TextBox textBox_file_path;
        private System.Windows.Forms.Button button_burn_one_click;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_speed;
        private System.Windows.Forms.Button button_connect;
        private System.Windows.Forms.Label label_mcu_group;
        private System.Windows.Forms.ComboBox comboBox_mcu_group;
        private System.Windows.Forms.Label label_mcu;
        private System.Windows.Forms.ComboBox comboBox_mcu;
        private System.Windows.Forms.Label label_erase_mode;
        private System.Windows.Forms.ComboBox comboBox_erase_mode;
        private System.Windows.Forms.Button button_reset;
        private System.Windows.Forms.Button button_check;
        private System.Windows.Forms.Button button_write;
        private System.Windows.Forms.Button button_earse;
        private System.Windows.Forms.Panel panel_splitter;
    }
}
