﻿using Rampastring.Tools;
using System.IO;

namespace ClientCore
{
    public class CCIniFile : IniFile
    {
        public CCIniFile(string path) : base(path)
        {
            foreach (IniSection section in Sections)
            {
                string baseSectionName = section.GetStringValue("$BaseSection", null);

                if (string.IsNullOrWhiteSpace(baseSectionName))
                    continue;

                var baseSection = Sections.Find(s => s.SectionName == baseSectionName);
                if (baseSection == null)
                {
                    Logger.Log($"基础部分在 INI 文件中未找到 {path}，部分 {section.SectionName}，基础部分名称: {baseSectionName}");
                    continue;
                }

                int addedKeyCount = 0;

                foreach (var kvp in baseSection.Keys)
                {
                    if (!section.KeyExists(kvp.Key))
                    {
                        section.Keys.Insert(addedKeyCount, kvp);
                        addedKeyCount++;
                    }
                }
            }
        }

        protected override void ApplyBaseIni()
        {
            string basedOnSetting = GetStringValue("INISystem", "BasedOn", string.Empty);
            if (string.IsNullOrEmpty(basedOnSetting))
                return;

            string[] basedOns = basedOnSetting.Split(',');
            foreach (string basedOn in basedOns)
                ApplyBasedOnIni(basedOn);
        }

        private void ApplyBasedOnIni(string basedOn)
        {
            if (string.IsNullOrEmpty(basedOn))
                return;

            FileInfo baseIniFile;
            if (basedOn.Contains("$THEME_DIR$"))
                baseIniFile = SafePath.GetFile(basedOn.Replace("$THEME_DIR$", ProgramConstants.GetResourcePath()));
            else
                baseIniFile = SafePath.GetFile(SafePath.GetFileDirectoryName(FileName), basedOn);

            // Consolidate with the INI file that this INI file is based on
            if (!baseIniFile.Exists)
                Logger.Log(FileName + ": 基础 INI 文件未找到！ " + baseIniFile.FullName);

            CCIniFile baseIni = new CCIniFile(baseIniFile.FullName);
            ConsolidateIniFiles(baseIni, this);
            Sections = baseIni.Sections;
        }
    }
}
