﻿using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace Cosmos.Debug.Common.CDebugger
{
	public class DebugSymbol {
		public string AssemblyFileName {
			get;
			set;
		}

		public int MethodMetaDataToken {
			get;
			set;
		}

		public int InstructionOffset {
			get;
			set;
		}

		public string LabelName {
			get;
			set;
		}
	}

	public class MLDebugSymbol {

        static protected FbConnection DBConn;

        protected static void OpenCPDB(string aPathname, bool aCreate) {
            var xCSB = new FbConnectionStringBuilder();
            xCSB.ServerType = FbServerType.Embedded;
            xCSB.Database = aPathname;
            xCSB.UserID = "sysdba";
            xCSB.Password = "masterkey";
            xCSB.Pooling = false;

            if (aCreate) {
                File.Delete(aPathname);
                FbConnection.CreateDatabase(xCSB.ToString());
            }

            DBConn = new FbConnection(xCSB.ToString());
            DBConn.Open();
        }

        protected static void CreateCPDB(string aPathname) {
            OpenCPDB(aPathname, true);
            var xExec = new FbBatchExecution(DBConn);

            xExec.SqlStatements.Add(
                "CREATE TABLE SYMBOL ("
                + "   LABELNAME   VARCHAR(255)  NOT NULL"
                + " , ADDRESS     BIGINT        NOT NULL"
                + " , STACKDIFF   INT           NOT NULL"
                + " , ILASMFILE   VARCHAR(255)  NOT NULL"
                + " , TYPETOKEN   INT           NOT NULL"
                + " , METHODTOKEN INT           NOT NULL"
                + " , ILOFFSET    INT           NOT NULL"
                + " , METHODNAME  VARCHAR(255)  NOT NULL"
                + ");"
            );

            xExec.Execute();
            // Batch execution closes the connection, so we have to reopen it
            DBConn.Open();
        }

		public static void WriteSymbolsListToFile(IEnumerable<MLDebugSymbol> aSymbols, string aFile) {
            CreateCPDB(Path.ChangeExtension(aFile, ".cpdb"));

            var xDS = new SymbolsDS();
            // Is a real DB now, but we still store all in RAM. We dont need to. Need to change to query DB as needed instead.
            foreach(var xItem in aSymbols){
                var x = xDS.Entry.NewEntryRow();
                x.LabelName = xItem.LabelName;
                x.Address = xItem.Address;
                x.StackDiff = xItem.StackDifference;
                x.ILAsmFile = xItem.AssemblyFile;
                x.TypeToken = xItem.TypeToken;
                x.MethodToken = xItem.MethodToken;
                x.ILOffset = xItem.ILOffset;
                x.MethodName = xItem.MethodName;
                xDS.Entry.AddEntryRow(x);

                var xCmd = new FbCommand("INSERT INTO SYMBOL (LABELNAME, ADDRESS, STACKDIFF, ILASMFILE, TYPETOKEN, METHODTOKEN, ILOFFSET, METHODNAME)"
                    + " VALUES (@LABELNAME, @ADDRESS, @STACKDIFF, @ILASMFILE, @TYPETOKEN, @METHODTOKEN, @ILOFFSET, @METHODNAME)"
                    , DBConn);
                xCmd.Parameters.Add("@LABELNAME", xItem.LabelName);
                xCmd.Parameters.Add("@ADDRESS", xItem.Address);
                xCmd.Parameters.Add("@STACKDIFF", xItem.StackDifference);
                xCmd.Parameters.Add("@ILASMFILE", xItem.AssemblyFile);
                xCmd.Parameters.Add("@TYPETOKEN", xItem.TypeToken);
                xCmd.Parameters.Add("@METHODTOKEN", xItem.MethodToken);
                xCmd.Parameters.Add("@ILOFFSET", xItem.ILOffset);
                xCmd.Parameters.Add("@METHODNAME", xItem.MethodName);
                xCmd.ExecuteNonQuery();
            }
            xDS.WriteXml(aFile);
		}

		public static void ReadSymbolsListFromFile(List<MLDebugSymbol> aSymbols, string aFile) {
            OpenCPDB(Path.ChangeExtension(aFile, ".cpdb"), false);
            var xDS = new SymbolsDS();
            xDS.ReadXml(aFile);
            foreach (SymbolsDS.EntryRow x in xDS.Entry.Rows) {
                aSymbols.Add(new MLDebugSymbol {
                    LabelName = x.LabelName,
                    Address = x.Address,
                    StackDifference = x.StackDiff,
                    AssemblyFile = x.ILAsmFile,
                    TypeToken = x.TypeToken,
                    MethodToken = x.MethodToken,
                    ILOffset = x.ILOffset,
                    MethodName = x.MethodName
                });
            }
		}

		public string LabelName {
			get;
			set;
		}

		public uint Address {
			get;
			set;
		}

		public int StackDifference {
			get;
			set;
		}

		public string AssemblyFile {
			get;
			set;
		}
		public int TypeToken {
			get;
			set;
		}
		public int MethodToken {
			get;
			set;
		}
		public int ILOffset {
			get;
			set;
		}

        public string MethodName
        {
            get;
            set;
        }
	}
}