﻿using System;
using System.Linq;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using ShomreiTorah.Common;
using ShomreiTorah.Data;
using ShomreiTorah.Data.UI;
using ShomreiTorah.Data.UI.DisplaySettings;
using ShomreiTorah.Singularity;
using ShomreiTorah.Singularity.Sql;
using ShomreiTorah.WinForms;
using ShomreiTorah.WinForms.Controls.Lookup;

namespace ShomreiTorah.DirectoryManager {
	class Program : AppFramework {
		[STAThread]
		static void Main() { new Program().Run(); }

		protected override ISplashScreen CreateSplash() { return null; }
		protected override void RegisterSettings() {
			if (Config.IsDebug)
				UserLookAndFeel.Default.SkinName = "DevExpress Style";
			else
				UserLookAndFeel.Default.SkinName = "Office 2010 Blue";

			Dialog.DefaultTitle = Config.OrgName + " Directory Manager";
			SkinManager.EnableFormSkins();

			EditorRepository.PersonLookup.AddConfigurator(properties => {
				properties.Columns.RemoveAt(properties.Columns.Count - 1);
				properties.Columns.Add(new DataSourceColumn {
					FieldName = "BalanceDue",
					Caption = "Total Due",
					FormatString = "{0:c}",
				});
			});
		}

		protected override Form CreateMainForm() {
			return new MainForm(new ExternalDataManager(DB.Default));
		}

		protected override DataSyncContext CreateDataContext() {
			var context = new DataContext();

			//These columns cannot be added in the strongly-typed row
			//because the People table must be usable without pledges
			//or payments.  (eg, ListMaker or Rafflizer)
			if (!Person.Schema.Columns.Contains("TotalPaid")) { //This can be called multiple times in the designer AppDomain
				Person.Schema.Columns.AddCalculatedColumn<Person, decimal>("TotalPaid", person => person.Payments.Sum(p => p.Amount));
				Person.Schema.Columns.AddCalculatedColumn<Person, decimal>("TotalPledged", person => person.Pledges.Sum(p => p.Amount));
				Person.Schema.Columns.AddCalculatedColumn<decimal>("BalanceDue", person => person.Field<decimal>("TotalPledged") - person.Field<decimal>("TotalPaid"));

				Payment.Schema.Columns.RemoveColumn(Payment.DepositColumn);
			}

			context.Tables.AddTable(Pledge.CreateTable());
			context.Tables.AddTable(Payment.CreateTable());
			context.Tables.AddTable(EmailAddress.CreateTable());
			context.Tables.AddTable(Person.CreateTable());

			var dsc = new DataSyncContext(context, new SqlServerSqlProvider(DB.Default));
			dsc.Tables.AddPrimaryMappings();
			return dsc;
		}
	}
}
