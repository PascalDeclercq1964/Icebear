namespace Icebear_Showcase
{
    partial class ShowCaseForm
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
            this.buttonCustomers = new System.Windows.Forms.Button();
            this.buttonProducts = new System.Windows.Forms.Button();
            this.buttonProductsGrouped = new System.Windows.Forms.Button();
            this.buttonOrderConfirmations = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonCustomers
            // 
            this.buttonCustomers.Location = new System.Drawing.Point(37, 30);
            this.buttonCustomers.Name = "buttonCustomers";
            this.buttonCustomers.Size = new System.Drawing.Size(263, 23);
            this.buttonCustomers.TabIndex = 0;
            this.buttonCustomers.Text = "Customer list";
            this.buttonCustomers.UseVisualStyleBackColor = true;
            // 
            // buttonProducts
            // 
            this.buttonProducts.Location = new System.Drawing.Point(37, 60);
            this.buttonProducts.Name = "buttonProducts";
            this.buttonProducts.Size = new System.Drawing.Size(263, 23);
            this.buttonProducts.TabIndex = 1;
            this.buttonProducts.Text = "Product list";
            this.buttonProducts.UseVisualStyleBackColor = true;
           // 
            // buttonProductsGrouped
            // 
            this.buttonProductsGrouped.Location = new System.Drawing.Point(37, 90);
            this.buttonProductsGrouped.Name = "buttonProductsGrouped";
            this.buttonProductsGrouped.Size = new System.Drawing.Size(263, 23);
            this.buttonProductsGrouped.TabIndex = 2;
            this.buttonProductsGrouped.Text = "Products grouped";
            this.buttonProductsGrouped.UseVisualStyleBackColor = true;
            // 
            // buttonOrderConfirmations
            // 
            this.buttonOrderConfirmations.Location = new System.Drawing.Point(37, 120);
            this.buttonOrderConfirmations.Name = "buttonOrderConfirmations";
            this.buttonOrderConfirmations.Size = new System.Drawing.Size(263, 23);
            this.buttonOrderConfirmations.TabIndex = 3;
            this.buttonOrderConfirmations.Text = "Order confirmation";
            this.buttonOrderConfirmations.UseVisualStyleBackColor = true;
            // 
            // ShowCaseForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(337, 176);
            this.Controls.Add(this.buttonOrderConfirmations);
            this.Controls.Add(this.buttonProductsGrouped);
            this.Controls.Add(this.buttonProducts);
            this.Controls.Add(this.buttonCustomers);
            this.Name = "ShowCaseForm";
            this.Text = "Icebear reports showcase";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonCustomers;
        private System.Windows.Forms.Button buttonProducts;
        private System.Windows.Forms.Button buttonProductsGrouped;
        private System.Windows.Forms.Button buttonOrderConfirmations;
    }
}

