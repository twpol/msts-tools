//------------------------------------------------------------------------------
// Jgr library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace Jgr {
	public class ApplicationSettings {
		readonly string _path;
		readonly ApplicationSettings<bool> _settingsBoolean;
		readonly ApplicationSettings<int> _settingsInteger;
		readonly ApplicationSettings<string> _settingsString;
		readonly Dictionary<string, ApplicationSettings> _groups;
		readonly ApplicationSettings _groupDefault;

		public string Path { get { return _path; } }
		public ApplicationSettings<bool> Boolean { get { return _settingsBoolean; } }
		public ApplicationSettings<int> Integer { get { return _settingsInteger; } }
		public ApplicationSettings<string> String { get { return _settingsString; } }
		public ApplicationSettings Default { get { return _groupDefault; } }

		ApplicationSettings() {
			_settingsBoolean = new ApplicationSettings<bool>(this, RegistryValueKind.DWord);
			_settingsInteger = new ApplicationSettings<int>(this, RegistryValueKind.DWord);
			_settingsString = new ApplicationSettings<string>(this, RegistryValueKind.String);
			_groups = new Dictionary<string, ApplicationSettings>();
		}

		public ApplicationSettings(string filePath)
			: this() {
			var version = FileVersionInfo.GetVersionInfo(filePath);
			_path = string.Format(@"Software\{0}\{1}", version.CompanyName, version.ProductName);
			if ((version.ProductName == version.FileDescription) || string.IsNullOrEmpty(version.FileDescription)) {
				_groupDefault = this;
			} else {
				_groupDefault = this[version.FileDescription];
			}
		}

		protected ApplicationSettings(ApplicationSettings root, string path)
			: this() {
			_path = string.Format(@"{0}\{1}", root._path, path);
		}

		public ApplicationSettings this[string group] {
			get {
				ApplicationSettings groupSettings;
				if (!_groups.TryGetValue(group, out groupSettings))
					_groups[group] = groupSettings = new ApplicationSettings(this, group);
				return groupSettings;
			}
		}
	}

	public class ApplicationSettings<T> : IEnumerable<string> {
		readonly ApplicationSettings _owner;
		readonly RegistryValueKind _kind;

		internal ApplicationSettings(ApplicationSettings owner, RegistryValueKind keyKind) {
			_owner = owner;
			_kind = keyKind;
		}

		public T this[string name] {
			get {
				using (var key = Registry.CurrentUser.OpenSubKey(_owner.Path)) {
					var value = key != null ? key.GetValue(name, null) : null;
					if ((value != null) && (typeof(T) == typeof(bool))) value = (int)value != 0;
					return value != null ? (T)value : default(T);
				}
			}
			set {
				using (var key = Registry.CurrentUser.CreateSubKey(_owner.Path)) {
					key.SetValue(name, value, _kind);
				}
			}
		}

		#region IEnumerable<string> Members

		public IEnumerator<string> GetEnumerator() {
			using (var key = Registry.CurrentUser.OpenSubKey(_owner.Path)) {
				return ((IEnumerable<string>)key.GetValueNames()).GetEnumerator();
			}
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion
	}
}
