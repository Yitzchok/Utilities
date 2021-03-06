using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShomreiTorah.Common;
using System.IO;
using System.Threading;

namespace ShomreiTorah.Backup {
	class Program {
		static void Main() {
			try {
				ExecOperation(DbBackup.DoBackup(), "Database");
			} catch (Exception ex) {
				Email.Default.Send(Email.AlertsAddress, Email.AdminAddress, "Shomrei Torah Backup Exception", ex.ToString(), false);
			}
			Console.WriteLine("Finished");
		}
		static void ExecOperation(IEnumerator<string> operation, string name) {
			Console.WriteLine("Starting " + name);
			using (operation) {
				string current = "Loading";
				while (true) {
					try {
						if (!operation.MoveNext()) return;
						current = operation.Current;
						Console.WriteLine("  " + current);
					} catch (Exception ex) {
						Email.Default.Send(Email.AlertsAddress, Email.AdminAddress, "Shomrei Torah " + name + " Backup Exception: " + current, ex.ToString(), false);
					}
				}
			}
		}

		public static bool AreEqual(string first, string second) {
			using (var stream1 = File.Open(first, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (var stream2 = File.Open(second, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				return stream1.IsEqualTo(stream2);
			}
		}
	}
}
