// $Id$

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

using Math = System.Math;

namespace MSBuild.Community.Tasks.Subversion
{
    /// <summary>
    /// Summarize the local revision(s) of a working copy.
    /// </summary>
    /// <example>The following example gets the revision of the current folder.
    /// <code><![CDATA[
    /// <Target Name="Version">
    ///   <SvnVersion LocalPath=".">
    ///     <Output TaskParameter="Revision" PropertyName="Revision" />
    ///   </SvnVersion>
    ///   <Message Text="Revision: $(Revision)"/>
    /// </Target>
    /// ]]></code>
    /// </example>
    public class SvnVersion : ToolTask
    {
        private static readonly Regex _numberRegex;
        private StringBuilder _outputBuffer;

        static SvnVersion()
        {
            _numberRegex = new Regex(@"\d+", RegexOptions.Compiled);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SvnVersion"/> class.
        /// </summary>
        public SvnVersion()
        {
            _outputBuffer = new StringBuilder();
        }

        #region Properties
        private string _localPath;

        /// <summary>Path to local working copy.</summary>
        [Required]
        public string LocalPath
        {
            get { return _localPath; }
            set { _localPath = value; }
        }

        /// <summary>Revision number of the local working repository.</summary>
        [Output]
        public int Revision
        {
            get { return _highRevision; }
            set { _highRevision = value; }
        }

        private int _highRevision = -1;

        /// <summary>High revision number of the local working repository revision range.</summary>
        [Output]
        public int HighRevision
        {
            get { return _highRevision; }
            set { _highRevision = value; }
        }

        private int _lowRevision = -1;

        /// <summary>Low revision number of the local working repository revision range.</summary>
        [Output]
        public int LowRevision
        {
            get { return _lowRevision; }
            set { _lowRevision = value; }
        }

        private bool _modifications = false;

        /// <summary>True if working copy contains modifications.</summary>
        [Output]
        public bool Modifications
        {
            get { return _modifications; }
            set { _modifications = value; }
        }

        private bool _switched = false;

        /// <summary>True if working copy is switched.</summary>
        [Output]
        public bool Switched
        {
            get { return _switched; }
            set { _switched = value; }
        }

        private bool _exported = false;

        /// <summary>
        /// True if invoked on a directory that is not a working copy, 
        /// svnversion assumes it is an exported working copy and prints "exported".
        /// </summary>
        [Output]
        public bool Exported
        {
            get { return _exported; }
            set { _exported = value; }
        }


        #endregion

        /// <summary>
        /// Returns the fully qualified path to the executable file.
        /// </summary>
        /// <returns>
        /// The fully qualified path to the executable file.
        /// </returns>
        protected override string GenerateFullPathToTool()
        {
            base.ToolPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"Subversion\bin");
            return Path.Combine(ToolPath, ToolName);
        }

        /// <summary>
        /// Gets the <see cref="T:Microsoft.Build.Framework.MessageImportance"></see> with which to log errors.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:Microsoft.Build.Framework.MessageImportance"></see> with which to log errors.</returns>
        protected override MessageImportance StandardOutputLoggingImportance
        {
            get { return MessageImportance.Normal; }
        }

        /// <summary>
        /// Logs the starting point of the run to all registered loggers.
        /// </summary>
        /// <param name="message">A descriptive message to provide loggers, usually the command line and switches.</param>
        protected override void LogToolCommand(string message)
        {
            Log.LogCommandLine(MessageImportance.Low, message);
        }

        /// <summary>
        /// Gets the name of the executable file to run.
        /// </summary>
        /// <value></value>
        /// <returns>The name of the executable file to run.</returns>
        protected override string ToolName
        {
            get { return "svnversion.exe"; }
        }

        /// <summary>
        /// Returns a string value containing the command line arguments to pass directly to the executable file.
        /// </summary>
        /// <returns>
        /// A string value containing the command line arguments to pass directly to the executable file.
        /// </returns>
        protected override string GenerateCommandLineCommands()
        {
            DirectoryInfo localPath = new DirectoryInfo(_localPath);
            return string.Format("--no-newline {0}", localPath.FullName);
        }

        /// <summary>
        /// Runs the exectuable file with the specified task parameters.
        /// </summary>
        /// <returns>
        /// true if the task runs successfully; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            bool result = base.Execute();
            if (result)
            {
                ParseOutput();
            }
            return result;
        }

        private void ParseOutput()
        {
            string buffer = _outputBuffer.ToString();
            MatchCollection revisions = _numberRegex.Matches(buffer);
            foreach (Match rm in revisions)
            {
                int revision;
                if (int.TryParse(rm.Value, out revision))
                {
                    _lowRevision = System.Math.Min(revision, _lowRevision);
                    _highRevision = System.Math.Max(revision, _highRevision);
                }
            }

            _modifications = buffer.Contains("M");
            _switched = buffer.Contains("S");
            _exported = buffer.Contains("exported");
            if (_exported) Log.LogWarning("LocalPath is not a working subversion copy.");

            Debug.WriteLine(string.Format("Revision: {0}; Modifications: {1}; Exported: {2};", 
                _highRevision, _modifications, _exported));
        }

        /// <summary>
        /// Logs the events from text output.
        /// </summary>
        /// <param name="singleLine">The single line.</param>
        /// <param name="messageImportance">The message importance.</param>
        protected override void LogEventsFromTextOutput(string singleLine, Microsoft.Build.Framework.MessageImportance messageImportance)
        {
            base.LogEventsFromTextOutput(singleLine, messageImportance);
            _outputBuffer.Append(singleLine);
        }
        
    }
}