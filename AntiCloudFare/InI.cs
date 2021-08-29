using System;
using System.Runtime.InteropServices;
using System.Text;


namespace EricZhao.UiThread
{
    public class IniFile
    {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="INIPath"></PARAM>
        public IniFile(string INIPath, Boolean abMakeSureExists = true)
        {
            path = INIPath;

            if (abMakeSureExists)
            {
                this.MakeSureExists(INIPath);
            }
        }

        public void MakeSureExists(String astrFile)
        {
            if (!System.IO.File.Exists(astrFile))
            {
                System.IO.StreamWriter loStream = null;
                try
                {
                    loStream = System.IO.File.CreateText(astrFile);
                }
                catch (System.Exception ex)
                {

                }
                finally
                {
                    try
                    {
                        loStream.Close();
                    }
                    catch (System.Exception ex)
                    {

                    }
                    finally
                    {
                    }
                }
            }
        }
        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// Section name
        /// <PARAM name="Key"></PARAM>
        /// Key Name
        /// <PARAM name="Value"></PARAM>
        /// Value Name
        public void IniWriteStringValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);

        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public string IniReadStringValue(string Section, string Key, String valueDefault = "", Boolean abAutoWriteIfNotExist = false)
        {
            lock (this)
            {
                StringBuilder temp = new StringBuilder(255);
                int i = GetPrivateProfileString(Section, Key, valueDefault, temp,
                                                255, this.path);

                if (temp.ToString().Equals(valueDefault, StringComparison.CurrentCultureIgnoreCase))
                {
                    this.IniWriteStringValue(Section, Key, valueDefault);
                }
                return temp.ToString();
            }

        }

        public void iniWriteIntValue(string Section, string Key, int anValue)
        {
            lock (this)
            {
                this.IniWriteStringValue(Section, Key, anValue + "");
            }
        }

        public int IniReadIntValue(string Section, string Key, int anDefaultValue = 0, Boolean abAutoWriteIfNotExist = false)
        {
            lock (this)
            {
                String lstrData = this.IniReadStringValue(Section, Key, anDefaultValue + "");

                int lnData = 0;
                if (Int32.TryParse(lstrData, out lnData))
                {
                    if (lnData == anDefaultValue)
                    {
                        this.IniWriteStringValue(Section, Key, lstrData);
                    }
                    return lnData;
                }
                else
                {
                    this.IniWriteStringValue(Section, Key, anDefaultValue + "");

                    return anDefaultValue;
                }
            } 
        }

        public void iniWriteDoubleValue(string Section, string Key, double anValue)
        {
            lock (this)
            {
                this.IniWriteStringValue(Section, Key, anValue + "");
            }
        }

        public double IniReadDoubleValue(string Section, string Key, double anDefaultValue = 0,
            Boolean abAutoWriteIfNotExist = false)
        {
            lock (this)
            {
                String lstrData = this.IniReadStringValue(Section, Key, anDefaultValue + "");

                double lnData = 0;
                if (Double.TryParse(lstrData, out lnData))
                {
                    if (lnData == anDefaultValue)
                    {
                        this.IniWriteStringValue(Section, Key, lstrData);
                    }
                    return lnData;
                }
                else
                {
                    this.IniWriteStringValue(Section, Key, anDefaultValue + "");

                    return anDefaultValue;
                }
            }
        }
    }
}
