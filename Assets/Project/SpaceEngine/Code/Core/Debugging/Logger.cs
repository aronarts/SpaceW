﻿#region License
// Procedural planet generator.
//  
// Copyright (C) 2015-2017 Denis Ovchinnikov [zameran] 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//     notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. Neither the name of the copyright holders nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION)HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
// 
// Creation Date: 2017.05.02
// Creation Time: 5:04 PM
// Creator: zameran
#endregion

using System.Collections.Generic;

namespace SpaceEngine.Core.Debugging
{
    using System;
    using System.IO;
    using System.Threading;

    using UnityEngine;

    public enum Category
    {
        Important = 0,
        Triggers = 1,
        InGameUI = 2,
        Camera = 3,
        Data = 4,
        Input = 5,
        Core = 6,
        Animation = 7,
        Player = 8,
        Editor = 9,
        Gameplay = 10,
        External = 11,
        Other = 12,
        Graphics = 13,
        Error = 14
    };

    public sealed class LoggerPalette
    {
        private Dictionary<Category, Color> Palette = new Dictionary<Category, Color>()
        {
            {Category.Important, XKCDColors.Amber},
            {Category.Triggers, XKCDColors.Orange},
            {Category.InGameUI, XKCDColors.DullGreen},
            {Category.Camera, XKCDColors.Green},
            {Category.Data, XKCDColors.BrightSkyBlue},
            {Category.Input, XKCDColors.Yellow},
            {Category.Core, XKCDColors.DullRed},
            {Category.Animation, XKCDColors.Purple},
            {Category.Player, XKCDColors.Greenish},
            {Category.Editor, XKCDColors.White},
            {Category.Gameplay, XKCDColors.DarkPurple},
            {Category.External, XKCDColors.Brownish},
            {Category.Other, XKCDColors.DirtyGreen},
            {Category.Graphics, XKCDColors.BrightRed},
            {Category.Error, XKCDColors.Red}
        };

        public Color GetColorFromCategory(Category category)
        {
            return Palette[category];
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class UseLogger : Attribute
    {
        public Category Category;

        public UseLogger(Category category)
        {
            this.Category = category;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class UseLoggerFile : Attribute
    {
        public string[] LogFileNamePrefixes;

        public UseLoggerFile(params string[] logFileNamePrefixes)
        {
            this.LogFileNamePrefixes = logFileNamePrefixes;
        }
    }

    public static class Logger
    {
        private static readonly ReaderWriterLockSlim IOLock = new ReaderWriterLockSlim();
        private static readonly LoggerPalette Palette = new LoggerPalette();
        private static readonly Category DefaultDebugCategory = Category.Other;

        private static bool DebuggerActive;

        private static Category DebugCategory;

        private static void Detect(out bool shouldDump, out string[] logFileNamePrefixes)
        {
            shouldDump = false;
            logFileNamePrefixes = new string[0];

            // NOTE : Be careful with skipFrames parameter!
            var frame = new System.Diagnostics.StackFrame(2, true);
            var declaringType = frame.GetMethod().DeclaringType;

            if (declaringType == null)
            {
                Debug.LogWarning("Logger: Declaring type is null!");

                return;
            }

            var loggerFileClassAttributes = declaringType.GetCustomAttributes(typeof(UseLoggerFile), true) as UseLoggerFile[];
            var loggerClassAttributes = declaringType.GetCustomAttributes(typeof(UseLogger), true) as UseLogger[];
            var loggerMethodAttributes = frame.GetMethod().GetCustomAttributes(typeof(UseLogger), false) as UseLogger[];

            if (loggerFileClassAttributes != null && loggerFileClassAttributes.Length != 0)
            {
                logFileNamePrefixes = loggerFileClassAttributes[0].LogFileNamePrefixes;

                shouldDump = true;
            }
            else
            { shouldDump = false; }

            if (loggerMethodAttributes != null && loggerMethodAttributes.Length != 0)
            {
                DebugCategory = loggerMethodAttributes[0].Category;
                DebuggerActive = true;
            }
            else
            {
                if (loggerClassAttributes != null && loggerClassAttributes.Length != 0)
                {
                    DebugCategory = loggerClassAttributes[0].Category;
                    DebuggerActive = true;
                }
                else
                {
                    DebugCategory = DefaultDebugCategory;
                    DebuggerActive = false;
                }
            }
        }

        public static void LogError(object obj)
        {
            bool shouldDump;
            string[] logFileNamePrefixes;

            Detect(out shouldDump, out logFileNamePrefixes);

            // Override our stuff for errors only...
            DebugCategory = Category.Error;
            DebuggerActive = true;

            DoWork(obj, shouldDump, logFileNamePrefixes);
        }

        public static void Log(object obj)
        {
            bool shouldDump;
            string[] logFileNamePrefixes;

            Detect(out shouldDump, out logFileNamePrefixes);

            DoWork(obj, shouldDump, logFileNamePrefixes);
        }

        private static void WriteToFile(ref string[] logFileNamePrefixes, string timeStamp, string typeNameString, object str, int i)
        {
            IOLock.EnterWriteLock();

            try
            {
                if (string.IsNullOrEmpty(logFileNamePrefixes[i])) return;

                var path = Path.GetFullPath(string.Format("{0}/../{1}_Log.txt", Application.dataPath, logFileNamePrefixes[i]));
                var dumpContent = string.Format("{0}[{1}] : {2}", timeStamp, typeNameString, str);

                using (var outputStream = File.AppendText(path))
                {
                    outputStream.WriteLine(dumpContent);
                    outputStream.Flush();
                    outputStream.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.Log(string.Format("Logger: WriteToFile Exception! {0}", ex.Message));
            }
            finally
            {
                IOLock.ExitWriteLock();
            }
        }

        private static void DoWork(object obj, bool shouldDump, string[] logFileNamePrefixes)
        {
            if (!DebuggerActive)
            {
                Debug.Log(obj.ToString());

                return;
            }

            var colorString = XKCDColors.ColorTranslator.ToRGBHex(Palette.GetColorFromCategory(DebugCategory));
            var categoryString = DebugCategory.ToString();
            var timeStampString = string.Format("[{0:H:mm:ss}]", DateTime.Now);

            if (!Application.isEditor)
            {
                if (shouldDump)
                {
                    for (byte i = 0; i < logFileNamePrefixes.Length; i++)
                    {
                        WriteToFile(ref logFileNamePrefixes, timeStampString, categoryString, obj, i);
                    }
                }

                obj = string.Format("{0}[{1}] : {2}", timeStampString, categoryString, obj);
            }
            else
            {
                obj = string.Format("<color={0}>[{1}] <b>{2}</b> </color>", colorString, categoryString, obj);
            }

            Debug.Log(obj);
        }
    }
}