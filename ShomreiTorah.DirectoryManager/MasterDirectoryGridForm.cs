using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.Data;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;

namespace ShomreiTorah.DirectoryManager {
	partial class MasterDirectoryGridForm : XtraForm {
		public MasterDirectoryGridForm() {
			InitializeComponent();
			gridView.ActiveFilterString = "TotalPledged <> 0 OR TotalPaid <> 0";
		}
	}
}