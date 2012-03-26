﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class DvdStudioPro : SubtitleFormat
    {
        static readonly Regex RegexTimeCodes = new Regex(@"^\d+:\d+:\d+:\d+\t,\t\d+:\d+:\d+:\d+\t,\t.*$", RegexOptions.Compiled);

        public override string Extension
        {
            get { return ".STL"; }
        }

        public override string Name
        {
            get { return "DVD Studio Pro"; }
        }

        public override bool HasLineNumber
        {
            get { return false; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            var subtitle = new Subtitle();
            LoadSubtitle(subtitle, lines, fileName);
            return subtitle.Paragraphs.Count > _errorCount;
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            const string paragraphWriteFormat = "{0}\t,\t{1}\t,\t{2}\r\n";
            const string timeFormat = "{0:00}:{1:00}:{2:00}:{3:00}";
            const string header = @"$VertAlign          =   Bottom
$Bold               =   FALSE
$Underlined         =   FALSE
$Italic             =   0
$XOffset                =   0
$YOffset                =   -5
$TextContrast           =   15
$Outline1Contrast           =   15
$Outline2Contrast           =   13
$BackgroundContrast     =   0
$ForceDisplay           =   FALSE
$FadeIn             =   0
$FadeOut                =   0
$HorzAlign          =   Center
";

            var sb = new StringBuilder();
            sb.AppendLine(header);
            foreach (Paragraph p in subtitle.Paragraphs)
            {
                double factor = (1000.0 / Configuration.Settings.General.CurrentFrameRate);
                string startTime = string.Format(timeFormat, p.StartTime.Hours, p.StartTime.Minutes, p.StartTime.Seconds, (int)Math.Round(p.StartTime.Milliseconds  / factor));
                string endTime = string.Format(timeFormat, p.EndTime.Hours, p.EndTime.Minutes, p.EndTime.Seconds, (int)Math.Round(p.EndTime.Milliseconds / factor));
                sb.Append(string.Format(paragraphWriteFormat, startTime, endTime, EncodeStyles(p.Text.Replace(Environment.NewLine, " | "))));
            }
            return sb.ToString().Trim();
        }

        public static byte GetFrameFromMilliseconds(int milliseconds, double frameRate)
        {
            return (byte)Math.Round(milliseconds / (1000.0 / frameRate));
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            _errorCount = 0;
            int number = 0;
            foreach (string line in lines)
            {
                if (line.Trim().Length > 0 && line[0] != '$')
                {
                    if (RegexTimeCodes.Match(line).Success)
                    {
                        string[] threePart = line.Split(new[] { "\t,\t"}, StringSplitOptions.None);
                        var p = new Paragraph();
                        if (threePart.Length == 3 &&
                            GetTimeCode(p.StartTime, threePart[0]) &&
                            GetTimeCode(p.EndTime, threePart[1]))
                        {
                            number++;
                            p.Number = number;
                            p.Text = threePart[2].TrimEnd().Replace(" | ", Environment.NewLine).Replace("|", Environment.NewLine);
                            p.Text = DecodeStyles(p.Text);
                            subtitle.Paragraphs.Add(p);
                        }
                    }
                    else
                    {
                        _errorCount++;
                    }
                }
            }
        }

        internal static string DecodeStyles(string text)
        {
            var sb = new StringBuilder();
            bool italicOn = false;
            bool boldOn = false;
            bool skipNext = false;
            for (int i = 0; i<text.Length; i++)
            {
                if (skipNext)
                {
                    skipNext = false;
                }
                else
                {
                    if (text.Substring(i).StartsWith("^I"))
                    {
                        if (!italicOn)
                            sb.Append("<i>");
                        else
                            sb.Append("</i>");
                        italicOn = !italicOn;
                        skipNext = true;
                    }
                    else if (text.Substring(i).StartsWith("^B"))
                    {
                        if (!boldOn)
                            sb.Append("<b>");
                        else
                            sb.Append("</b>");
                        boldOn = !boldOn;
                        skipNext = true;
                    }
                    else
                    {
                        sb.Append(text.Substring(i, 1));
                    }                    
                }
            }
            return sb.ToString();
        }

        internal static string EncodeStyles(string text)
        {
            text = text.Replace("<i>", "^I");
            text = text.Replace("<I>", "^I");
            text = text.Replace("</i>", "^I");
            text = text.Replace("</I>", "^I");

            text = text.Replace("<b>", "^B");
            text = text.Replace("<B>", "^B");
            text = text.Replace("</b>", "^B");
            text = text.Replace("</B>", "^B");

            return text;
        }

        private static bool GetTimeCode(TimeCode timeCode, string timeString)
        {
            try
            {
                string[] timeParts = timeString.Split(':');
                timeCode.Hours = int.Parse(timeParts[0]);
                timeCode.Minutes = int.Parse(timeParts[1]);
                timeCode.Seconds = int.Parse(timeParts[2]);
                int frames = int.Parse(timeParts[3]);

                int milliseconds = (int)Math.Round(1000.0 / Configuration.Settings.General.CurrentFrameRate * frames);
                if (milliseconds > 999)
                    milliseconds = 999;

                timeCode.Milliseconds = milliseconds;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
