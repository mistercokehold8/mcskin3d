﻿//
//    MCSkin3D, a 3d skin management studio for Minecraft
//    Copyright (C) 2011-2012 Altered Softworks & MCSkin3D Team
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MCSkin3D.Language
{
	public static class LanguageLoader
	{
		public static List<Language> Languages = new List<Language>();

		public static void LoadLanguages(string path)
		{
			foreach (var file in Directory.GetFiles(path, "*.lang"))
			{
				try
				{
					using (var sr = new StreamReader(file, Encoding.Unicode))
						Languages.Add(Language.Parse(sr));
				}
				catch
				{
				}
			}
		}

		public static Language FindLanguage(string p)
		{
			foreach (var l in Languages)
				if (l.Name == p ||
					l.Culture.Name == p)
					return l;

			return null;
		}

		public static Language LoadDefault()
		{
			using (var writer = new FileStream("Languages\\English.lang", FileMode.Create))
				writer.Write(Properties.Resources.English, 0, Properties.Resources.English.Length);

			using (var reader = new StreamReader(new MemoryStream(Properties.Resources.English), Encoding.Unicode))
			{
				var lang = Language.Parse(reader);
				Languages.Add(lang);
				return lang;
			}
		}
	}
}
