﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Deveel.CSharpCC.Parser {
    internal static class CSharpCCGlobals {
        public const string ToolName = "CSharpCC";

        public static string FileName;
        public static string OriginalFileName;

        public static bool TreeGenerated;

        public static IList<string> ToolNames;

        public static String cu_name;
        public static IList<Token> cu_to_insertion_point_1 = new List<Token>();
        public static IList<Token> cu_to_insertion_point_2 = new List<Token>();
        public static IList<Token> cu_from_insertion_point_2 = new List<Token>();

        public static IList<NormalProduction> bnfproductions = new List<NormalProduction>();
        public static IDictionary<string, NormalProduction> production_table = new Dictionary<string, NormalProduction>();

        public static IDictionary<string, int> lexstate_S2I = new Dictionary<string, int>();
        public static IDictionary<int, string> lexstate_I2S = new Dictionary<int, string>();

        public static IList<Token> token_mgr_decls;

        public static IList<TokenProduction> rexprlist = new List<TokenProduction>();

        public static int tokenCount;
        public static IDictionary<string, RegularExpression> named_tokens_table = new Dictionary<string, RegularExpression>();
        public static IList<RegularExpression> ordered_named_tokens = new List<RegularExpression>();
        public static IDictionary<int, string> names_of_tokens = new Dictionary<int, string>();
        public static IDictionary<int, RegularExpression> rexps_of_tokens = new Dictionary<int, RegularExpression>();

        public static IDictionary<string, IDictionary<string, RegularExpression>> simple_tokens_table =
            new Dictionary<string, IDictionary<string, RegularExpression>>();

        internal static int maskindex = 0;
        internal static int cc2index = 0;
        public static bool lookaheadNeeded;

        internal static List<int[]> maskVals = new List<int[]>();

        internal static Action actForEof;
        internal static String nextStateForEof;
        internal static int cline;
        internal static int ccol;

        public static void BannerLine(String fullName, String ver) {
            Console.Out.Write("C# Compiler Compiler Version " + Assembly.GetAssembly(typeof(CSharpCCGlobals)).GetName().Version + " (" + fullName);
            if (!ver.Equals("")) {
                Console.Out.Write(" Version " + ver);
            }
            Console.Out.WriteLine();
        }

        public static string GetIdString(String toolName, String fileName) {
            IList<string> toolNames = new List<string>();
            toolNames.Add(toolName);
            return GetIdString(toolNames, fileName);
        }

        public static String GetIdString(IList<string> toolNames, String fileName) {
            int i;
            String toolNamePrefix = "Generated By:";

            for (i = 0; i < toolNames.Count - 1; i++)
                toolNamePrefix += toolNames[i] + "&";
            toolNamePrefix += toolNames[i] + ":";

            if (toolNamePrefix.Length > 200) {
                Console.Out.WriteLine("Tool names too long.");
                throw new InvalidOperationException();
            }

            return toolNamePrefix + " Do not edit this line. " + AddUnicodeEscapes(fileName);
        }

        public static bool IsGeneratedBy(String toolName, String fileName) {
            IList<string> v = GetToolNames(fileName);

            for (int i = 0; i < v.Count; i++)
                if (toolName.Equals(v[i]))
                    return true;

            return false;
        }

        private static IList<string> MakeToolNameList(String str) {
            List<string> retVal = new List<string>();

            int limit1 = str.IndexOf('\n');
            if (limit1 == -1)
                limit1 = 1000;
            int limit2 = str.IndexOf('\r');
            if (limit2 == -1)
                limit2 = 1000;
            int limit = (limit1 < limit2) ? limit1 : limit2;

            String tmp;
            if (limit == 1000) {
                tmp = str;
            } else {
                tmp = str.Substring(0, limit);
            }

            if (tmp.IndexOf(':') == -1)
                return retVal;

            tmp = tmp.Substring(tmp.IndexOf(':') + 1);

            if (tmp.IndexOf(':') == -1)
                return retVal;

            tmp = tmp.Substring(0, tmp.IndexOf(':'));

            int i = 0, j = 0;

            while (j < tmp.Length && (i = tmp.IndexOf('&', j)) != -1) {
                retVal.Add(tmp.Substring(j, i - j));
                j = i + 1;
            }

            if (j < tmp.Length)
                retVal.Add(tmp.Substring(j));

            return retVal;
        }

        public static IList<string> GetToolNames(string fileName) {
            char[] buf = new char[256];
            StreamReader stream = null;
            int read, total = 0;

            try {
                stream = new StreamReader(fileName);

                for (;;)
                    if ((read = stream.Read(buf, total, buf.Length - total)) != 0) {
                        if ((total += read) == buf.Length)
                            break;
                    } else
                        break;

                return MakeToolNameList(new String(buf, 0, total));
            } catch (FileNotFoundException) {
            } catch (IOException) {
                if (total > 0)
                    return MakeToolNameList(new String(buf, 0, total));
            } finally {
                if (stream != null)
                    try {
                        stream.Close();
                    } catch (Exception) {
                    }
            }

            return new List<string>();
        }

        public static void CreateOutputDir(string outputDir) {
            if (!Directory.Exists(outputDir)) {
                CSharpCCErrors.Warning("Output directory \"" + outputDir + "\" does not exist. Creating the directory.");

                if (Directory.CreateDirectory(outputDir) == null) {
                    CSharpCCErrors.SemanticError("Cannot create the output directory : " + outputDir);
                    return;
                }
            }

            /*
            TODO:
            if (!outputDir.canWrite())
            {
                CSharpCCErrors.SemanticError("Cannot write to the output output directory : \"" + outputDir + "\"");
                return;
            }
            */
        }

        public static String AddEscapes(String str) {
            String retval = "";
            char ch;
            for (int i = 0; i < str.Length; i++) {
                ch = str[i];
                if (ch == '\b') {
                    retval += "\\b";
                } else if (ch == '\t') {
                    retval += "\\t";
                } else if (ch == '\n') {
                    retval += "\\n";
                } else if (ch == '\f') {
                    retval += "\\f";
                } else if (ch == '\r') {
                    retval += "\\r";
                } else if (ch == '\"') {
                    retval += "\\\"";
                } else if (ch == '\'') {
                    retval += "\\\'";
                } else if (ch == '\\') {
                    retval += "\\\\";
                } else if (ch < 0x20 || ch > 0x7e) {
                    String s = "0000" + Convert.ToString(ch, 16);
                    retval += "\\u" + s.Substring(s.Length - 4, s.Length);
                } else {
                    retval += ch;
                }
            }
            return retval;
        }

        public static String AddUnicodeEscapes(String str) {
            String retval = "";
            char ch;
            for (int i = 0; i < str.Length; i++) {
                ch = str[i];
                if (ch < 0x20 || ch > 0x7e || ch == '\\') {
                    String s = "0000" + Convert.ToString(ch, 16);
                    retval += "\\u" + s.Substring(s.Length - 4, s.Length);
                } else {
                    retval += ch;
                }
            }
            return retval;
        }

        internal static void PrintTokenSetup(Token t) {
            Token tt = t;
            while (tt.specialToken != null)
                tt = tt.specialToken;
            cline = tt.beginLine;
            ccol = tt.beginColumn;
        }

        internal static void PrintTokenOnly(Token t, TextWriter ostr) {
            for (; cline < t.beginLine; cline++) {
                ostr.WriteLine("");
                ccol = 1;
            }
            for (; ccol < t.beginColumn; ccol++) {
                ostr.Write(" ");
            }
            if (t.kind == CSharpCCParserConstants.STRING_LITERAL ||
                t.kind == CSharpCCParserConstants.CHARACTER_LITERAL)
                ostr.Write(AddUnicodeEscapes(t.image));
            else
                ostr.Write(t.image);
            cline = t.endLine;
            ccol = t.endColumn + 1;
            char last = t.image[t.image.Length - 1];
            if (last == '\n' || last == '\r') {
                cline++;
                ccol = 1;
            }
        }

        internal static void PrintToken(Token t, TextWriter ostr) {
            Token tt = t.specialToken;
            if (tt != null) {
                while (tt.specialToken != null)
                    tt = tt.specialToken;
                while (tt != null) {
                    PrintTokenOnly(tt, ostr);
                    tt = tt.next;
                }
            }
            PrintTokenOnly(t, ostr);
        }

        internal static void PrintTokenList(IList<Token> list, TextWriter ostr) {
            Token t = null;
            foreach (Token token in list) {
                t = token;
                PrintToken(t, ostr);
            }

            if (t != null)
                PrintTrailingComments(t, ostr);
        }

        internal static void PrintLeadingComments(Token t, TextWriter ostr) {
            if (t.specialToken == null)
                return;
            Token tt = t.specialToken;
            while (tt.specialToken != null)
                tt = tt.specialToken;
            while (tt != null) {
                PrintTokenOnly(tt, ostr);
                tt = tt.next;
            }
            if (ccol != 1 && cline != t.beginLine) {
                ostr.WriteLine("");
                cline++;
                ccol = 1;
            }
        }

        internal static void PrintTrailingComments(Token t, TextWriter ostr) {
            if (t.next == null)
                return;
            PrintLeadingComments(t.next);
        }

        internal static string PrintTokenOnly(Token t) {
            String retval = "";
            for (; cline < t.beginLine; cline++) {
                retval += "\n";
                ccol = 1;
            }
            for (; ccol < t.beginColumn; ccol++) {
                retval += " ";
            }
            if (t.kind == CSharpCCParserConstants.STRING_LITERAL ||
                t.kind == CSharpCCParserConstants.CHARACTER_LITERAL)
                retval += AddUnicodeEscapes(t.image);
            else
                retval += t.image;
            cline = t.endLine;
            ccol = t.endColumn + 1;
            char last = t.image[t.image.Length - 1];
            if (last == '\n' || last == '\r') {
                cline++;
                ccol = 1;
            }
            return retval;
        }

        internal static String PrintToken(Token t) {
            String retval = "";
            Token tt = t.specialToken;
            if (tt != null) {
                while (tt.specialToken != null)
                    tt = tt.specialToken;
                while (tt != null) {
                    retval += PrintTokenOnly(tt);
                    tt = tt.next;
                }
            }
            retval += PrintTokenOnly(t);
            return retval;
        }

        internal static String PrintLeadingComments(Token t) {
            String retval = "";
            if (t.specialToken == null)
                return retval;
            Token tt = t.specialToken;
            while (tt.specialToken != null)
                tt = tt.specialToken;
            while (tt != null) {
                retval += PrintTokenOnly(tt);
                tt = tt.next;
            }
            if (ccol != 1 && cline != t.beginLine) {
                retval += "\n";
                cline++;
                ccol = 1;
            }
            return retval;
        }

        internal static String PrintTrailingComments(Token t) {
            if (t.next == null)
                return "";
            return PrintLeadingComments(t.next);
        }

        public static void ReInit() {
            FileName = null;
            OriginalFileName = null;
            TreeGenerated = false;
            ToolNames = null;
            cu_name = null;
            cu_to_insertion_point_1 = new List<Token>();
            cu_to_insertion_point_2 = new List<Token>();
            cu_from_insertion_point_2 = new List<Token>();
            bnfproductions = new List<NormalProduction>();
            production_table = new Dictionary<string, NormalProduction>();
            lexstate_S2I = new Dictionary<string, int>();
            lexstate_I2S = new Dictionary<int, string>();
            token_mgr_decls = null;
            rexprlist = new List<TokenProduction>();
            tokenCount = 0;
            named_tokens_table = new Dictionary<string, RegularExpression>();
            ordered_named_tokens = new List<RegularExpression>();
            names_of_tokens = new Dictionary<int, string>();
            rexps_of_tokens = new Dictionary<int, RegularExpression>();
            simple_tokens_table = new Dictionary<string, IDictionary<string, RegularExpression>>();
            maskindex = 0;
            cc2index = 0;
            maskVals = new List<int[]>();
            cline = 0;
            ccol = 0;
            actForEof = null;
            nextStateForEof = null;
        }
    }
}